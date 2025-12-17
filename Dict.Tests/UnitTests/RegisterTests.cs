using Dict.DTO.Auth;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit; // Dùng xUnit

namespace Dict.Tests.UnitTests
{
    // Phải là public để Test Runner thấy
    public class RegisterTests
    {
        // Khai báo các đối tượng "giả" (Mock)
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockMemoryCache;

        // Đối tượng đang được test (System Under Test)
        private readonly AuthService _authService;

        // Constructor (chạy thay cho [SetUp])
        public RegisterTests()
        {
            // === Boilerplate cho Identity (bắt buộc) ===
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
            var loggerSignInMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemesMock = new Mock<IAuthenticationSchemeProvider>();
            var userConfirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object, httpContextAccessorMock.Object, userClaimsPrincipalFactoryMock.Object,
                identityOptionsMock.Object, loggerSignInMock.Object, schemesMock.Object, userConfirmationMock.Object);

            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            // === Mock các Interface ===
            _mockJwtService = new Mock<IJwtService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockMemoryCache = new Mock<IMemoryCache>();

            // Khởi tạo AuthService
            _authService = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockRoleManager.Object,
                _mockJwtService.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockMemoryCache.Object
            );
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockMemoryCache
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(mockCacheEntry.Object);
        }
        public class RegisterTestCase
        {
            public string TestCaseName { get; set; }
            public string InputUser { get; set; }
            public string InputEmail { get; set; }
            public string InputPass { get; set; }
            public bool FindByEmailReturnsUser { get; set; }
            public bool FindByNameReturnsUser { get; set; }
            public string FrontendUrlValue { get; set; } // null hoặc string
            public bool EmailServiceThrowsError { get; set; }
            public bool CacheServiceThrowsError { get; set; }
            public string ExpectedExceptionType { get; set; } // "Exception" hoặc "InvalidOperationException"
            public string ExpectedMessage { get; set; }

            public override string ToString() => TestCaseName;
        }

        // 2. Hàm "Data Loader" (đọc file JSON)
        public static IEnumerable<object[]> GetRegisterSadPathData()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "register_test_data.json");
            var jsonText = File.ReadAllText(filePath);
            var testCases = JsonConvert.DeserializeObject<List<RegisterTestCase>>(jsonText);
            return testCases.Select(tc => new object[] { tc });
        }

        // 3. HÀM MỚI: Test [Theory] đọc từ file JSON
        [Theory]
        [MemberData(nameof(GetRegisterSadPathData))]
        public async Task RegisterAsync_SadPaths_FromExternalFile(RegisterTestCase testCase)
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = testCase.InputUser,
                Email = testCase.InputEmail,
                Password = testCase.InputPass
            };

            // 1. Mock FindByEmail
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(testCase.FindByEmailReturnsUser ? new ApplicationUser() : null);

            // 2. Mock FindByName
            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(testCase.FindByNameReturnsUser ? new ApplicationUser() : null);

            // 3. Mock Configuration
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns(testCase.FrontendUrlValue);

            // 4. Mock Email Service
            if (testCase.EmailServiceThrowsError)
            {
                _mockEmailService.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(new Exception(testCase.ExpectedMessage));
            }

            // 5. Mock Cache Service
            if (testCase.CacheServiceThrowsError)
            {
                _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
                    .Throws(new Exception(testCase.ExpectedMessage));
            }

            // ACT & ASSERT
            Exception ex;
            if (testCase.ExpectedExceptionType == "InvalidOperationException")
            {
                ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
            }
            else
            {
                ex = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(request));
            }

            Assert.Equal(testCase.ExpectedMessage, ex.Message);
        }
        // === KỊCH BẢN 1: TEST VALIDATION PASSWORD (BỚI LỖI INPUT) ===
        [Theory]
        [InlineData(null, "Password is required.")] // Test Null
        [InlineData("", "Password is required.")]   // Test Rỗng
        [InlineData(" ", "Password is required.")]  // Test Khoảng trắng
        [InlineData("12345", "Password must be at least 6 characters long.")] // Test Quá ngắn
        public async Task RegisterAsync_WithInvalidPassword_ShouldThrowInvalidOperationException(string invalidPassword, string expectedMessage)
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "valid_user",
                Email = "valid@email.com",
                Password = invalidPassword // Password không hợp lệ
            };

            // ACT & ASSERT
            // Kiểm tra xem hàm có ném ra đúng Exception VÀ đúng Message không
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal(expectedMessage, ex.Message);
        }

        // === KỊCH BẢN 2: TEST VALIDATION USERNAME (BỚI LỖI INPUT) ===
        [Theory]
        [InlineData(null)]        // Test Null (Regex.IsMatch sẽ trả về false)
        [InlineData("")]          // Test Rỗng
        [InlineData(" ")]         // Test Khoảng trắng
        [InlineData("user name")] // Test Có khoảng trắng
        [InlineData("user@!")]    // Test Ký tự đặc biệt
        public async Task RegisterAsync_WithInvalidUsername_ShouldThrowInvalidOperationException(string invalidUsername)
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = invalidUsername, // Username không hợp lệ
                Email = "valid@email.com",
                Password = "ValidPassword123" // Password phải HỢP LỆ để vượt qua vòng 1
            };

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("Username chỉ được chứa chữ cái, số hoặc dấu gạch dưới, không có khoảng trắng hoặc dấu.", ex.Message);
        }

        // === KỊCH BẢN 3: TEST TRÙNG EMAIL (LOGIC NGHIỆP VỤ) ===
        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "valid_user",
                Email = "existing@email.com", // Email này đã tồn tại
                Password = "ValidPassword123"
            };

            // Dạy Mock: Nếu tìm Email này, hãy trả về 1 user (giả lập là đã tìm thấy)
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(new ApplicationUser());

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("Email already exists.", ex.Message);
        }

        // === KỊCH BẢN 4: TEST TRÙNG USERNAME (LOGIC NGHIỆP VỤ) ===
        [Fact]
        public async Task RegisterAsync_WithExistingUsername_ShouldThrowInvalidOperationException()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "existing_user", // Username này đã tồn tại
                Email = "valid@email.com",
                Password = "ValidPassword123"
            };

            // Dạy Mock: Tìm Email -> KHÔNG thấy (để qua vòng check email)
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Dạy Mock: Tìm Username -> CÓ thấy
            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(new ApplicationUser());

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("Username already exists.", ex.Message);
        }

        // === KỊCH BẢN 5: TEST LỖI CONFIG (LỖI MÔI TRƯỜNG) ===
        [Fact]
        public async Task RegisterAsync_WhenFrontendUrlIsMissing_ShouldThrowException()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "valid_user",
                Email = "valid@email.com",
                Password = "ValidPassword123"
            };

            // Dạy Mock: Các check logic đều qua
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

            // DÒNG SỬA LẠI ĐÚNG
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns((string)null);
            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("FrontendUrl is not configured", ex.Message);
        }

        // === KỊCH BẢN 6: TEST HAPPY PATH (CHẠY ĐÚNG 100%) ===
        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldSendEmailWithTokenAndReturnSuccess()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "new_user",
                Email = "test@example.com",
                Password = "ValidPassword123"
            };

            string capturedToken = null; // Biến để "bắt" token
            string capturedEmailBody = null; // Biến để "bắt" nội dung email

            // Dạy Mock: Các check logic đều qua
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

            // DÒNG SỬA LẠI ĐÚNG
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns("http://fake-frontend.com");

            // Dạy Mock: Giả lập IMemoryCache.Set()
            // (Phải mock CreateEntry vì .Set là extension method)
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockMemoryCache
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Callback<object>(key =>
                {
                    // "Bắt" cái token khi nó được dùng làm key
                    capturedToken = key as string;
                })
                .Returns(mockCacheEntry.Object);

            // Dạy Mock: Bắt nội dung EmailService
            _mockEmailService
                .Setup(m => m.SendEmailAsync(request.Email, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((email, subject, body) =>
                {
                    // "Bắt" nội dung body
                    capturedEmailBody = body;
                })
                .Returns(Task.CompletedTask);

            // ACT
            var result = await _authService.RegisterAsync(request);

            // ASSERT
            // 1. Kết quả trả về đúng chuỗi
            Assert.Equal("Registration successful. Please check your email to confirm your account.", result);

            // 2. Email Service được gọi đúng 1 lần với đúng email
            _mockEmailService.Verify(m => m.SendEmailAsync(
                request.Email,
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Once);

            // 3. Cache được set đúng 1 lần
            _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);

            // 4. (QUAN TRỌNG NHẤT) Token trong cache phải nằm trong link email
            Assert.NotNull(capturedToken);
            Assert.NotNull(capturedEmailBody);
            Assert.Contains(capturedToken, capturedEmailBody);
            Assert.Contains("http://fake-frontend.com/confirm-account?token=", capturedEmailBody);
        }

        // === KỊCH BẢN 7: TEST DỊCH VỤ NGOÀI BỊ SẬP (BỚI LỖI) ===
        [Fact]
        public async Task RegisterAsync_WhenEmailServiceFails_ShouldThrowException()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "new_user",
                Email = "test@example.com",
                Password = "ValidPassword123"
            };

            // Dạy Mock: Các check logic đều qua
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

            // DÒNG SỬA LẠI ĐÚNG
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns("http://fake-frontend.com");

            // Dạy Mock: Email Service bị lỗi và ném ra Exception
            _mockEmailService
                .Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Giả lập lỗi hệ thống email"));

            // ACT & ASSERT
            // Hàm RegisterAsync của bạn không try...catch lỗi email,
            // nên Exception đó sẽ nổi lên -> Đây là điều chúng ta kiểm tra
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("Giả lập lỗi hệ thống email", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenCacheServiceFails_ShouldThrowException()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "new_user",
                Email = "test@example.com",
                Password = "ValidPassword123"
            };

            // Dạy Mock: Các check logic đều qua
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

            // Dạy Mock: Config OK
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns("http://fake-frontend.com");

            // Dạy Mock: IMemoryCache.Set() bị lỗi
            // (Lưu ý: Chúng ta mock CreateEntry, vì Set là extension method)
            _mockMemoryCache
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Throws(new Exception("Giả lập lỗi cache sập"));

            // ACT & ASSERT
            // Hàm RegisterAsync của bạn không try...catch lỗi cache
            // nên Exception đó sẽ nổi lên -> Đây là điều chúng ta kiểm tra
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("Giả lập lỗi cache sập", ex.Message);
        }

        // === KỊCH BẢN 9: TEST ĐỘ NHẠY HOA/THƯỜNG (CASE-SENSITIVE) (BỚI LỖI) ===
        // Test này GIẢ LẬP rằng Identity/CSDL của bạn KHÔNG tự động xử lý
        // chữ hoa/thường, và code của bạn cũng không xử lý.
        [Fact]
        public async Task RegisterAsync_WithDifferentCaseEmail_ShouldStillFindUser()
        {
            // ARRANGE
            var request = new RegistrationRequestDto
            {
                Username = "new_user",
                Email = "test@example.com", // User gõ chữ thường
                Password = "ValidPassword123"
            };

            // Dạy Mock: Giả lập trong DB đã có email "TEST@EXAMPLE.COM" (chữ hoa)
            // Chúng ta mock cả 2 trường hợp cho chắc
            _mockUserManager.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(new ApplicationUser());
            _mockUserManager.Setup(m => m.FindByEmailAsync("TEST@EXAMPLE.COM")).ReturnsAsync(new ApplicationUser());

            // ACT & ASSERT
            // Chúng ta mong đợi hàm sẽ ném lỗi "Email already exists."
            // Nếu nó KHÔNG ném lỗi -> bạn có một BUG
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.RegisterAsync(request)
            );
            Assert.Equal("Email already exists.", ex.Message);

            // Xác minh rằng nó đã được gọi (dù chữ hoa hay thường)
            _mockUserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Once);
        }
    }
}