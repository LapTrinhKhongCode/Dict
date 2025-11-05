using Dict.Data;
using Dict.DTO.Admin;
using Dict.DTO.Deck;
using Dict.DTO.User;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using BCrypt.Net; // Thêm
using System.Globalization;
using Dict.Models.Enum; // Thêm

namespace Dict.Service
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string DefaultAvatarUrl = "/images/default_ava.jpg";

        public AdminService(
            ApplicationDbContext context,
            ILogger<AdminService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✨ ĐÃ SỬA LỖI: Chạy truy vấn tuần tự
        public async Task<AdminDashboardStatsDto> GetDashboardStatisticsAsync()
        {
            var today = DateTime.UtcNow;
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Chạy tuần tự để tránh lỗi DbContext concurrency
            var totalUsers = await _context.Users.CountAsync();
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth);
            var totalDecks = await _context.Decks.CountAsync();
            var newDecks = await _context.Decks.CountAsync(d => d.CreatedAt >= startOfMonth);
            var premiumUsers = await _context.Users.CountAsync(u => u.Role == Role.PREMIUM_USER);
            var ocrJobs = await _context.OcrJobs.CountAsync(j => j.CreatedAt >= startOfMonth);
            // Sửa lỗi SumAsync cho kiểu nullable
            var totalStorageBytes = await _context.MediaStore.SumAsync(m => (long?)m.SizeBytes) ?? 0L;

            return new AdminDashboardStatsDto
            {
                TotalUsers = totalUsers,
                NewUsersThisMonth = newUsers,
                TotalDecks = totalDecks,
                NewDecksThisMonth = newDecks,
                TotalPremiumUsers = premiumUsers,
                TotalOcrJobsThisMonth = ocrJobs,
                TotalStorageUsedMb = Math.Round(totalStorageBytes / (1024.0 * 1024.0), 2)
            };
        }

        // --- CÁC HÀM MỚI ---

        public async Task<PaginatedUsersDto> SearchUsersAsync(string? searchTerm, int page, int pageSize)
        {
            // Đảm bảo page và pageSize hợp lệ
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower().Trim();
                query = query.Where(u => u.Username.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards) // Include lồng để đếm Card
                .ToListAsync();

            return new PaginatedUsersDto
            {
                Users = users.Select(MapUserToDto).ToList(), // Dùng hàm Map
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<HistoricalStatsDto> GetMonthlyGrowthStatisticsAsync()
        {
            var today = DateTime.UtcNow;
            // Lấy dữ liệu từ đầu của 11 tháng trước (tổng cộng 12 tháng)
            var lookbackDate = new DateTime(today.Year, today.Month, 1).AddMonths(-11);

            // Nhóm người dùng mới
            var usersByMonth = await _context.Users
                .Where(u => u.CreatedAt >= lookbackDate)
                .GroupBy(u => new { Year = u.CreatedAt.Value.Year, Month = u.CreatedAt.Value.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // Nhóm deck mới
            var decksByMonth = await _context.Decks
                .Where(d => d.CreatedAt >= lookbackDate)
                .GroupBy(d => new { Year = d.CreatedAt.Value.Year, Month = d.CreatedAt.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // Tạo danh sách 12 tháng qua
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

            return new HistoricalStatsDto { MonthlyData = monthlyData.OrderBy(d => d.Month).ToList() }; // Sắp xếp theo thứ tự
        }

        public async Task<bool> AdminResetUserPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("AdminResetUserPasswordAsync: User not found with ID {UserId}", userId);
                return false;
            }

            if (user.Role == Role.ADMIN)
            {
                _logger.LogWarning("AdminResetUserPasswordAsync: Admin {AdminId} attempted to reset password of ADMIN account {TargetUserId}", GetAdminId(), userId);
                return false; // Không cho reset pass của Admin khác
            }

            // Băm mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} reset password for User {UserId}", GetAdminId(), userId);
            return true;
        }

        public async Task<bool> SetDeckVisibilityAsync(int deckId, bool isPublic)
        {
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


        // --- CÁC HÀM CŨ (Đã có trong code bạn cung cấp) ---

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
               .AsNoTracking()
               .Include(u => u.Decks)
                   .ThenInclude(d => d.Cards)
               .OrderBy(u => u.Username)
               .ToListAsync();
            return users.Select(user => MapUserToDto(user));
        }

        public async Task<bool> SetUserLockStatusAsync(int userId, bool isLocked)
        {
            var user = await _context.Users.FindAsync(userId); if (user == null) return false;
            if (user.Role == Role.ADMIN) { _logger.LogWarning("Admin {AdminId} attempted to lock ADMIN {TargetUserId}", GetAdminId(), userId); return false; }
            user.IsActive = !isLocked; user.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} set User {UserId} IsActive to {IsActive}", GetAdminId(), userId, user.IsActive);
            return true;
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            var user = await _context.Users.FindAsync(userId); if (user == null) return false;
            var validRoles = typeof(Role).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.GetValue(null).ToString()).ToList();
            if (!validRoles.Contains(newRole)) { _logger.LogWarning("Admin {AdminId} specified invalid role '{NewRole}'", GetAdminId(), newRole); throw new ArgumentException("Invalid role specified."); }
            if (user.Role == Role.ADMIN) { _logger.LogWarning("Admin {AdminId} attempted to change role of ADMIN {TargetUserId}", GetAdminId(), userId); return false; }
            user.Role = newRole; user.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} updated User {UserId} role to {NewRole}", GetAdminId(), userId, newRole);
            return true;
        }

        public async Task<bool> AdminDeleteDeckAsync(int deckId)
        {
            var deck = await _context.Decks.Include(d => d.Cards).ThenInclude(c => c.CardStates).Include(d => d.Cards).ThenInclude(c => c.ReviewLogs).FirstOrDefaultAsync(d => d.Id == deckId);
            if (deck == null) { _logger.LogWarning("AdminDeleteDeckAsync: Deck not found: {DeckId}", deckId); return false; }
            foreach (var card in deck.Cards) { _context.ReviewLogs.RemoveRange(card.ReviewLogs); _context.CardStates.RemoveRange(card.CardStates); }
            _context.Cards.RemoveRange(deck.Cards); _context.Decks.Remove(deck); await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} deleted deck {DeckId}", GetAdminId(), deckId);
            return true;
        }

        public async Task<bool> AdminDeleteUserAsync(int userId)
        {
            _logger.LogWarning("Admin {AdminId} attempting DESTRUCTIVE delete of user {UserId}", GetAdminId(), userId);
            var user = await _context.Users.FindAsync(userId); if (user == null) return false;
            if (user.Role == Role.ADMIN) { _logger.LogError("CRITICAL: Admin {AdminId} attempted to DELETE ADMIN {TargetUserId}", GetAdminId(), userId); return false; }
            try
            {
                // Xóa thủ công các dependencies
                var decks = await _context.Decks.Where(d => d.UserId == userId).ToListAsync();
                foreach (var deck in decks) { await AdminDeleteDeckAsync(deck.Id); } // Tái sử dụng logic xóa deck

                var ocrJobs = await _context.OcrJobs.Where(j => j.UserId == userId).ToListAsync(); if (ocrJobs.Any()) _context.OcrJobs.RemoveRange(ocrJobs);
                var media = await _context.MediaStore.Where(m => m.OwnerId == userId).ToListAsync(); if (media.Any()) _context.MediaStore.RemoveRange(media);
                var otherCardStates = await _context.CardStates.Where(cs => cs.UserId == userId).ToListAsync(); if (otherCardStates.Any()) _context.CardStates.RemoveRange(otherCardStates);
                var otherReviewLogs = await _context.ReviewLogs.Where(rl => rl.UserId == userId).ToListAsync(); if (otherReviewLogs.Any()) _context.ReviewLogs.RemoveRange(otherReviewLogs);

                _context.Users.Remove(user); await _context.SaveChangesAsync();
                _logger.LogInformation("Admin {AdminId} successfully deleted user {UserId}", GetAdminId(), userId);
                return true;
            }
            catch (DbUpdateException ex) { _logger.LogError(ex, "AdminDeleteUserAsync: FAILED due to FK constraint. User {UserId}", userId); return false; }
        }

        // --- HÀM HELPER ---

        private UserDto MapUserToDto(User user)
        {
            string avatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl;
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                Role = user.Role,
                AvatarUrl = avatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Decks = user.Decks?.Select(d => new DeckSummaryDto { Id = d.Id, Name = d.Name, Description = d.Description ?? "", IsPublic = d.IsPublic ?? false, CardCount = d.Cards?.Count() ?? 0, AuthorName = user.Username }).ToList() ?? new List<DeckSummaryDto>()
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