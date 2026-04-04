using Dict.DTO;
using Dict.DTO.User;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ResponseDTO _response;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        public UsersController(IUserService userService, IMemoryCache cache, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userService = userService;
            _response = new ResponseDTO();
             _cache = cache;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: api/users
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                _response.Result = users;
                _response.Message = "Successfully retrieved all users.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                // Trả về lỗi 500 Internal Server Error
                return StatusCode(500, _response);
            }
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found.";
                    return NotFound(_response); // 404 Not Found
                }
                _response.Result = user;
                _response.Message = "Successfully retrieved the user.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // POST: api/users
       
       

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found.";
                    return NotFound(_response);
                }
                _response.Message = "User deleted successfully.";
                return Ok(_response); // Thay vì NoContent(), trả về Ok() với thông điệp
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
      
            try
            {
                var result = await _userService.ChangePasswordAsync(request.Username, request.OldPassword, request.NewPassword);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found.";
                    return NotFound(_response);
                }
                _response.Message = "User deleted successfully.";
                return Ok(_response); 
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        [HttpGet("search/{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchUsers(string username)
        {
            try
            {
                var users = await _userService.SearchUsersByUsernameAsync(username);
                _response.Result = users;
                _response.Message = $"Successfully found users matching '{username}'.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        [HttpPut("by-username/{username}")]
        public async Task<IActionResult> UpdateUserByUsername([FromRoute] string username, [FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid input data.";
                return BadRequest(_response);
            }

            try
            {
                var updated = await _userService.UpdateUserByUsernameAsync(username, updateUserDto);
                if (updated == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found or update failed.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "User updated successfully.";
                _response.Result = updated;
                return Ok(_response);
            }
            catch (InvalidOperationException ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Conflict(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid input data.";
                return BadRequest(_response);
            }

            try
            {
                var updated = await _userService.UpdateUserAsync(id, updateUserDto);
                if (updated == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found or update failed.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "User updated successfully.";
                _response.Result = updated;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
            return userId;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = GetUserId();
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Result = user;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid input data.";
                return BadRequest(_response);
            }

            try
            {
                var userId = GetUserId();
                var updated = await _userService.UpdateUserAsync(userId, updateUserDto);
                if (updated == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found or update failed.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "User updated successfully.";
                _response.Result = updated;
                return Ok(_response);
            }
            catch (InvalidOperationException ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Conflict(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        [HttpPost("send-email-otp")]
        public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpDto dto)
        {
            var currentUserId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            // 1. Kiểm tra email mới đã có ai dùng chưa
            var existingUser = await _userManager.FindByEmailAsync(dto.NewEmail);
            if (existingUser != null)
            {
                return BadRequest(new { isSuccess = false, message = "Email này đã được sử dụng bởi người khác." });
            }

            // 2. Tạo mã OTP 6 số
            string otpCode = new Random().Next(100000, 999999).ToString();

            // 3. Lưu OTP vào Cache với thời hạn 5 phút (Key: OTP_UserId_NewEmail)
            string cacheKey = $"OTP_{currentUserId}_{dto.NewEmail}";
            _cache.Set(cacheKey, otpCode, TimeSpan.FromMinutes(5));

            // 4. Gửi Email (Bạn thay bằng hàm gửi mail thực tế của dự án nhé)
            await _emailService.SendEmailAsync(dto.NewEmail, "Mã xác nhận đổi Email", $"Mã OTP của bạn là: {otpCode}. Mã có hiệu lực trong 5 phút.");
            Console.WriteLine($"[DEBUG] OTP cho {dto.NewEmail} là: {otpCode}");

            return Ok(new { isSuccess = true, message = "Đã gửi mã OTP." });
        }

        [HttpPost("verify-email-otp")]
        public IActionResult VerifyEmailOtp([FromBody] VerifyEmailOtpDto dto)
        {
            var currentUserId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            string cacheKey = $"OTP_{currentUserId}_{dto.NewEmail}";

            // 1. Kiểm tra OTP trong Cache
            if (_cache.TryGetValue(cacheKey, out string? savedOtp))
            {
                if (savedOtp == dto.Otp)
                {
                    // 2. Nếu đúng -> Xóa OTP cũ, cấp 1 cờ "Đã xác thực" lưu trong 15 phút
                    _cache.Remove(cacheKey);
                    _cache.Set($"VerifiedEmail_{currentUserId}", dto.NewEmail, TimeSpan.FromMinutes(15));

                    return Ok(new { isSuccess = true, message = "Xác nhận OTP thành công." });
                }
            }

            return BadRequest(new { isSuccess = false, message = "Mã OTP không chính xác hoặc đã hết hạn." });
        }
    }
}
