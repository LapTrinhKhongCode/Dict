using Dict.DTO;
using Dict.DTO.Admin;
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
    } 

    // Bạn cần một DTO mới cho việc cập nhật nhiều Role
    public class AdminUpdateUserRolesDto
    {
        [Required]
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}