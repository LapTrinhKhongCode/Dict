using Dict.DTO;
using Dict.DTO.Admin;
// using Dict.Models.Enum; // <-- XÓA
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ✨ SỬA: Dùng string "Admin"
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ResponseDTO _response;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _response = new ResponseDTO();
            _logger = logger;
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
    }

    // Bạn cần một DTO mới cho việc cập nhật nhiều Role
    public class AdminUpdateUserRolesDto
    {
        [Required]
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}