using Dict.DTO.Auth;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Dict.DTO;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ResponseDTO _response; 

        public AuthController(IAuthService authService)
        {
            _authService = authService;
            _response = new ResponseDTO();
        }
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailDto verifyDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid input.";
                return BadRequest(_response);
            }

            try
            {
                // Trả về token đăng nhập
                var loginResponse = await _authService.VerifyEmailAsync(verifyDto);
                _response.Result = loginResponse;
                _response.Message = "Email verified successfully. You are now logged in.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }
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
    }
}