using Dict.DTO.Auth;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Dict.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
                _response.Message = "Dữ liệu đăng ký không hợp lệ.";
                return BadRequest(_response);
            }

            try
            {
                var message = await _authService.RegisterAsync(request);
                _response.Message = message;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Dữ liệu đăng nhập không hợp lệ.";
                return BadRequest(_response);
            }

            try
            {
                var loginResponse = await _authService.LoginAsync(request);
                _response.Result = loginResponse;
                _response.Message = "Đăng nhập thành công.";
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Unauthorized(_response);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetUserId();
                await _authService.LogoutAsync(userId);
                _response.IsSuccess = true;
                _response.Message = "Đăng xuất thành công.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", GetUserId());
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi khi đăng xuất.";
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto dto)
        {
            if (string.IsNullOrEmpty(dto.Token) || string.IsNullOrEmpty(dto.Email))
            {
                _response.IsSuccess = false;
                _response.Message = "Email và Token là bắt buộc.";
                return BadRequest(_response);
            }

            try
            {
                var loginResponse = await _authService.ConfirmEmailAsync(dto.Email, dto.Token);
                _response.Result = loginResponse;
                _response.Message = "Xác nhận email thành công. Bạn đã được đăng nhập.";
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (InvalidOperationException opEx)
            {
                _response.IsSuccess = false;
                _response.Message = opEx.Message;
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi xác nhận email {Email}", dto.Email);
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi hệ thống.";
                return StatusCode(500, _response);
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
            {
                _response.IsSuccess = false;
                _response.Message = "Email là bắt buộc.";
                return BadRequest(_response);
            }

            try
            {
                var message = await _authService.ForgotPasswordAsync(dto.Email);
                _response.Message = message;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý quên mật khẩu cho email {Email}", dto.Email);
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi hệ thống.";
                return StatusCode(500, _response);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Dữ liệu không hợp lệ.";
                return BadRequest(_response);
            }

            try
            {
                var message = await _authService.ResetPasswordAsync(dto.Email, dto.Otp, dto.NewPassword, dto.ConfirmPassword);
                _response.Message = message;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (InvalidOperationException opEx)
            {
                _response.IsSuccess = false;
                _response.Message = opEx.Message;
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reset mật khẩu cho email {Email}", dto.Email);
                _response.IsSuccess = false;
                _response.Message = "Đã xảy ra lỗi hệ thống.";
                return StatusCode(500, _response);
            }
        }
    }
}