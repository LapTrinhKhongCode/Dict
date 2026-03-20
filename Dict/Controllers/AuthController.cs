using Dict.DTO.Auth;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Dict.DTO;
using Microsoft.AspNetCore.Authorization;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ResponseDTO _response;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _response = new ResponseDTO();
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                // Dòng này sẽ được kích hoạt nếu token không hợp lệ hoặc không chứa userId,
                // mặc dù [Authorize] thường sẽ chặn các request này trước.
                throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
            }
            return userId;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegistrationRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid registration data.";
                _response.Result = ModelState;
                return BadRequest(_response);
            }

            try
            {
                // Giả sử RegisterAsync trả về một đối tượng UserDto hoặc User
                var message = await _authService.RegisterAsync(request);
                _response.Message = message;
                _response.IsSuccess = true;
                return Ok(_response); // Trả về 200 OK với tin nhắn
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message; // Ví dụ: "Username already exists."
                // Sử dụng 400 Bad Request hoặc 409 Conflict cho lỗi đăng ký
                return BadRequest(_response);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid login data.";
                return BadRequest(_response);
            }

            try
            {
                // Giả sử LoginAsync trả về một LoginResponseDto (chứa token và thông tin user)
                var loginResponse = await _authService.LoginAsync(request);

                _response.Result = loginResponse;
                _response.Message = "Login successful.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message; // Ví dụ: "Invalid username or password."
                // Sử dụng 401 Unauthorized cho lỗi đăng nhập sai
                return Unauthorized(_response);
            }
        }
        [HttpPost("logout")]
        [Authorize] // User must be logged in to log out
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetUserId();
                await _authService.LogoutAsync(userId);

                _response.IsSuccess = true;
                _response.Message = "Logout successful. Please clear your token.";
                return Ok(_response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Unauthorized(_response);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error during logout for user {UserId}", GetUserId()); // Use GetUserId carefully in catch block if it can fail

                _response.IsSuccess = false;
                _response.Message = "An error occurred during logout.";
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
        [HttpPost("confirm-registration")]
        public async Task<IActionResult> ConfirmRegistration(ConfirmRegistrationDto dto)
        {
            if (string.IsNullOrEmpty(dto.Token))
            {
                _response.IsSuccess = false;
                _response.Message = "Token is required.";
                return BadRequest(_response);
            }

            try
            {
                // Gọi hàm service mới
                var loginResponse = await _authService.ConfirmRegistrationAsync(dto.Token);

                _response.Result = loginResponse; // Trả về token đăng nhập
                _response.Message = "Tạo tài khoản thành công và đã đăng nhập.";
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (InvalidOperationException opEx) // Lỗi do token hết hạn/sai
            {
                _response.IsSuccess = false;
                _response.Message = opEx.Message;
                return BadRequest(_response);
            }
            catch (Exception ex) // Lỗi hệ thống
            {
                _logger.LogError(ex, "Lỗi khi xác nhận đăng ký với token {Token}", dto.Token);
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi hệ thống.";
                return StatusCode(500, _response);
            }
        }
    }
}