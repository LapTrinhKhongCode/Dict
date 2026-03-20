using Dict.DTO.Auth;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit; // THAY ĐỔI 1: Đổi using

namespace Dict.Tests.UnitTests
{
    // THAY ĐỔI 2: Đã xóa [TestFixture]
    public class LoginTests
    {
        // Khai báo các đối tượng "giả" (Mock)
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        // Đây là đối tượng SUT (System Under Test - Hệ thống đang được test)
        private readonly AuthService _authService;

        // THAY ĐỔI 3: Đã đổi [SetUp] thành Constructor
        public LoginTests()
        {
            // ---- PHẦN NÀY LÀ BOILERPLATE (CODE MẪU) BẮT BUỘC ----
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
            var loggerMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemesMock = new Mock<IAuthenticationSchemeProvider>();
            var userConfirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                httpContextAccessorMock.Object,
                userClaimsPrincipalFactoryMock.Object,
                identityOptionsMock.Object,
                loggerMock.Object,
                schemesMock.Object,
                userConfirmationMock.Object);
            // ---------------- HẾT PHẦN BOILERPLATE -----------------

            _mockJwtService = new Mock<IJwtService>();
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockMemoryCache = new Mock<IMemoryCache>();

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
        }
        public class LoginTestCase
        {
            public string TestCaseName { get; set; }
            public string InputUser { get; set; }
            public string InputPass { get; set; }
            public bool FindByNameReturnsUser { get; set; }
            public string CheckPasswordResult { get; set; } // "Success", "Failed", "LockedOut", "NotAllowed"
            public bool UserIsActive { get; set; }
            public string ExpectedMessage { get; set; }

            // Dùng để hiển thị tên test case trong Test Explorer
            public override string ToString() => TestCaseName;
        }

        // 2. Hàm "Data Loader" (đọc file JSON)
        public static IEnumerable<object[]> GetLoginSadPathData()
        {
            // (Hãy chắc chắn file JSON đã được set "Copy if newer")
            var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "login_test_data.json");
            var jsonText = File.ReadAllText(filePath);
            var testCases = JsonConvert.DeserializeObject<List<LoginTestCase>>(jsonText);
            return testCases.Select(tc => new object[] { tc });
        }

        // 3. HÀM MỚI: Test [Theory] đọc từ file JSON
        [Theory]
        [MemberData(nameof(GetLoginSadPathData))] // <-- Gọi hàm data loader
        public async Task LoginAsync_SadPaths_FromExternalFile(LoginTestCase testCase)
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = testCase.InputUser, Password = testCase.InputPass };
            var fakeUser = new ApplicationUser
            {
                UserName = testCase.InputUser,
                IsActive = testCase.UserIsActive
            };

            // "Dạy" Mock UserManager dựa trên dữ liệu từ file
            if (testCase.FindByNameReturnsUser)
            {
                _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                               .ReturnsAsync(fakeUser);
            }
            else
            {
                _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                               .ReturnsAsync((ApplicationUser)null);
            }

            // "Dạy" Mock SignInManager dựa trên dữ liệu từ file
            switch (testCase.CheckPasswordResult)
            {
                case "Success":
                    _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                                     .ReturnsAsync(SignInResult.Success);
                    break;
                case "LockedOut":
                    _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                                     .ReturnsAsync(SignInResult.LockedOut);
                    break;
                case "NotAllowed":
                    _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                                    .ReturnsAsync(SignInResult.NotAllowed);
                    break;
                case "Failed":
                default:
                    _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                                    .ReturnsAsync(SignInResult.Failed);
                    break;
            }

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await _authService.LoginAsync(request)
            );

            // Khẳng định kết quả
            Assert.Equal(testCase.ExpectedMessage, ex.Message);
        }
        // === KỊCH BẢN 1: USER KHÔNG TỒN TẠI ===
        [Fact] // THAY ĐỔI 4: Đổi [Test] thành [Fact]
        public async Task LoginAsync_WithNonExistingUser_ShouldThrowException()
        {
            // ARRANGE (Sắp đặt)
            var request = new LoginRequestDto { Username = "user_khong_ton_tai" };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync((ApplicationUser)null);

            // ACT & ASSERT (Hành động & Khẳng định)

            // THAY ĐỔI 5: Thêm 'await' vì Assert.ThrowsAsync của xUnit trả về Task<T>
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await _authService.LoginAsync(request)
            );

            // THAY ĐỔI 6: Đổi cú pháp Assert
            Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", ex.Message);
        }

        // === KỊCH BẢN 2: SAI MẬT KHẨU ===
        [Fact] // THAY ĐỔI
        public async Task LoginAsync_WithWrongPassword_ShouldThrowException()
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = "user_dung", Password = "password_sai" };
            var fakeUser = new ApplicationUser { UserName = "user_dung" };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(fakeUser);

            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                .ReturnsAsync(SignInResult.Failed);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(async () => // THAY ĐỔI
                await _authService.LoginAsync(request)
            );
            Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", ex.Message); // THAY ĐỔI
        }

        // === KỊCH BẢN 3: TÀI KHOẢN BỊ KHÓA ===
        [Fact] // THAY ĐỔI
        public async Task LoginAsync_WithInactiveUser_ShouldThrowException()
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = "user_bi_khoa", Password = "password_dung" };
            var fakeUser = new ApplicationUser { UserName = "user_bi_khoa", IsActive = false };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(fakeUser);

            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                .ReturnsAsync(SignInResult.Success);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(async () => // THAY ĐỔI
                await _authService.LoginAsync(request)
            );
            Assert.Equal("Tài khoản này đã bị khóa.", ex.Message); // THAY ĐỔI
        }

        // === KỊCH BẢN 4: ĐĂNG NHẬP THÀNH CÔNG (HAPPY PATH) ===
        [Fact] // THAY ĐỔI
        public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponseDto()
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = "user_hop_le", Password = "password_dung" };
            var fakeUser = new ApplicationUser
            {
                Id = 123, // Giữ nguyên 'int' như bạn đã xác nhận
                UserName = "user_hop_le",
                Email = "test@example.com",
                AvatarUrl = "avatar.png",
                IsActive = true
            };
            var fakeRoles = new List<string> { "User" };
            var fakeToken = "day_la_mot_token_gia_do_mock_tao_ra";

            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(fakeUser);

            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                .ReturnsAsync(SignInResult.Success);

            _mockUserManager.Setup(m => m.GetRolesAsync(fakeUser))
                .ReturnsAsync(fakeRoles);

            _mockJwtService.Setup(m => m.GenerateToken(fakeUser, fakeRoles))
                .Returns(fakeToken);

            // ACT
            var result = await _authService.LoginAsync(request);

            // ASSERT (THAY ĐỔI 7 & 8)
            Assert.NotNull(result);
            Assert.IsType<LoginResponseDto>(result);
            Assert.Equal(fakeToken, result.Token);
            Assert.Equal(fakeUser.UserName, result.Username);
            Assert.Equal(fakeUser.Email, result.Email);
            Assert.Equal(fakeUser.Id, result.UserId);
            Assert.Equal(fakeRoles.First(), result.Role);
        }

        [Theory] // Dùng [Theory] thay vì [Fact]
                 // Sắp xếp dữ liệu test: [username, password, thông báo lỗi mong đợi]
        [InlineData(null, "any_pass", "Tên đăng nhập hoặc mật khẩu không chính xác.")]
        [InlineData("", "any_pass", "Tên đăng nhập hoặc mật khẩu không chính xác.")]
        [InlineData(" ", "any_pass", "Tên đăng nhập hoặc mật khẩu không chính xác.")] // (Test khoảng trắng)
        [InlineData("any_user", null, "Tên đăng nhập hoặc mật khẩu không chính xác.")]
        [InlineData("any_user", "", "Tên đăng nhập hoặc mật khẩu không chính xác.")]
        [InlineData("any_user", " ", "Tên đăng nhập hoặc mật khẩu không chính xác.")] // (Test khoảng trắng)
        public async Task LoginAsync_WithInvalidInput_ShouldThrowException(string username, string password, string expectedMessage)
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = username, Password = password };

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await _authService.LoginAsync(request)
            );

            // Kiểm tra xem có đúng thông báo lỗi validation không
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task LoginAsync_WithLockedOutUser_ShouldThrowException()
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = "user_bi_lock", Password = "password_dung" };
            var fakeUser = new ApplicationUser { UserName = "user_bi_lock", IsActive = true };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(fakeUser);

            // Dạy mock: Mật khẩu đúng, NHƯNG tài khoản đang bị Lockout
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                .ReturnsAsync(SignInResult.LockedOut); // <-- Quan trọng

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await _authService.LoginAsync(request)
            );

            // Bạn cần quyết định xem logic code của bạn sẽ báo lỗi gì
            // (Trong code gốc, bạn chưa xử lý .IsLockedOut)
            Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_WithEmailNotConfirmed_ShouldThrowException()
        {
            // ARRANGE
            var request = new LoginRequestDto { Username = "user_chua_confirm", Password = "password_dung" };
            var fakeUser = new ApplicationUser { UserName = "user_chua_confirm", IsActive = true };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.Username))
                .ReturnsAsync(fakeUser);

            // Dạy mock: Mật khẩu đúng, NHƯNG chưa được phép (ví dụ: chưa confirm email)
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(fakeUser, request.Password, false))
                .ReturnsAsync(SignInResult.NotAllowed); // <-- Quan trọng

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await _authService.LoginAsync(request)
            );

            Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", ex.Message);
        }
    }
}