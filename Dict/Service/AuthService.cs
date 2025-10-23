using System.Text.RegularExpressions;
using Dict.Data;
using Dict.DTO;
using Dict.DTO.Auth;
using Dict.Models;
using Dict.Models.Enum;
using Dict.Service.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class AuthService : IAuthService
    {
        private readonly  ApplicationDbContext _context; // Thay YourDbContext bằng tên DbContext của bạn
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment; // ✨ Thêm dòng này

        public AuthService(
            ApplicationDbContext context,
            IJwtService jwtService,
            IEmailService emailService,
            IWebHostEnvironment webHostEnvironment) // ✨ Thêm vào constructor
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment; // ✨ Lưu lại
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

            // BƯỚC 2: Kiểm tra User/Email
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            // BƯỚC 3: Tạo người dùng mới với Role và Avatar mặc định
            // (Model User của bạn phải có các cột Role và AvatarUrl)
            string defaultAvatarBase64;
            try
            {
                // Đường dẫn tương đối đến file avatar mặc định trong wwwroot
                string defaultAvatarRelativePath = "images/default_ava.jpg";
                // Lấy đường dẫn vật lý
                string defaultAvatarFullPath = Path.Combine(_webHostEnvironment.WebRootPath, defaultAvatarRelativePath);

                if (System.IO.File.Exists(defaultAvatarFullPath))
                {
                    // Đọc toàn bộ byte của file
                    byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(defaultAvatarFullPath);
                    // Lấy kiểu MIME (cần thiết cho data URI) - bạn có thể cần logic phức tạp hơn nếu có nhiều loại ảnh
                    string mimeType = "image/jpeg"; // Hoặc image/png, image/gif tùy file của bạn
                                                    // Tạo chuỗi Base64 data URI
                    defaultAvatarBase64 = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
                }
                else
                {
                    // Xử lý nếu file không tìm thấy (gán null hoặc một giá trị mặc định khác)
                    defaultAvatarBase64 = null; // Hoặc ném lỗi tùy bạn
                                                // Log lỗi ở đây nếu cần
                                                // _logger.LogWarning("Default avatar file not found at {Path}", defaultAvatarFullPath);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi đọc file
                // _logger.LogError(ex, "Error reading default avatar file.");
                defaultAvatarBase64 = null; // Gán null khi có lỗi
            }


            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,

                // --- ✨ GÁN GIÁ TRỊ BASE64 ---
                Role = "USER",
                AvatarUrl = defaultAvatarBase64 // ✨ Lưu chuỗi Base64 (có thể rất dài)
                                                // --- KẾT THÚC ---
            };

            // BƯỚC 4: Lưu vào CSDL
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
    }
}
