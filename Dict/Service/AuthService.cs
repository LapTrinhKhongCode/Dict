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
            // BƯỚC 1: Kiểm tra mật khẩu (giữ nguyên)
            var passwordError = ValidatePassword(request.Password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                throw new InvalidOperationException(passwordError);
            }

            // BƯỚC 2: Kiểm tra User/Email (vẫn cần kiểm tra)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            // BƯỚC 3: Hash mật khẩu
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // BƯỚC 4: Tạo Token tạm thời (Key cho cache)
            var confirmationToken = Guid.NewGuid().ToString();

            // ✨ BƯỚC 5: KHÔNG LƯU VÀO DB. Lưu vào Cache trong 30 phút.
            // Chúng ta lưu (DTO request + HashedPassword) vào cache
            var cacheEntry = new { RequestData = request, HashedPassword = hashedPassword };
            _cache.Set(confirmationToken, cacheEntry, TimeSpan.FromMinutes(30));

            // BƯỚC 6: Tạo link và gửi email
            var frontendUrl = _configuration["FrontendUrl"];
            if (string.IsNullOrEmpty(frontendUrl))
            {
                throw new Exception("FrontendUrl is not configured");
            }

            // ✨ Link này sẽ trỏ đến trang frontend xử lý xác nhận
            var confirmationUrl = $"{frontendUrl}/confirm-account?token={confirmationToken}";

            var subject = "Chào mừng bạn đến với Miyo Dictionary - Xác nhận tài khoản";
            var body = $"Cảm ơn bạn đã đăng ký. Vui lòng nhấp vào link sau để hoàn tất tạo tài khoản:<br/>" +
                       $"<a href='{confirmationUrl}' style='...'>Click để Xác nhận Tài khoản</a>" +
                       $"<br/><br/>Link này sẽ hết hạn sau 30 phút.";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return "Registration successful. Please check your email to confirm your account.";
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
                AvatarUrl = "https://ocrr.blob.core.windows.net/avatars/106449882_p0.png",
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
