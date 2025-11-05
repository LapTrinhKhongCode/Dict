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

namespace Dict.Service
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IMemoryCache _cache; // ✨ 2. Thêm IMemoryCache

        public AuthService(
            ApplicationDbContext context,
            IJwtService jwtService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IMemoryCache cache) // ✨ 3. Inject IMemoryCache
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _cache = cache; // ✨ 4. Lưu lại
        }

        // ✨ THAY ĐỔI: Hàm RegisterAsync
        public async Task<string> RegisterAsync(RegistrationRequestDto request)
        {
            var passwordError = ValidatePassword(request.Password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                throw new InvalidOperationException(passwordError);
            }

            if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]+$"))
                throw new InvalidOperationException("Username chỉ được chứa chữ cái, số hoặc dấu gạch dưới, không có khoảng trắng hoặc dấu.");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hashedPassword,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Role = Role.USER,
                AvatarUrl = "https://ocrr.blob.core.windows.net/avatars/17cbb928-af2d-4d11-9397-102f8d3d332f.png",
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return "Registration successful.";
        }

        // ✨ HÀM MỚI: Dùng để xác nhận
        public async Task<LoginResponseDto> ConfirmRegistrationAsync(string token)
        {
            // BƯỚC 1: Lấy thông tin từ cache
            if (!_cache.TryGetValue(token, out var cacheEntry))
            {
                throw new InvalidOperationException("Link xác thực không hợp lệ, đã hết hạn hoặc không tồn tại.");
            }

            // BƯỚC 2: Ép kiểu dữ liệu từ cache
            // (Chúng ta phải dùng dynamic hoặc một class/struct nội bộ)
            var requestData = (RegistrationRequestDto)cacheEntry.GetType().GetProperty("RequestData").GetValue(cacheEntry, null);
            var hashedPassword = (string)cacheEntry.GetType().GetProperty("HashedPassword").GetValue(cacheEntry, null);

            // BƯỚC 3: Kiểm tra lại (đề phòng 2 người cùng đăng ký 1 lúc)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == requestData.Username || u.Email == requestData.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            // BƯỚC 4: ✨ TẠO USER CHÍNH THỨC
            var user = new User
            {
                Username = requestData.Username,
                Email = requestData.Email,
                PasswordHash = hashedPassword,
                IsActive = true, // Kích hoạt ngay
                CreatedAt = DateTime.UtcNow,
                Role = Role.USER,
                AvatarUrl = "https://ocrr.blob.core.windows.net/avatars/17cbb928-af2d-4d11-9397-102f8d3d332f.png",
            };

            // BƯỚC 5: Lưu vào DB
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // BƯỚC 6: Xóa cache
            _cache.Remove(token);

            // BƯỚC 7: Trả về token ĐĂNG NHẬP
            var loginToken = _jwtService.GenerateToken(user);
            return new LoginResponseDto
            {
                Token = loginToken,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl,
                UserId = user.Id
            };
        }
        private string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) { return "Password is required."; }
            if (password.Length < 8) { return "Password must be at least 8 characters long."; }
            // (Các kiểm tra khác...)
            return null;
        }

        // ✨ HÀM LOGIN (BẰNG PASSWORD) VẪN HOẠT ĐỘNG BÌNH THƯỜNG
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // (Giữ nguyên không thay đổi)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            if (!user.IsActive)
            {
                throw new Exception("Tài khoản này đã bị khóa.");
            }

            var token = _jwtService.GenerateToken(user);


            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
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
