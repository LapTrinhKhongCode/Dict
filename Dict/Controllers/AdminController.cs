using Dict.DTO;
using Dict.DTO.Admin;
using Microsoft.EntityFrameworkCore;
// using Dict.Models.Enum; // <-- XÓA
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Http; // ✨ THÊM: 1. Cần cho IHttpClientFactory
using System.Net.Http.Headers; // ✨ THÊM: 1. Cần cho AuthenticationHeaderValue
using Dict.Service; // ✨ THÊM: 1. Giả sử đây là namespace của AzureTokenProvider
namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")] // ✨ SỬA: Dùng string "Admin"
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ResponseDTO _response;
        private readonly ILogger<AdminController> _logger;
        private readonly AzureTokenProvider _tokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AdminController(
            IAdminService adminService,
            ILogger<AdminController> logger,
            AzureTokenProvider tokenProvider,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _adminService = adminService;
            _response = new ResponseDTO();
            _logger = logger;
            _tokenProvider = tokenProvider;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Lấy ID của Admin đang thực hiện hành động (để ghi log)
        /// </summary>
        private string GetAdminId() // Sửa thành string cho an toàn
        {
            // ✨ SỬA: Dùng ClaimTypes.NameIdentifier
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
            }
            return userIdClaim.Value;
        }
        /// <summary>
        /// Lấy số liệu % CPU của một Azure VM (Ví dụ)
        /// </summary>
        [HttpGet("azure/vm-cpu")]
        public async Task<IActionResult> GetAzureVmCpu()
        {
            try
            {
                // BƯỚC 1: LẤY TOKEN TỰ ĐỘNG
                string accessToken = await _tokenProvider.GetAccessTokenAsync();

                var resourceId = _configuration["AzureAd:VmResourceId"];
                var metricName = "Percentage CPU";
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddHours(-1);
                var timeSpan = $"{startTime:yyyy-MM-ddTHH:mm:ssZ}/{endTime:yyyy-MM-ddTHH:mm:ssZ}";
                var interval = "PT5M";

                var url = $"https://management.azure.com{resourceId}/providers/Microsoft.Insights/metrics" +
                          $"?api-version=2018-01-01" +
                          $"&metricnames={metricName}" +
                          $"&timespan={timeSpan}" +
                          $"&interval={interval}";

                // BƯỚC 3: GỌI API
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var azureResponse = await httpClient.GetAsync(url);
                var jsonString = await azureResponse.Content.ReadAsStringAsync();

                if (!azureResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Lỗi khi gọi Azure API: {StatusCode} - {Content}", azureResponse.StatusCode, jsonString);
                    _response.IsSuccess = false;
                    _response.Message = $"Lỗi từ Azure API: {jsonString}";
                    return StatusCode((int)azureResponse.StatusCode, _response);
                }

                // Trả về dữ liệu JSON thô từ Azure.
                // Bạn có thể deserialize `jsonString` nếu muốn xử lý phía backend
                _response.Result = jsonString;
                _response.Message = "Azure VM metrics retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure VM metrics (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // ✨ THÊM ENDPOINT MỚI CHO AZURE SQL
        /// <summary>
        /// Lấy số liệu (metrics) của một Azure SQL Database.
        /// </summary>
        [HttpGet("azure/sql-db")] // Đặt tên route mới
        public async Task<IActionResult> GetAzureSqlDbMetrics()
        {
            try
            {
                string accessToken = await _tokenProvider.GetAccessTokenAsync();

                var resourceId = _configuration["AzureAd:SqlResourceId"];
                var metricName = "cpu_percent";

                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddHours(-1);
                var timeSpan = $"{startTime:yyyy-MM-ddTHH:mm:ssZ}/{endTime:yyyy-MM-ddTHH:mm:ssZ}";
                var interval = "PT5M";

                // Xây dựng URL cuối cùng
                var url = $"https://management.azure.com{resourceId}/providers/Microsoft.Insights/metrics" +
                          $"?api-version=2018-01-01" +
                          $"&metricnames={metricName}" +
                          $"&timespan={timeSpan}" +
                          $"&interval={interval}";

                // BƯỚC 3: GỌI API (Dùng chung logic)
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var azureResponse = await httpClient.GetAsync(url);
                var jsonString = await azureResponse.Content.ReadAsStringAsync();

                if (!azureResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Lỗi khi gọi Azure API (SQL DB): {StatusCode} - {Content}", azureResponse.StatusCode, jsonString);
                    _response.IsSuccess = false;
                    _response.Message = $"Lỗi từ Azure API: {jsonString}";
                    return StatusCode((int)azureResponse.StatusCode, _response);
                }

                // Trả về dữ liệu JSON thô từ Azure
                _response.Result = jsonString;
                _response.Message = "Azure SQL DB metrics retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure SQL DB metrics (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Lấy số liệu thống kê tổng quan cho Dashboard.
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            try
            {
                _response.Result = await _adminService.GetDashboardStatisticsAsync();
                _response.Message = "Statistics retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Lấy danh sách TẤT CẢ người dùng.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                _response.Result = await _adminService.GetAllUsersAsync();
                _response.Message = "All users retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Lấy danh sách người dùng CÓ PHÂN TRANG và TÌM KIẾM
        /// </summary>
        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _response.Result = await _adminService.SearchUsersAsync(searchTerm, page, pageSize);
                _response.Message = "Users retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }


        /// <summary>
        /// Khóa hoặc mở khóa một tài khoản người dùng.
        /// </summary>
        [HttpPatch("users/{userId}/lock-status")]
        public async Task<IActionResult> SetUserLockStatus(int userId, [FromBody] AdminSetUserLockDto lockDto)
        {
            try
            {
                var result = await _adminService.SetUserLockStatusAsync(userId, lockDto.IsLocked);
                if (!result)
                {
                    _response.IsSuccess = false; _response.Message = "User not found or operation failed (e.g., trying to lock an Admin)."; return NotFound(_response);
                }
                _response.Message = $"User lock status set to {lockDto.IsLocked}.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting lock status for User {TargetUserId} (Admin: {AdminId})", userId, GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Cập nhật vai trò (Role) của một người dùng.
        /// </summary>
        [HttpPatch("users/{userId}/roles")] // Sửa route (số nhiều)
        public async Task<IActionResult> UpdateUserRoles(int userId, [FromBody] AdminUpdateUserRolesDto rolesDto) // Sửa DTO
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false; _response.Message = "Invalid input data."; return BadRequest(_response);
            }

            try
            {
                // ✨ SỬA: Gọi hàm mới
                var result = await _adminService.UpdateUserRolesAsync(userId, rolesDto.RoleNames);
                if (!result)
                {
                    _response.IsSuccess = false; _response.Message = "User not found or operation failed."; return NotFound(_response);
                }
                _response.Message = $"User roles updated.";
                return Ok(_response);
            }
            catch (ArgumentException ex) // Bắt lỗi nếu Role không hợp lệ
            {
                _response.IsSuccess = false; _response.Message = ex.Message; return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for User {TargetUserId} (Admin: {AdminId})", userId, GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Admin xóa vĩnh viễn một bộ thẻ (deck).
        /// </summary>
        [HttpDelete("decks/{deckId}")]
        public async Task<IActionResult> AdminDeleteDeck(int deckId)
        {
            try
            {
                var result = await _adminService.AdminDeleteDeckAsync(deckId);
                if (!result)
                {
                    _response.IsSuccess = false; _response.Message = "Deck not found."; return NotFound(_response);
                }
                _response.Message = "Deck deleted successfully by admin.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deck {DeckId} (Admin: {AdminId})", deckId, GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }
        /// <summary>
        /// [ADMIN] Lấy danh sách TẤT CẢ bộ thẻ (có phân trang) và TÌM KIẾM
        /// </summary>
        [HttpGet("decks/search")]
        public async Task<IActionResult> SearchAllDecks([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Chỉ cần gọi service
                _response.Result = await _adminService.SearchAllDecksAsync(searchTerm, page, pageSize);
                _response.Message = "Decks retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching all decks (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        /// <summary>
        /// Admin xóa vĩnh viễn một người dùng. (NGUY HIỂM)
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> AdminDeleteUser(int userId)
        {
            try
            {
                var result = await _adminService.AdminDeleteUserAsync(userId);
                if (!result)
                {
                    _response.IsSuccess = false; _response.Message = "User not found, is an Admin, or delete failed due to DB constraint."; return BadRequest(_response);
                }
                _response.Message = "User deleted successfully by admin.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {TargetUserId} (Admin: {AdminId})", userId, GetAdminId());
                _response.IsSuccess = false; _response.Message = $"Delete failed: {ex.Message}"; return StatusCode(500, _response);
            }
        }
        /// <summary>
        /// Lấy thống kê tăng trưởng 12 tháng qua.
        /// </summary>
        [HttpGet("statistics/growth")]
        public async Task<IActionResult> GetMonthlyGrowthStatistics()
        {
            try
            {
                _response.Result = await _adminService.GetMonthlyGrowthStatisticsAsync();
                _response.Message = "Monthly growth stats retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetMonthlyGrowthStatistics (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Admin đặt lại mật khẩu cho người dùng.
        /// </summary>
        [HttpPatch("users/{userId}/reset-password")]
        public async Task<IActionResult> AdminResetUserPassword(int userId, [FromBody] AdminResetPasswordDto passDto)
        {
            if (!ModelState.IsValid) { _response.IsSuccess = false; _response.Message = "Invalid input."; _response.Result = ModelState; return BadRequest(_response); }
            try
            {
                var result = await _adminService.AdminResetUserPasswordAsync(userId, passDto.NewPassword);
                if (!result)
                {
                    _response.IsSuccess = false; _response.Message = "User not found or cannot reset Admin password."; return NotFound(_response);
                }
                _response.Message = "User password reset successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error AdminResetUserPassword for {TargetUserId} (Admin: {AdminId})", userId, GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Admin ẩn/hiện một bộ thẻ bất kỳ.
        /// </summary>
        [HttpPatch("decks/{deckId}/visibility")]
        public async Task<IActionResult> SetDeckVisibility(int deckId, [FromBody] AdminSetDeckVisibilityDto visibilityDto)
        {
            try
            {
                var result = await _adminService.SetDeckVisibilityAsync(deckId, visibilityDto.IsPublic);
                if (!result)
                {
                    _response.IsSuccess = false; _response.Message = "Deck not found."; return NotFound(_response);
                }
                _response.Message = $"Deck visibility set to {visibilityDto.IsPublic}.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error SetDeckVisibility for {DeckId} (Admin: {AdminId})", deckId, GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Thống kê 20 từ được tra cứu thành công nhiều nhất.
        /// </summary>
        [HttpGet("statistics/top-searched")]
        public async Task<IActionResult> GetTopSearchedWords()
        {
            try
            {
                _response.Result = await _adminService.GetTopSearchedWordsAsync();
                _response.Message = "Top searched words retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top searched words (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Thống kê 20 từ khóa người dùng tìm nhưng không thấy (Lỗ hổng nội dung).
        /// </summary>
        [HttpGet("statistics/search-misses")]
        public async Task<IActionResult> GetTopSearchMisses()
        {
            try
            {
                _response.Result = await _adminService.GetTopSearchMissesAsync();
                _response.Message = "Top search misses retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top search misses (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Thống kê hiệu suất API (Thời gian phản hồi và Tỷ lệ lỗi).
        /// </summary>
        [HttpGet("statistics/api-performance")]
        public async Task<IActionResult> GetApiPerformanceStats()
        {
            try
            {
                _response.Result = await _adminService.GetApiPerformanceStatsAsync();
                _response.Message = "API performance stats retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API performance stats (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Lấy danh sách các Job hệ thống bị lỗi (OCR/Import).
        /// </summary>
        [HttpGet("statistics/failed-jobs")]
        public async Task<IActionResult> GetFailedSystemJobs()
        {
            try
            {
                _response.Result = await _adminService.GetFailedSystemJobsAsync();
                _response.Message = "Failed system jobs retrieved successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed system jobs (Admin: {AdminId})", GetAdminId());
                _response.IsSuccess = false; _response.Message = ex.Message; return StatusCode(500, _response);
            }
        }

        // GET: api/Admin/workspaces
        [HttpGet("workspaces")]
        public async Task<IActionResult> GetAllWorkspaces()
        {
            try
            {
                var workspaces = await _adminService.GetAllWorkspacesAsync();
                _response.Result = workspaces;
                _response.IsSuccess = true;
                _response.Message = "Lấy danh sách workspace thành công.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi Admin lấy danh sách Workspace");
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi hệ thống.";
                return StatusCode(500, _response);
            }
        }

        // GET: api/Admin/workspaces/{wsId}/projects
        [HttpGet("workspaces/{wsId}/projects")]
        public async Task<IActionResult> GetProjectsByWorkspace(int wsId)
        {
            try
            {
                var projects = await _adminService.GetProjectsByWorkspaceIdAsync(wsId);
                _response.Result = projects;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi Admin lấy projects của Workspace {wsId}");
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi hệ thống.";
                return StatusCode(500, _response);
            }
        }

        // DELETE: api/Admin/workspaces/{id}
        [HttpDelete("workspaces/{id}")]
        public async Task<IActionResult> DeleteWorkspace(int id)
        {
            try
            {
                var isDeleted = await _adminService.DeleteWorkspaceAsync(id);
                if (!isDeleted)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy Workspace để xóa.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "Đã xóa Workspace và toàn bộ dữ liệu liên quan.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi Admin xóa Workspace {id}");
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi khi xóa Workspace.";
                return StatusCode(500, _response);
            }
        }

        // DELETE: api/Admin/projects/{id}
        [HttpDelete("projects/{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var isDeleted = await _adminService.DeleteProjectAsync(id);
                if (!isDeleted)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy Project để xóa.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "Đã xóa Project thành công.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi Admin xóa Project {id}");
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi khi xóa Project.";
                return StatusCode(500, _response);
            }
        }

        // ===== QUẢN LÝ TỪ ĐIỂN (ENTRY) =====

        // GET /api/admin/entries?page=1&pageSize=20&search=
        [HttpGet("entries")]
        public async Task<IActionResult> GetEntries(
            [FromQuery] string? search,
            [FromQuery] string? type,
            [FromQuery] string? jlptLevel,      // N1, N2, N3, N4, N5
            [FromQuery] int? strokeMin,
            [FromQuery] int? strokeMax,
            [FromQuery] int? grade,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var query = db.Entries.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Label.Contains(search) || (e.ShortMean != null && e.ShortMean.Contains(search)));
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(e => e.Type == type);

            var total = await query.CountAsync();

            // Left join với bảng Kanji để lấy Meaning, StrokeCount, JlptLevel, Freq, Grade
            var items = await (
                from e in query
                join k in db.Kanji
                    on EF.Functions.Collate(e.Label, "DATABASE_DEFAULT")
                    equals EF.Functions.Collate(k.Character, "DATABASE_DEFAULT") into kj
                from k in kj.DefaultIfEmpty()
                // Kanji filters — chỉ áp dụng khi type=kanji
                where (string.IsNullOrWhiteSpace(jlptLevel) || k.JlptLevel == jlptLevel)
                   && (!strokeMin.HasValue || k.StrokeCount >= strokeMin)
                   && (!strokeMax.HasValue || k.StrokeCount <= strokeMax)
                   && (!grade.HasValue || k.Grade == grade)
                orderby (e.Weight ?? 0), e.Label
                select new EntryAdminDto
                {
                    Id = e.Id,
                    Label = e.Label,
                    ShortMean = e.ShortMean,
                    Phonetic = e.Phonetic,
                    Romaji = e.Romaji,
                    Type = e.Type,
                    EntryCategory = e.EntryCategory,
                    Weight = e.Weight,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    Meaning = k != null ? k.Meaning : null,
                    StrokeCount = k != null ? k.StrokeCount : null,
                    JlptLevel = k != null ? k.JlptLevel : null,
                    Freq = k != null ? k.Freq : null,
                    Grade = k != null ? k.Grade : null,
                }
            ).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            _response.Result = new PagedResult<EntryAdminDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            };
            return Ok(_response);
        }

        // GET /api/admin/entries/{id}/diagnose
        [AllowAnonymous]
        [HttpGet("entries/{id}/diagnose")]
        public async Task<IActionResult> DiagnoseEntry(int id,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var entry = await db.Entries
                .Where(e => e.Id == id)
                .Select(e => new {
                    e.Id, e.Label, e.Type, e.EntryCategory, e.MobileId,
                    WordCount = e.Words.Count(),
                    SenseCount = e.Senses.Count(),
                    GlossCount = e.Senses.SelectMany(s => s.Glosses).Count(),
                    ExampleCount = e.Senses.SelectMany(s => s.Examples).Count(),
                    HasRawJson = e.RawJson != null && e.RawJson.Length > 10,
                    RawJsonSource = e.RawJson != null && e.RawJson.Contains("\"_rev\"") ? "CouchDB_Old" :
                                   e.RawJson != null && e.RawJson.Length > 10 ? "JsonBuilder" : "Empty",
                    Words = e.Words.Select(w => new { w.Id, w.WordText, w.Phonetic, w.ShortMean, w.Weight, w.MobileId, w.EntryId }).ToList(),
                    Senses = e.Senses.OrderBy(s => s.SenseOrder).Select(s => new {
                        s.Id, s.Pos, s.SenseOrder,
                        Glosses = s.Glosses.Select(g => new { g.Id, g.Text }).ToList(),
                        ExampleCount = s.Examples.Count()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (entry == null) return NotFound();

            // Check for sibling entries with same label or mobileId that have senses
            var siblings = await db.Entries
                .Where(e => e.Id != id && (e.Label == entry.Label || (entry.MobileId != null && e.MobileId == entry.MobileId)))
                .Select(e => new {
                    e.Id, e.Label, e.EntryCategory, e.MobileId,
                    SenseCount = e.Senses.Count(),
                    GlossCount = e.Senses.SelectMany(s => s.Glosses).Count(),
                    RawJsonLen = e.RawJson == null ? 0 : e.RawJson.Length,
                    RawJsonSource = e.RawJson != null && e.RawJson.Contains("\"_rev\"") ? "CouchDB_Old" :
                                   e.RawJson != null && e.RawJson.Length > 10 ? "JsonBuilder" : "Empty",
                })
                .ToListAsync();

            // Show first 300 chars of RawJson for quick inspection
            var rawJsonPreview = await db.Entries
                .Where(e => e.Id == id)
                .Select(e => e.RawJson == null ? "" : e.RawJson.Substring(0, e.RawJson.Length > 300 ? 300 : e.RawJson.Length))
                .FirstOrDefaultAsync();

            _response.Result = new { entry, siblings, rawJsonPreview };
            return Ok(_response);
        }

        // PUT /api/admin/entries/{id}
        [HttpPut("entries/{id}")]
        public async Task<IActionResult> UpdateEntry(int id, [FromBody] UpdateEntryDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var entry = await db.Entries.FindAsync(id);
            if (entry == null) return NotFound("Không tìm thấy từ.");

            entry.ShortMean = dto.ShortMean;
            entry.Phonetic = dto.Phonetic;
            entry.Romaji = dto.Romaji;
            entry.EntryCategory = dto.EntryCategory;
            entry.Weight = dto.Weight;
            entry.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _response.Result = new EntryAdminDto
            {
                Id = entry.Id,
                Label = entry.Label,
                ShortMean = entry.ShortMean,
                Phonetic = entry.Phonetic,
                Romaji = entry.Romaji,
                Type = entry.Type,
                EntryCategory = entry.EntryCategory,
                Weight = entry.Weight,
                UpdatedAt = entry.UpdatedAt,
            };
            _response.Message = "Đã cập nhật từ điển.";
            return Ok(_response);
        }

        // DELETE /api/admin/entries/{id}
        [HttpDelete("entries/{id}")]
        public async Task<IActionResult> DeleteEntry(int id,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var entry = await db.Entries.FindAsync(id);
            if (entry == null) return NotFound("Không tìm thấy từ.");

            db.Entries.Remove(entry);
            await db.SaveChangesAsync();

            _response.Message = $"Đã xóa từ '{entry.Label}'.";
            return Ok(_response);
        }

        // GET /api/admin/entries/{id}/detail — đọc bảng con để edit
        [HttpGet("entries/{id}/detail")]
        public async Task<IActionResult> GetEntryDetail(int id,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var entry = await db.Entries
                .Where(e => e.Id == id)
                .Include(e => e.Words)
                    .ThenInclude(w => w.Relations)
                        .ThenInclude(r => r.RelatedWord)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder))
                    .ThenInclude(s => s.Glosses)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder))
                    .ThenInclude(s => s.Examples)
                .Include(e => e.ReadingElements)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (entry == null) return NotFound();

            // Kanji bảng riêng — nếu là kanji type
            KanjiAdminDto? kanjiDto = null;
            if (entry.Type == "kanji")
            {
                var kanji = await db.Kanji
                    .Where(k => k.Character == entry.Label)
                    .Include(k => k.KanjiExamples.OrderBy(e => e.Id))
                    .FirstOrDefaultAsync();
                if (kanji != null)
                {
                    kanjiDto = new KanjiAdminDto
                    {
                        Id = kanji.Id,
                        Character = kanji.Character,
                        StrokeCount = kanji.StrokeCount,
                        JlptLevel = kanji.JlptLevel,
                        Meaning = kanji.Meaning,
                        Freq = kanji.Freq,
                        Examples = kanji.KanjiExamples?.Select(ex => new KanjiExampleAdminDto
                        {
                            Id = ex.Id,
                            ExampleType = ex.ExampleType,
                            ReadingGroup = ex.ReadingGroup,
                            Word = ex.Word,
                            Meaning = ex.Meaning,
                            Reading = ex.Reading,
                            HanViet = ex.HanViet
                        }).ToList() ?? new()
                    };
                }
            }

            _response.Result = new EntryDetailDto
            {
                Id = entry.Id,
                Label = entry.Label,
                Type = entry.Type,
                ShortMean = entry.ShortMean,
                Phonetic = entry.Phonetic,
                Romaji = entry.Romaji,
                EntryCategory = entry.EntryCategory,
                Weight = entry.Weight,
                Words = entry.Words?.Select(w => new WordAdminDto
                {
                    Id = w.Id,
                    WordText = w.WordText,
                    Phonetic = w.Phonetic,
                    Romaji = w.Romaji,
                    ShortMean = w.ShortMean,
                    Weight = w.Weight,
                    Opposites = w.Relations?
                        .Where(r => r.RelationType == "opposite" && r.RelatedWord != null)
                        .Select(r => new WordRelationAdminDto
                        {
                            Id = r.Id,
                            RelatedWordId = r.RelatedWordId,
                            RelatedWordText = r.RelatedWord.WordText,
                            RelationType = r.RelationType
                        }).ToList() ?? new()
                }).ToList() ?? new(),
                Senses = entry.Senses?.Select(s => new SenseAdminDto
                {
                    Id = s.Id,
                    Pos = s.Pos,
                    SenseOrder = s.SenseOrder,
                    Glosses = s.Glosses?.Select(g => new GlossAdminDto
                    {
                        Id = g.Id,
                        Text = g.Text
                    }).ToList() ?? new(),
                    Examples = s.Examples?.Select(ex => new ExampleAdminDto
                    {
                        Id = ex.Id,
                        ContentJp = ex.ContentJp,
                        ContentTranslated = ex.ContentTranslated,
                        Transcription = ex.Transcription
                    }).ToList() ?? new()
                }).ToList() ?? new(),
                Kanji = kanjiDto,
                ReadingElements = entry.ReadingElements?.Select(r => new ReadingElementAdminDto
                {
                    Id = r.Id,
                    Reb = r.Reb,
                    ReNoKanji = r.ReNoKanji,
                    RePri = r.RePri
                }).ToList() ?? new()
            };
            return Ok(_response);
        }

        // PATCH /api/admin/words/{id}
        [HttpPatch("words/{id}")]
        public async Task<IActionResult> PatchWord(int id, [FromBody] PatchWordDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var word = await db.Words.FindAsync(id);
            if (word == null) return NotFound();

            if (dto.WordText != null) word.WordText = dto.WordText;
            if (dto.Phonetic != null) word.Phonetic = dto.Phonetic;
            if (dto.Romaji != null) word.Romaji = dto.Romaji;
            if (dto.ShortMean != null) word.ShortMean = dto.ShortMean;
            if (dto.Weight.HasValue) word.Weight = dto.Weight;
            word.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật Word.";
            return Ok(_response);
        }

        // PATCH /api/admin/senses/{id}
        [HttpPatch("senses/{id}")]
        public async Task<IActionResult> PatchSense(int id, [FromBody] PatchSenseDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var sense = await db.Senses.FindAsync(id);
            if (sense == null) return NotFound();

            if (dto.Pos != null) sense.Pos = dto.Pos;
            if (dto.SenseOrder.HasValue) sense.SenseOrder = dto.SenseOrder;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật Sense.";
            return Ok(_response);
        }

        // PATCH /api/admin/glosses/{id}
        [HttpPatch("glosses/{id}")]
        public async Task<IActionResult> PatchGloss(int id, [FromBody] PatchGlossDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var gloss = await db.Glosses.FindAsync(id);
            if (gloss == null) return NotFound();

            if (dto.Text != null) gloss.Text = dto.Text;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật Gloss.";
            return Ok(_response);
        }

        // PATCH /api/admin/examples/{id}
        [HttpPatch("examples/{id}")]
        public async Task<IActionResult> PatchExample(int id, [FromBody] PatchExampleDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var ex = await db.Examples.FindAsync(id);
            if (ex == null) return NotFound();

            if (dto.ContentJp != null) ex.ContentJp = dto.ContentJp;
            if (dto.ContentTranslated != null) ex.ContentTranslated = dto.ContentTranslated;
            if (dto.Transcription != null) ex.Transcription = dto.Transcription;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật Example.";
            return Ok(_response);
        }

        // PATCH /api/admin/reading-elements/{id}
        [HttpPatch("reading-elements/{id}")]
        public async Task<IActionResult> PatchReadingElement(int id, [FromBody] PatchReadingElementDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var re = await db.ReadingElements.FindAsync(id);
            if (re == null) return NotFound();

            if (dto.Reb != null) re.Reb = dto.Reb;
            if (dto.ReNoKanji != null) re.ReNoKanji = dto.ReNoKanji;
            if (dto.RePri != null) re.RePri = dto.RePri;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật ReadingElement.";
            return Ok(_response);
        }

        // PATCH /api/admin/kanji/{id}
        [HttpPatch("kanji/{id}")]
        public async Task<IActionResult> PatchKanji(int id, [FromBody] PatchKanjiDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var kanji = await db.Kanji.FindAsync(id);
            if (kanji == null) return NotFound();

            if (dto.StrokeCount.HasValue) kanji.StrokeCount = dto.StrokeCount;
            if (dto.JlptLevel != null) kanji.JlptLevel = dto.JlptLevel;
            if (dto.Meaning != null) kanji.Meaning = dto.Meaning;
            if (dto.Freq.HasValue) kanji.Freq = dto.Freq;
            kanji.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật Kanji.";
            return Ok(_response);
        }

        // PATCH /api/admin/kanji-examples/{id}
        [HttpPatch("kanji-examples/{id}")]
        public async Task<IActionResult> PatchKanjiExample(int id, [FromBody] PatchKanjiExampleDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var ex = await db.KanjiExamples.FindAsync(id);
            if (ex == null) return NotFound();

            if (dto.ExampleType != null) ex.ExampleType = dto.ExampleType;
            if (dto.ReadingGroup != null) ex.ReadingGroup = dto.ReadingGroup;
            if (dto.Word != null) ex.Word = dto.Word;
            if (dto.Meaning != null) ex.Meaning = dto.Meaning;
            if (dto.Reading != null) ex.Reading = dto.Reading;
            if (dto.HanViet != null) ex.HanViet = dto.HanViet;

            await db.SaveChangesAsync();
            _response.Message = "Đã cập nhật KanjiExample.";
            return Ok(_response);
        }

        // POST /api/admin/entries/{id}/rebuild — rebuild RawJson từ bảng con
        [HttpPost("entries/{id}/rebuild")]
        public async Task<IActionResult> RebuildEntry(int id,
            [FromServices] Dict.Data.ApplicationDbContext db = null!,
            [FromServices] IJsonBuilderService jsonBuilder = null!,
            [FromServices] IWordService wordService = null!,
            [FromServices] Dict.Models.KanjiCache kanjiCache = null!)
        {
            var entry = await db.Entries.FindAsync(id);
            if (entry == null) return NotFound();

            string newJson;
            if (entry.Type == "kanji")
            {
                newJson = await jsonBuilder.RebuildJsonForKanjiAsync(entry.Label);
                if (!string.IsNullOrEmpty(newJson))
                {
                    // Cập nhật DB
                    entry.RawJson = newJson;
                    entry.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    // Cập nhật KanjiCache trong RAM ngay lập tức
                    if (kanjiCache != null && !string.IsNullOrEmpty(entry.Label))
                        kanjiCache.Data[entry.Label[0]] = newJson;
                }
            }
            else
            {
                newJson = await jsonBuilder.RebuildJsonForWordAsync(entry.Label);
                if (!string.IsNullOrEmpty(newJson))
                    // UpsertCacheForLabelAsync đã tự invalidate MemoryCache
                    await wordService.UpsertCacheForLabelAsync(entry.Label, newJson, "Admin_Rebuild");
            }

            if (string.IsNullOrEmpty(newJson))
            {
                _response.IsSuccess = false;
                _response.Message = "Rebuild thất bại — không tìm thấy dữ liệu.";
                return Ok(_response);
            }

            _response.Message = $"Đã rebuild + invalidate cache cho '{entry.Label}'.";
            return Ok(_response);
        }

        // ===== ADD MỚI BẢNG CON =====

        // POST /api/admin/senses/{senseId}/examples
        [HttpPost("senses/{senseId}/examples")]
        public async Task<IActionResult> AddExample(int senseId, [FromBody] PatchExampleDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var sense = await db.Senses.FindAsync(senseId);
            if (sense == null) return NotFound("Sense không tồn tại.");

            var ex = new Dict.Models.Example
            {
                SenseId = senseId,
                ContentJp = dto.ContentJp ?? "",
                ContentTranslated = dto.ContentTranslated ?? "",
                Transcription = dto.Transcription ?? ""
            };
            db.Examples.Add(ex);
            await db.SaveChangesAsync();

            _response.Message = "Đã thêm Example mới.";
            _response.Result = new ExampleAdminDto { Id = ex.Id, ContentJp = ex.ContentJp, ContentTranslated = ex.ContentTranslated, Transcription = ex.Transcription };
            return Ok(_response);
        }

        // POST /api/admin/senses/{senseId}/glosses
        [HttpPost("senses/{senseId}/glosses")]
        public async Task<IActionResult> AddGloss(int senseId, [FromBody] PatchGlossDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var sense = await db.Senses.FindAsync(senseId);
            if (sense == null) return NotFound("Sense không tồn tại.");

            var gloss = new Dict.Models.Gloss
            {
                SenseId = senseId,
                Text = dto.Text ?? ""
            };
            db.Glosses.Add(gloss);
            await db.SaveChangesAsync();

            _response.Message = "Đã thêm Gloss mới.";
            _response.Result = new GlossAdminDto { Id = gloss.Id, Text = gloss.Text };
            return Ok(_response);
        }

        // POST /api/admin/entries/{entryId}/senses
        [HttpPost("entries/{entryId}/senses")]
        public async Task<IActionResult> AddSense(int entryId, [FromBody] PatchSenseDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var entry = await db.Entries.FindAsync(entryId);
            if (entry == null) return NotFound("Entry không tồn tại.");

            // auto-assign order = max + 1
            var maxOrder = await db.Senses.Where(s => s.EntryId == entryId).MaxAsync(s => (int?)s.SenseOrder) ?? 0;
            var sense = new Dict.Models.Sense
            {
                EntryId = entryId,
                Pos = dto.Pos ?? "",
                SenseOrder = dto.SenseOrder ?? (maxOrder + 1)
            };
            db.Senses.Add(sense);
            await db.SaveChangesAsync();

            _response.Message = "Đã thêm Sense mới.";
            _response.Result = new SenseAdminDto { Id = sense.Id, Pos = sense.Pos, SenseOrder = sense.SenseOrder, Glosses = new(), Examples = new() };
            return Ok(_response);
        }

        // POST /api/admin/entries/{entryId}/reading-elements
        [HttpPost("entries/{entryId}/reading-elements")]
        public async Task<IActionResult> AddReadingElement(int entryId, [FromBody] PatchReadingElementDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var entry = await db.Entries.FindAsync(entryId);
            if (entry == null) return NotFound();

            var re = new Dict.Models.ReadingElement
            {
                EntryId = entryId,
                Reb = dto.Reb ?? "",
                ReNoKanji = dto.ReNoKanji,
                RePri = dto.RePri
            };
            db.ReadingElements.Add(re);
            await db.SaveChangesAsync();

            _response.Message = "Đã thêm ReadingElement mới.";
            _response.Result = new ReadingElementAdminDto { Id = re.Id, Reb = re.Reb };
            return Ok(_response);
        }

        // POST /api/admin/kanji/{kanjiId}/examples
        [HttpPost("kanji/{kanjiId}/examples")]
        public async Task<IActionResult> AddKanjiExample(int kanjiId, [FromBody] PatchKanjiExampleDto dto,
            [FromServices] Dict.Data.ApplicationDbContext db = null!)
        {
            var kanji = await db.Kanji.FindAsync(kanjiId);
            if (kanji == null) return NotFound();

            var ex = new Dict.Models.KanjiExample
            {
                KanjiId = kanjiId,
                ExampleType = dto.ExampleType ?? "on",
                ReadingGroup = dto.ReadingGroup,
                Word = dto.Word ?? "",
                Meaning = dto.Meaning ?? "",
                Reading = dto.Reading ?? "",
                HanViet = dto.HanViet
            };
            db.KanjiExamples.Add(ex);
            await db.SaveChangesAsync();

            _response.Message = "Đã thêm KanjiExample mới.";
            _response.Result = new KanjiExampleAdminDto { Id = ex.Id, ExampleType = ex.ExampleType, Word = ex.Word, Meaning = ex.Meaning, Reading = ex.Reading };
            return Ok(_response);
        }

        // POST /api/admin/entries/rebuild-batch?type=word&limit=100 — rebuild hàng loạt
        [HttpPost("entries/rebuild-batch")]
        public async Task<IActionResult> RebuildBatch(
            [FromQuery] string type = "word",
            [FromQuery] int limit = 100,
            [FromServices] Dict.Data.ApplicationDbContext db = null!,
            [FromServices] IJsonBuilderService jsonBuilder = null!,
            [FromServices] IWordService wordService = null!,
            [FromServices] Dict.Models.KanjiCache kanjiCache = null!)
        {
            var entries = await db.Entries
                .Where(e => e.Type == type)
                .OrderBy(e => e.UpdatedAt ?? e.CreatedAt)
                .Take(limit)
                .Select(e => new { e.Id, e.Label })
                .ToListAsync();

            int success = 0, failed = 0;

            foreach (var e in entries)
            {
                try
                {
                    if (type == "kanji")
                    {
                        var json = await jsonBuilder.RebuildJsonForKanjiAsync(e.Label);
                        if (!string.IsNullOrEmpty(json))
                        {
                            var entry = await db.Entries.FindAsync(e.Id);
                            if (entry != null) { entry.RawJson = json; entry.UpdatedAt = DateTime.UtcNow; }
                            if (kanjiCache != null && !string.IsNullOrEmpty(e.Label))
                                kanjiCache.Data[e.Label[0]] = json;
                            success++;
                        }
                        else failed++;
                    }
                    else
                    {
                        var json = await jsonBuilder.RebuildJsonForWordAsync(e.Label);
                        if (!string.IsNullOrEmpty(json))
                        {
                            // UpsertCacheForLabelAsync tự invalidate MemoryCache
                            await wordService.UpsertCacheForLabelAsync(e.Label, json, "Batch_Rebuild");
                            success++;
                        }
                        else failed++;
                    }
                }
                catch { failed++; }
            }

            if (type == "kanji") await db.SaveChangesAsync(); // batch save kanji

            _response.Message = $"Rebuild xong: {success} thành công, {failed} thất bại. Cache đã invalidate.";
            _response.Result = new { success, failed, total = entries.Count };
            return Ok(_response);
        }

        // POST /api/admin/reload-trie — rebuild Trie + KanjiCache ngay lập tức
        [HttpPost("reload-trie")]
        [Microsoft.AspNetCore.Http.Timeouts.RequestTimeout(300)]  // 5 phút
        public async Task<IActionResult> ReloadTrie(
            [FromServices] TrieLoaderService trieLoader)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var (wordCount, kanjiCount, ms) = await trieLoader.ReloadAsync();
                _response.Message = $"Trie reload thành công: {wordCount:N0} words, {kanjiCount:N0} kanji, mất {ms}ms.";
                _response.Result = new { wordCount, kanjiCount, ms };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = $"Reload thất bại: {ex.Message}";
                return StatusCode(500, _response);
            }
        }
    }

    // Bạn cần một DTO mới cho việc cập nhật nhiều Role
    public class AdminUpdateUserRolesDto
    {
        [Required]
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}