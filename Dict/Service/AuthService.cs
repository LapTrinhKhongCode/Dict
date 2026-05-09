using System.Text.RegularExpressions;
using Dict.Data;
using Dict.DTO;
using Dict.DTO.Auth;
using Dict.Models;
using Dict.Models.Enum;
using Dict.Service.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dict.Service
{
    // Class nhỏ dùng để lưu trữ Cache cho OTP an toàn
    public class OtpCacheEntry
    {
        public string OtpCode { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IMemoryCache _cache;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtService jwtService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
        }

        // ==========================================
        // 1. ĐĂNG KÝ (GỬI LINK XÁC NHẬN EMAIL)
        // ==========================================
        public async Task<string> RegisterAsync(RegistrationRequestDto request)
        {
            var passwordError = ValidatePassword(request.Password);
            if (!string.IsNullOrEmpty(passwordError))
                throw new InvalidOperationException(passwordError);

            if (string.IsNullOrEmpty(request.Username) || !Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]+$"))
                throw new InvalidOperationException("Username chỉ được chứa chữ cái, số hoặc dấu gạch dưới.");

            if (await _userManager.FindByEmailAsync(request.Email) != null)
                throw new InvalidOperationException("Email already exists.");

            if (await _userManager.FindByNameAsync(request.Username) != null)
                throw new InvalidOperationException("Username already exists.");

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = false,
                AvatarUrl = "/images/default_ava.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            // Tạo User trong DB
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _userManager.AddToRoleAsync(user, "USER");

            // TẠO TOKEN XÁC NHẬN
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontendUrl = _configuration["FrontendUrl"];
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationUrl = $"{frontendUrl}/confirm-account?email={user.Email}&token={encodedToken}";

            var subject = "Chào mừng bạn - Xác nhận tài khoản";
            var body = $"<p>Vui lòng nhấp vào link sau để xác nhận email của bạn:</p><a href='{confirmationUrl}'>Xác nhận Email</a>";

            // 🛑 THAY ĐỔI QUAN TRỌNG: Không dùng Task.Run nữa. 
            // Chúng ta phải chờ gửi mail xong mới báo kết quả về cho người dùng.
            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                // Nếu gửi mail lỗi, ta XÓA LUÔN user vừa tạo để họ có thể đăng ký lại bằng email đúng
                await _userManager.DeleteAsync(user);

                _logger.LogError(ex, "Lỗi gửi email đăng ký cho {Email}", user.Email);

                // Ném lỗi về cho Controller để Controller trả về Bad Request (400) cho FE
                throw new InvalidOperationException("Không thể gửi email xác nhận. Vui lòng kiểm tra lại địa chỉ Email hoặc thử lại sau.");
            }

            return "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.";
        }

        // ==========================================
        // 2. XÁC NHẬN EMAIL
        // ==========================================
        public async Task<LoginResponseDto> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("Email không tồn tại.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                throw new InvalidOperationException("Link xác nhận không hợp lệ hoặc đã hết hạn.");

            var roles = await _userManager.GetRolesAsync(user);
            var loginToken = _jwtService.GenerateToken(user, roles);

            return new LoginResponseDto
            {
                Token = loginToken,
                Username = user.UserName,
                Email = user.Email,
                Role = roles.FirstOrDefault(),
                AvatarUrl = user.AvatarUrl,
                UserId = user.Id
            };
        }

        // ==========================================
        // 3. QUÊN MẬT KHẨU (GỬI MÃ OTP)
        // ==========================================
        public async Task<string> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return "Nếu email hợp lệ, mã OTP khôi phục đã được gửi.";
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var cacheEntry = new OtpCacheEntry { OtpCode = otpCode, ResetToken = resetToken };
            _cache.Set($"OTP_{email}", cacheEntry, TimeSpan.FromMinutes(10));

            var subject = "Mã xác nhận quên mật khẩu ";
            var body = $"<p>Mã xác nhận (OTP) để đặt lại mật khẩu của bạn là: <strong style='font-size:24px;'>{otpCode}</strong></p>" +
                       $"<p>Mã này có hiệu lực trong 10 phút. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>";

            // 💡 CHẠY NGẦM VIỆC GỬI EMAIL
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email quên mật khẩu cho {Email}", email);
                }
            });

            return "Nếu email hợp lệ, mã OTP khôi phục đã được gửi.";
        }

        // ==========================================
        // 4. ĐẶT LẠI MẬT KHẨU (Dùng OTP)
        // ==========================================
        public async Task<string> ResetPasswordAsync(string email, string otp, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
                throw new InvalidOperationException("Mật khẩu xác nhận không khớp.");

            var passwordError = ValidatePassword(newPassword);
            if (!string.IsNullOrEmpty(passwordError))
                throw new InvalidOperationException(passwordError);

            if (!_cache.TryGetValue($"OTP_{email}", out OtpCacheEntry cacheEntry))
            {
                throw new InvalidOperationException("Mã OTP đã hết hạn hoặc không tồn tại. Vui lòng yêu cầu lại.");
            }

            if (cacheEntry.OtpCode != otp)
            {
                throw new InvalidOperationException("Mã OTP không chính xác.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("Lỗi hệ thống, không tìm thấy người dùng.");

            var result = await _userManager.ResetPasswordAsync(user, cacheEntry.ResetToken, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Lỗi đặt lại mật khẩu: {errors}");
            }

            _cache.Remove($"OTP_{email}");

            return "Đổi mật khẩu thành công. Bây giờ bạn có thể đăng nhập bằng mật khẩu mới.";
        }

        // ==========================================
        // 5. ĐĂNG NHẬP & ĐĂNG XUẤT
        // ==========================================
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
                throw new Exception("Tên đăng nhập hoặc mật khẩu không chính xác.");

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                throw new Exception("Vui lòng kiểm tra email và xác nhận tài khoản trước khi đăng nhập.");
            }

            if (!user.IsActive)
                throw new Exception("Tài khoản này đã bị khóa.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                throw new Exception("Tên đăng nhập hoặc mật khẩu không chính xác.");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            return new LoginResponseDto
            {
                Token = token,
                Username = user.UserName,
                Email = user.Email,
                Role = roles.FirstOrDefault(),
                AvatarUrl = user.AvatarUrl,
                UserId = user.Id
            };
        }

        public async Task LogoutAsync(int userId)
        {
            _logger.LogInformation("User {UserId} logged out at {Timestamp}", userId, DateTime.UtcNow);
            await Task.CompletedTask;
        }

        private string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return "Mật khẩu không được để trống.";
            if (password.Length < 6) return "Mật khẩu phải dài ít nhất 6 ký tự.";
            return null;
        }
    }
}