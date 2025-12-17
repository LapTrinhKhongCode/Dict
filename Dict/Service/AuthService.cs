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
using static System.Net.WebRequestMethods;
// 1. THÊM CÁC DỊCH VỤ IDENTITY
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Cần cho List

namespace Dict.Service
{
    public class AuthService : IAuthService
    {
        // 2. XÓA BỎ DbContext, THÊM CÁC MANAGER CỦA IDENTITY
        // private readonly ApplicationDbContext _context; // <-- XÓA
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IMemoryCache _cache;

        // 3. CẬP NHẬT HÀM KHỞI TẠO (CONSTRUCTOR)
        public AuthService(
            // ApplicationDbContext context, // <-- XÓA
            UserManager<ApplicationUser> userManager, // THÊM
            SignInManager<ApplicationUser> signInManager, // THÊM
            RoleManager<ApplicationRole> roleManager, // THÊM
            IJwtService jwtService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IMemoryCache cache)
        {
            // _context = context; // <-- XÓA
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> RegisterAsync(RegistrationRequestDto request)
        {
            var passwordError = ValidatePassword(request.Password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                throw new InvalidOperationException(passwordError);
            }

            if (string.IsNullOrEmpty(request.Username) || !Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]+$"))
                throw new InvalidOperationException("Username chỉ được chứa chữ cái, số hoặc dấu gạch dưới, không có khoảng trắng hoặc dấu.");

            // << --- CÁC DÒNG CODE CŨ (DÙNG _context) ĐÃ ĐƯỢC XÓA Ở ĐÂY --- >>

            // 4. KIỂM TRA BẰNG USERNMANAGER (Code đúng)
            var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }
            var existingUserByName = await _userManager.FindByNameAsync(request.Username);
            if (existingUserByName != null)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            // 5. KHÔNG HASH MẬT KHẨU NỮA
            // var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password); // <-- XÓA

            var confirmationToken = Guid.NewGuid().ToString();

            // 6. LƯU MẬT KHẨU GỐC (RAW) VÀO CACHE
            // (Vì UserManager cần mật khẩu gốc để hash)
            // var cacheEntry = new { RequestData = request, HashedPassword = hashedPassword }; // <-- SỬA
            var cacheEntry = new { RequestData = request }; // Chỉ cần lưu request
            _cache.Set(confirmationToken, cacheEntry, TimeSpan.FromMinutes(30));

            // Gửi email (giữ nguyên)
            var frontendUrl = _configuration["FrontendUrl"];
            if (string.IsNullOrEmpty(frontendUrl))
            {
                throw new Exception("FrontendUrl is not configured");
            }
            var confirmationUrl = $"{frontendUrl}/confirm-account?token={confirmationToken}";
            var subject = "Chào mừng bạn đến với Miyo Dictionary - Xác nhận tài khoản";
            var body = $"Cảm ơn bạn đã đăng ký. Vui lòng nhấp vào link sau để hoàn tất tạo tài khoản:<br/>" +
                       $"<a href='{confirmationUrl}'>Click để Xác nhận Tài khoản</a>" +
                       $"<br/><br/>Link này sẽ hết hạn sau 30 phút.";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return "Registration successful. Please check your email to confirm your account.";
        }

        public async Task<LoginResponseDto> ConfirmRegistrationAsync(string token)
        {
            if (!_cache.TryGetValue(token, out var cacheEntry))
            {
                throw new InvalidOperationException("Link xác thực không hợp lệ, đã hết hạn hoặc không tồn tại.");
            }

            // 7. LẤY DỮ LIỆU TỪ CACHE
            var requestData = (RegistrationRequestDto)cacheEntry.GetType().GetProperty("RequestData").GetValue(cacheEntry, null);
            // var hashedPassword = (string)cacheEntry.GetType().GetProperty("HashedPassword").GetValue(cacheEntry, null); // <-- KHÔNG CẦN NỮA

            // 8. KIỂM TRA LẠI (GIỮ NGUYÊN, NHƯNG DÙNG USERNMANAGER)
            if (await _userManager.FindByNameAsync(requestData.Username) != null ||
                await _userManager.FindByEmailAsync(requestData.Email) != null)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            // 9. TẠO USER (NHƯNG CHƯA CÓ MẬT KHẨU)
            var user = new ApplicationUser
            {
                UserName = requestData.Username,
                Email = requestData.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                AvatarUrl = "https://ocrr.blob.core.windows.net/avatars/523d68a2-cc64-4537-b8b0-dbf9d40b26a8.png",
                // KHÔNG SET PASSWORD HASH Ở ĐÂY
            };

            // 10. DÙNG USERNMANAGER ĐỂ TẠO USER (QUAN TRỌNG NHẤT)
            // Lệnh này sẽ tự động HASH mật khẩu (requestData.Password) và lưu user
            var result = await _userManager.CreateAsync(user, requestData.Password);

            if (!result.Succeeded)
            {
                // Nếu thất bại, ném lỗi
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            // 11. GÁN VAI TRÒ (ROLE) CHO USER
            // (Đảm bảo bạn đã có Role "User" trong DB)
            var roleResult = await _userManager.AddToRoleAsync(user, "USER"); // Gán vào biến mới

            if (!roleResult.Succeeded) // Kiểm tra kết quả
            {
                // Nếu thất bại, ném lỗi
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign role: {errors}");
            }
            _cache.Remove(token);

            // 12. TẠO TOKEN ĐĂNG NHẬP (LẤY CẢ ROLE)
            var roles = await _userManager.GetRolesAsync(user);
            var loginToken = _jwtService.GenerateToken(user, roles); // Truyền roles vào

            return new LoginResponseDto
            {
                Token = loginToken,
                Username = user.UserName,
                Email = user.Email,
                Role = roles.FirstOrDefault(), // Trả về vai trò
                AvatarUrl = user.AvatarUrl,
                UserId = user.Id
            };
        }
        private string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) { return "Password is required."; }
            if (password.Length < 6) { return "Password must be at least 6 characters long."; }
            // (Các kiểm tra khác...)
            return null;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 13. DÙNG USERNMANAGER ĐỂ TÌM USER
            var user = await _userManager.FindByNameAsync(request.Username);

            if (user == null)
            {
                throw new Exception("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // 14. DÙNG SIGNINMANAGER ĐỂ KIỂM TRA MẬT KHẨU
            // (Nó sẽ tự động so sánh hash)
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false); // false = không khóa tài khoản nếu sai

            if (!result.Succeeded)
            {
                throw new Exception("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            if (!user.IsActive)
            {
                throw new Exception("Tài khoản này đã bị khóa.");
            }

            // 15. LẤY ROLE VÀ TẠO TOKEN
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            return new LoginResponseDto
            {
                Token = token,
                Username = user.UserName,
                Email = user.Email,
                Role = roles.FirstOrDefault(), // Trả về vai trò
                AvatarUrl = user.AvatarUrl,
                UserId = user.Id
            };
        }
        public async Task LogoutAsync(int userId)
        {
            _logger.LogInformation("User {UserId} logged out at {Timestamp}", userId, DateTime.UtcNow);
            await Task.CompletedTask;
        }
    }
}