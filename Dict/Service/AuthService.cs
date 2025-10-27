using System.Text.RegularExpressions;
using Dict.Data;
using Dict.DTO;
using Dict.DTO.Auth;
using Dict.Models;
using Dict.Models.Enum;
using Dict.Service.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;

namespace Dict.Service
{
    public class AuthService : IAuthService
    {
        private readonly  ApplicationDbContext _context; // Thay YourDbContext bằng tên DbContext của bạn
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment; // ✨ Thêm dòng này
        private readonly IBlobService _blobService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        public AuthService(
            ApplicationDbContext context,
            IJwtService jwtService,
            IEmailService emailService,
            IWebHostEnvironment webHostEnvironment,
            IBlobService blobService,
            IConfiguration configuration,
            ILogger<AuthService> logger) // ✨ Thêm vào constructor
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment; // ✨ Lưu lại
            _blobService = blobService;
            _configuration = configuration;
            _logger = logger;
        }

        //public async Task<string> RegisterAsync(RegistrationRequestDto request)
        //{
        //    // ✨ THÊM MỚI: BƯỚC 1 - Kiểm tra độ mạnh mật khẩu
        //    var passwordError = ValidatePassword(request.Password);
        //    if (!string.IsNullOrEmpty(passwordError))
        //    {
        //        // Ném lỗi để Controller bắt và trả về 400 Bad Request
        //        throw new InvalidOperationException(passwordError);
        //    }

        //    // BƯỚC 2: Kiểm tra User/Email (giữ nguyên)
        //    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
        //    if (existingUser != null)
        //    {
        //        throw new InvalidOperationException("Username or Email already exists.");
        //    }

        //    // ... (Phần còn lại của logic đăng ký: tạo mã, tạo user, gửi email...)
        //    // (Giữ nguyên như cũ)
        //    var verificationCode = new Random().Next(1000, 9999).ToString();
        //    //var user = new User
        //    //{
        //    //    Username = request.Username,
        //    //    Email = request.Email,
        //    //    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Chỉ hash sau khi đã validate
        //    //    IsActive = false,
        //    //    EmailVerificationCode = verificationCode,
        //    //    VerificationCodeExpires = DateTime.UtcNow.AddMinutes(15),
        //    //    CreatedAt = DateTime.UtcNow
        //    //};
        //    //// ... (lưu CSDL và gửi email)
        //    //_context.Users.Add(user);
        //    await _context.SaveChangesAsync();

        //    var subject = "Your Verification Code";
        //    var body = $"Your verification code is: <strong>{verificationCode}</strong>. It will expire in 15 minutes.";
        //    //await _emailService.SendEmailAsync(user.Email, subject, body);

        //    return "Registration successful. Please check your email for the verification code.";
        //}


        public async Task<string> RegisterAsync(RegistrationRequestDto request)
        {
            // BƯỚC 1: Kiểm tra độ mạnh mật khẩu
            var passwordError = ValidatePassword(request.Password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                throw new InvalidOperationException(passwordError);
            }

            // BƯỚC 2: Kiểm tra User/Email tồn tại
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            // BƯỚC 3: Upload avatar mặc định lên Azure Blob
       
            // BƯỚC 4: Tạo user mới
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Role = "USER",
                AvatarUrl = "https://ocrr.blob.core.windows.net/avatars/106449882_p0.png"
            };

            // BƯỚC 5: Lưu vào DB
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return "Registration successful.";
        }

        private string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) { return "Password is required."; }
            if (password.Length < 8) { return "Password must be at least 8 characters long."; }
            if (!password.Any(char.IsUpper)) { return "Password must contain at least one uppercase letter."; }
            if (!password.Any(char.IsLower)) { return "Password must contain at least one lowercase letter."; }
            if (!password.Any(char.IsDigit)) { return "Password must contain at least one number."; }
            var specialCharRegex = new Regex("[!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]+");
            if (!specialCharRegex.IsMatch(password)) { return "Password must contain at least one special character (e.g., !@#$%)."; }
            return null;
        }

        // ✨ THÊM MỚI: Triển khai VerifyEmailAsync
        public async Task<LoginResponseDto> VerifyEmailAsync(VerifyEmailDto verifyDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verifyDto.Email);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (user.IsActive)
                throw new InvalidOperationException("Account already verified.");

            //if (user.VerificationCodeExpires < DateTime.UtcNow)
            //    throw new InvalidOperationException("Verification code has expired. Please register again.");

            //if (user.EmailVerificationCode != verifyDto.Code)
            //    throw new InvalidOperationException("Invalid verification code.");

      
            //user.IsActive = true;
            //user.EmailVerificationCode = null; // Xóa mã sau khi dùng
            //user.VerificationCodeExpires = null;
            //await _context.SaveChangesAsync();

     
            // ✨ Tạm thời: Bạn cần thay thế bằng logic tạo token của mình
            return new LoginResponseDto();
        }

        // ✨ THAY ĐỔI: Cập nhật LoginAsync

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 1. Tìm user (Không cần .Include() nữa)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // 2. Kiểm tra user và mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // 3. Kiểm tra tài khoản
            if (!user.IsActive)
            {
                throw new Exception("Tài khoản này đã bị khóa.");
            }

            // 4. Tạo JWT token
            var token = _jwtService.GenerateToken(user);

            // 5. Xác định AvatarUrl (Gán mặc định nếu null hoặc rỗng)
            // URL này có thể là link blob hoặc link local tùy vào lúc bạn cập nhật
            string avatarUrl = string.IsNullOrEmpty(user.AvatarUrl)
                ? "/images/default_ava.jpg"
                : user.AvatarUrl;

            // 6. Trả về token và thông tin user
            // (Đảm bảo LoginResponseDto có các trường Role và AvatarUrl)
            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role, // Đã là string
                AvatarUrl = avatarUrl
            };
        }
        public async Task LogoutAsync(int userId)
        {
            // In a simple JWT setup, logout is primarily handled client-side
            // by deleting the token.

            // Server-side actions (optional):
            // 1. Log the logout event
            _logger.LogInformation("User {UserId} logged out at {Timestamp}", userId, DateTime.UtcNow);

            // 2. If using a token blacklist:
            //    - Extract the token identifier (e.g., JTI) from the current request's token.
            //    - Add the identifier to the blacklist (e.g., in Redis or a database table)
            //      with an expiry matching the token's original expiry.
            //    await _tokenBlacklistService.BlacklistTokenAsync(jti, expiry);

            // For now, we just log.
            await Task.CompletedTask; // Represents async operation completion
        }
    }
}
