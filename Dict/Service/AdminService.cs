using Dict.Data;
using Dict.DTO;
using Dict.DTO.Admin;
using Dict.DTO.Deck;
using Dict.DTO.User;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.AspNetCore.Http;
// 1. THÊM
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Dict.Service
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context; // Vẫn cần DbContext cho Decks, OcrJobs, v.v.
        private readonly ILogger<AdminService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string DefaultAvatarUrl = "/images/default_ava.jpg";

        // 2. THÊM MANAGER CỦA IDENTITY
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        // 3. CẬP NHẬT CONSTRUCTOR
        public AdminService(
            ApplicationDbContext context,
            ILogger<AdminService> logger,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager, // THÊM
            RoleManager<ApplicationRole> roleManager) // THÊM
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager; // GÁN
            _roleManager = roleManager; // GÁN
        }

        // 4. SỬA LẠI HÀM GETDASHBOARD (DÙNG MANAGER)
        public async Task<AdminDashboardStatsDto> GetDashboardStatisticsAsync()
        {
            var today = DateTime.UtcNow;
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Dùng _userManager.Users thay vì _context.Users
            var totalUsers = await _userManager.Users.CountAsync();
            var newUsers = await _userManager.Users.CountAsync(u => u.CreatedAt >= startOfMonth);

            var totalDecks = await _context.Decks.CountAsync();
            var newDecks = await _context.Decks.CountAsync(d => d.CreatedAt >= startOfMonth);

            // Sửa lại cách đếm Premium Users (dùng RoleManager)
            var premiumRole = await _roleManager.FindByNameAsync("PREMIUM_USER"); 
            var premiumUsers = 0;
            if (premiumRole != null)
            {
                // Đếm số user có RoleId này trong bảng AspNetUserRoles
                premiumUsers = await _context.UserRoles.CountAsync(ur => ur.RoleId == premiumRole.Id);
            }

            var ocrJobs = await _context.OcrJobs.CountAsync(j => j.CreatedAt >= startOfMonth);
            var dbSizeMb = 0.0;
            try
            {
                var dbSizeResult = await _context.Database
                    .SqlQueryRaw<decimal>("SELECT SUM(CAST(size AS DECIMAL(18,2))) * 8.0 / 1024.0 AS Value FROM sys.database_files")
                    .FirstOrDefaultAsync();

                dbSizeMb = (double)dbSizeResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể truy vấn dung lượng database. Đảm bảo user database có quyền 'VIEW DATABASE STATE'.");
                dbSizeMb = 0; // Đặt là 0 nếu có lỗi
            }

            return new AdminDashboardStatsDto
            {
                TotalUsers = totalUsers,
                NewUsersThisMonth = newUsers,
                TotalDecks = totalDecks,
                NewDecksThisMonth = newDecks,
                TotalPremiumUsers = premiumUsers, // Đã sửa
                TotalOcrJobsThisMonth = ocrJobs,
                TotalStorageUsedMb = Math.Round(dbSizeMb, 2)
            };
        }

        // 5. SỬA LẠI HÀM SEARCHUSERS (DÙNG MANAGER VÀ SỬA VÒNG LẶP MAP)
        public async Task<PagedResult<UserDto>> SearchUsersAsync(string? searchTerm, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            // Dùng _userManager.Users
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(lowerSearchTerm) ||
                                         u.Email.ToLower().Contains(lowerSearchTerm));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .ToListAsync();

            // Sửa lại vòng lặp Map vì MapUserToDto giờ là async
            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                userDtos.Add(await MapUserToDto(user));
            }

            return new PagedResult<UserDto>
            {
                Items = userDtos, // Đã sửa
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // 6. SỬA LẠI HÀM GETMONTHLYGROWTH (DÙNG MANAGER)
        public async Task<HistoricalStatsDto> GetMonthlyGrowthStatisticsAsync()
        {
            var today = DateTime.UtcNow;
            var lookbackDate = new DateTime(today.Year, today.Month, 1).AddMonths(-11);

            // Dùng _userManager.Users
            var usersByMonth = await _userManager.Users
                .Where(u => u.CreatedAt >= lookbackDate)
                .GroupBy(u => new { Year = u.CreatedAt.Value.Year, Month = u.CreatedAt.Value.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // (Phần Decks giữ nguyên)
            var decksByMonth = await _context.Decks
                .Where(d => d.CreatedAt >= lookbackDate)
                .GroupBy(d => new { Year = d.CreatedAt.Value.Year, Month = d.CreatedAt.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // (Logic lặp tháng giữ nguyên)
            var monthlyData = new List<MonthlyDataPointDto>();
            for (int i = 0; i < 12; i++)
            {
                var month = today.AddMonths(-i);
                var monthKey = $"{month.Year}-{month.Month:D2}";
                var users = usersByMonth.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                var decks = decksByMonth.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);

                monthlyData.Add(new MonthlyDataPointDto
                {
                    Month = monthKey,
                    NewUserCount = users?.Count ?? 0,
                    NewDeckCount = decks?.Count ?? 0
                });
            }

            return new HistoricalStatsDto { MonthlyData = monthlyData.OrderBy(d => d.Month).ToList() };
        }

        // 7. SỬA LẠI HOÀN TOÀN HÀM RESET PASSWORD
        public async Task<bool> AdminResetUserPasswordAsync(int userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("AdminResetUserPasswordAsync: User not found with ID {UserId}", userId);
                return false;
            }

            // Dùng IsInRoleAsync
            if (await _userManager.IsInRoleAsync(user, "ADMIN"))
            {
                _logger.LogWarning("ADMINResetUserPasswordAsync: ADMIN {ADMINId} attempted to reset password of ADMIN account {TargetUserId}", GetAdminId(), userId);
                return false;
            }

            // Xóa mật khẩu cũ (nếu có)
            if (await _userManager.HasPasswordAsync(user))
            {
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded) throw new Exception("Failed to remove old password.");
            }

            // Thêm mật khẩu mới ( UserManager sẽ tự hash)
            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to add new password: {errors}");
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user); // Lưu lại UpdateAt

            _logger.LogInformation("Admin {AdminId} reset password for User {UserId}", GetAdminId(), userId);
            return true;
        }

        public async Task<bool> SetDeckVisibilityAsync(int deckId, bool isPublic)
        {
            // (Hàm này OK, không liên quan đến User)
            var deck = await _context.Decks.FindAsync(deckId);
            if (deck == null)
            {
                _logger.LogWarning("SetDeckVisibilityAsync: Deck not found with ID {DeckId}", deckId);
                return false;
            }

            deck.IsPublic = isPublic;
            deck.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} set Deck {DeckId} visibility to {IsPublic}", GetAdminId(), deckId, isPublic);
            return true;
        }


        // 8. SỬA LẠI HÀM GETALLUSERS (SỬA VÒNG LẶP MAP)
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
               .AsNoTracking()
               .Include(u => u.Decks)
                   .ThenInclude(d => d.Cards)
               .OrderBy(u => u.UserName)
               .ToListAsync();

            // Sửa vòng lặp
            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                dtos.Add(await MapUserToDto(user));
            }
            return dtos;
        }

        // 9. SỬA LẠI HÀM LOCKSTATUS (DÙNG MANAGER)
        public async Task<bool> SetUserLockStatusAsync(int userId, bool isLocked)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            if (await _userManager.IsInRoleAsync(user, "ADMIN"))
            {
                _logger.LogWarning("Admin {AdminId} attempted to lock ADMIN {TargetUserId}", GetAdminId(), userId);
                return false;
            }

            user.IsActive = !isLocked; // Cập nhật trường tùy chỉnh
            user.UpdatedAt = DateTime.UtcNow;

            // Dùng UserManager để lưu
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Admin {AdminId} set User {UserId} IsActive to {IsActive}", GetAdminId(), userId, user.IsActive);
                return true;
            }
            return false;
        }

        // 10. SỬA LẠI HOÀN TOÀN HÀM UPDATE ROLE
        public async Task<bool> UpdateUserRolesAsync(int userId, List<string> newRoleNames)
        {
            if (newRoleNames == null)
            {
                // Chủ động ném ra lỗi đúng loại mà test đang mong đợi
                throw new ArgumentNullException(nameof(newRoleNames));
            }
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            if (await _userManager.IsInRoleAsync(user, "ADMIN"))
            {
                _logger.LogWarning("Admin {AdminId} attempted to change role of ADMIN {TargetUserId}", GetAdminId(), userId);
                return false;
            }

            foreach (var roleName in newRoleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogWarning("Admin {AdminId} specified invalid role '{NewRole}'", GetAdminId(), roleName);
                    throw new ArgumentException($"Invalid role specified: {roleName}");
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new Exception("Failed to remove old roles.");

            var addResult = await _userManager.AddToRolesAsync(user, newRoleNames);
            if (!addResult.Succeeded) throw new Exception("Failed to add new roles.");

            _logger.LogInformation("Admin {AdminId} updated User {UserId} roles to {NewRoles}", GetAdminId(), userId, string.Join(", ", newRoleNames));
            return true;
        }
        public async Task<PagedResult<DeckAdminDto>> SearchAllDecksAsync(string? searchTerm, int page, int pageSize)
        {
            // Bắt đầu query. KHÔNG lọc IsPublic, admin xem được tất cả.
            var query = _context.Decks
                                .Include(d => d.User) // Phải Include Author để lấy AuthorName
                                .AsQueryable();

            // 1. Áp dụng tìm kiếm (nếu có)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerTerm = searchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(lowerTerm) ||       // Tìm theo tên deck
                    (d.User != null && d.User.UserName.ToLower().Contains(lowerTerm)) // Tìm theo tên tác giả
                );
            }

            // 2. Lấy tổng số lượng (cho phân trang)
            var totalCount = await query.CountAsync();

            // 3. Áp dụng phân trang và Select DTO
            var decks = await query
                .OrderByDescending(d => d.CreatedAt) // Sắp xếp theo deck mới nhất
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeckAdminDto // ✨ Chuyển đổi sang DTO
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    IsPublic = d.IsPublic,
                    CardCount = d.Cards.Count, // Đếm số thẻ (EF Core sẽ dịch thành subquery)
                    AuthorId = d.UserId,
                    AuthorName = d.User != null ? d.User.UserName : "N/A", // Lấy tên tác giả
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            // 4. Trả về kết quả
            return new PagedResult<DeckAdminDto>
            {
                Items = decks,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<bool> AdminDeleteDeckAsync(int deckId)
        {
            // (Hàm này OK)
            var deck = await _context.Decks.Include(d => d.Cards).ThenInclude(c => c.CardStates).Include(d => d.Cards).ThenInclude(c => c.ReviewLogs).FirstOrDefaultAsync(d => d.Id == deckId);
            if (deck == null) { _logger.LogWarning("AdminDeleteDeckAsync: Deck not found: {DeckId}", deckId); return false; }
            foreach (var card in deck.Cards) { _context.ReviewLogs.RemoveRange(card.ReviewLogs); _context.CardStates.RemoveRange(card.CardStates); }
            _context.Cards.RemoveRange(deck.Cards); _context.Decks.Remove(deck); await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} deleted deck {DeckId}", GetAdminId(), deckId);
            return true;
        }

        // 11. SỬA LẠI HÀM DELETE USER (DÙNG MANAGER)
        public async Task<bool> AdminDeleteUserAsync(int userId)
        {
            _logger.LogWarning("Admin {AdminId} attempting DESTRUCTIVE delete of user {UserId}", GetAdminId(), userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            if (await _userManager.IsInRoleAsync(user, "ADMIN"))
            {
                _logger.LogError("CRITICAL: Admin {AdminId} attempted to DELETE ADMIN {TargetUserId}", GetAdminId(), userId);
                return false;
            }

            try
            {
                // BƯỚC 1: Xóa thủ công các dependencies (OK)
                var decks = await _context.Decks.Where(d => d.UserId == userId).ToListAsync();
                foreach (var deck in decks) { await AdminDeleteDeckAsync(deck.Id); }

                var ocrJobs = await _context.OcrJobs.Where(j => j.UserId == userId).ToListAsync(); if (ocrJobs.Any()) _context.OcrJobs.RemoveRange(ocrJobs);
                var media = await _context.MediaStore.Where(m => m.OwnerId == userId).ToListAsync(); if (media.Any()) _context.MediaStore.RemoveRange(media);
                var otherCardStates = await _context.CardStates.Where(cs => cs.UserId == userId).ToListAsync(); if (otherCardStates.Any()) _context.CardStates.RemoveRange(otherCardStates);
                var otherReviewLogs = await _context.ReviewLogs.Where(rl => rl.UserId == userId).ToListAsync(); if (otherReviewLogs.Any()) _context.ReviewLogs.RemoveRange(otherReviewLogs);

                // (Lưu các thay đổi xóa phụ thuộc)
                await _context.SaveChangesAsync();

                // BƯỚC 2: Xóa User bằng UserManager (OK)
                var deleteResult = await _userManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    throw new Exception(string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
                }

                _logger.LogInformation("Admin {AdminId} successfully deleted user {UserId}", GetAdminId(), userId);
                return true;
            }
            catch (Exception ex) // Bắt lỗi chung
            {
                _logger.LogError(ex, "AdminDeleteUserAsync: FAILED. User {UserId}", userId);
                return false;
            }
        }


        // --- CÁC HÀM THỐNG KÊ NÂNG CAO (ĐÃ SỬA) ---

        /// <summary>
        /// Thống kê 20 từ được tra cứu nhiều nhất (tra thành công)
        /// </summary>
        public async Task<List<TopSearchItemDto>> GetTopSearchedWordsAsync()
        {
            // === SỬA LỖI: JOIN VỚI entries THAY VÌ words ===
            var topWords = await _context.StatsWordFreq
                .AsNoTracking()
                .OrderByDescending(s => s.Occurrences)
                .Take(20)
                // Join với bảng Entries để lấy Label
                .Join(_context.Entries, // <-- SỬA
                    stat => stat.EntryId, // <-- SỬA (EntryId)
                    entry => entry.Id,
                    (stat, entry) => new TopSearchItemDto
                    {
                        Term = entry.Label, // <-- SỬA (Dùng Label của Entry)
                        Count = stat.Occurrences ?? 0,
                        WordId = entry.Id, // (Tên DTO là WordId, nhưng nó đang là EntryId)
                        ActualWord = entry.Label // <-- SỬA
                    })
                .ToListAsync();

            return topWords;
        }

        /// <summary>
        /// Thống kê 20 từ khóa tìm không thấy (Lỗ hổng nội dung)
        /// </summary>
        public async Task<List<TopSearchItemDto>> GetTopSearchMissesAsync()
        {
            // (Code này đã đúng, vì nó dùng SearchMiss (số ít) như bạn đã sửa)
            var topMisses = await _context.SearchMiss
                .AsNoTracking()
                .OrderByDescending(sm => sm.SearchCount)
                .Take(20)
                .Select(sm => new TopSearchItemDto
                {
                    Term = sm.SearchTerm,
                    Count = sm.SearchCount,
                    WordId = null,
                    ActualWord = null // Không tìm thấy
                })
                .ToListAsync();

            return topMisses;
        }

        /// <summary>
        /// Thống kê hiệu suất API (Lỗi và Tốc độ)
        /// </summary>
        public async Task<List<ApiStatItemDto>> GetApiPerformanceStatsAsync()
        {
            // (Code này đúng, không phụ thuộc vào schema)
            var apiStats = await _context.ApiCalls
                .GroupBy(a => a.Endpoint)
                .Select(g => new ApiStatItemDto
                {
                    Endpoint = g.Key,
                    TotalCalls = g.Count(),
                    ErrorCount = g.Count(a => a.ResponseStatus >= 400),
                    AvgResponseTimeMs = (int)g.Average(a => a.ResponseTimeMs),
                })
                .OrderByDescending(a => (double)a.ErrorCount / a.TotalCalls)
                .ThenByDescending(a => a.AvgResponseTimeMs)
                .ToListAsync();

            foreach (var stat in apiStats)
            {
                stat.ErrorRate = stat.TotalCalls > 0 ? Math.Round((double)stat.ErrorCount / stat.TotalCalls * 100, 2) : 0;
            }

            return apiStats;
        }

        /// <summary>
        /// Lấy danh sách các Job hệ thống bị lỗi (OCR và Import)
        /// </summary>
        public async Task<List<FailedJobItemDto>> GetFailedSystemJobsAsync()
        {
            // (Code này đúng, không phụ thuộc vào schema)
            var failedOcrJobs = await _context.OcrJobs
                .AsNoTracking()
                .Where(j => j.Status == "Failed")
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new FailedJobItemDto
                {
                    Id = j.Id,
                    JobType = "OCR",
                    Status = j.Status,
                    ErrorMessage = "Check OcrJob details for specific error.",
                    StartedAt = j.CreatedAt ?? DateTime.MinValue
                })
                .Take(50)
                .ToListAsync();

            var failedImportJobs = await _context.ImportJobs
                .AsNoTracking()
                .Where(j => j.Status == "Failed")
                .OrderByDescending(j => j.StartedAt)
                .Select(j => new FailedJobItemDto
                {
                    Id = j.Id,
                    JobType = j.JobType,
                    Status = j.Status,
                    ErrorMessage = EF.Functions.Like(j.Meta, "%error%") ? j.Meta.Substring(0, Math.Min(j.Meta.Length, 200)) : j.Meta,
                    StartedAt = j.StartedAt ?? DateTime.MinValue
                })
                .Take(50)
                .ToListAsync();

            return failedOcrJobs.Concat(failedImportJobs)
                .OrderByDescending(j => j.StartedAt)
                .ToList();
        }

        // --- HÀM HELPER ---

        private async Task<UserDto> MapUserToDto(ApplicationUser user)
        {
            string avatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl;
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                Role = roles.FirstOrDefault(),
                AvatarUrl = avatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Decks = user.Decks?.Select(d => new DeckSummaryDto { Id = d.Id, Name = d.Name, Description = d.Description ?? "", IsPublic = d.IsPublic ?? false, CardCount = d.Cards?.Count() ?? 0, AuthorName = user.UserName }).ToList() ?? new List<DeckSummaryDto>()
            };
        }

        private string GetAdminId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return "UnknownAdmin (No HttpContext)";
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "UnknownAdmin (No Claim)";
        }
    }
}