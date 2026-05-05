//using Dict.DTO.Auth;
//using Dict.Models;
//using Dict.Service;
//using Dict.Service.IService;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Moq;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Xunit; // Dùng xUnit

//namespace Dict.Tests.UnitTests
//{
//    public class ConfirmRegistrationTests
//    {
//        // Khai báo các đối tượng "giả" (Mock)
//        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
//        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
//        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
//        private readonly Mock<IJwtService> _mockJwtService;
//        private readonly Mock<IEmailService> _mockEmailService;
//        private readonly Mock<IConfiguration> _mockConfiguration;
//        private readonly Mock<ILogger<AuthService>> _mockLogger;
//        private readonly Mock<IMemoryCache> _mockMemoryCache;

//        // Đối tượng đang được test (System Under Test)
//        private readonly AuthService _authService;

//        // Dữ liệu cache giả
//        private readonly object _fakeCacheEntry;
//        private readonly RegistrationRequestDto _fakeRequestData;
//        private readonly ApplicationUser _fakeUser;
//        private readonly string _validToken = "valid_token_123";

//        // Constructor (chạy thay cho [SetUp])
//        public ConfirmRegistrationTests()
//        {
//            // === Boilerplate cho Identity (bắt buộc) ===
//            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
//            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
//                userStoreMock.Object, null, null, null, null, null, null, null, null);

//            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
//            var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
//            var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
//            var loggerSignInMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
//            var schemesMock = new Mock<IAuthenticationSchemeProvider>();
//            var userConfirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();

//            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
//                _mockUserManager.Object, httpContextAccessorMock.Object, userClaimsPrincipalFactoryMock.Object,
//                identityOptionsMock.Object, loggerSignInMock.Object, schemesMock.Object, userConfirmationMock.Object);

//            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
//            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
//                roleStoreMock.Object, null, null, null, null);

//            // === Mock các Interface ===
//            _mockJwtService = new Mock<IJwtService>();
//            _mockEmailService = new Mock<IEmailService>();
//            _mockConfiguration = new Mock<IConfiguration>();
//            _mockLogger = new Mock<ILogger<AuthService>>();
//            _mockMemoryCache = new Mock<IMemoryCache>();

//            // === Khởi tạo AuthService ===
//            _authService = new AuthService(
//                _mockUserManager.Object,
//                _mockSignInManager.Object,
//                _mockRoleManager.Object,
//                _mockJwtService.Object,
//                _mockEmailService.Object,
//                _mockConfiguration.Object,
//                _mockLogger.Object,
//                _mockMemoryCache.Object
//            );

//            // === Chuẩn bị dữ liệu giả cho cache ===
//            _fakeRequestData = new RegistrationRequestDto
//            {
//                Username = "new_user",
//                Email = "test@example.com",
//                Password = "ValidPassword123"
//            };
//            _fakeCacheEntry = new { RequestData = _fakeRequestData };
//            _fakeUser = new ApplicationUser { Id = 1, UserName = _fakeRequestData.Username };
//        }
//        public class ConfirmTestCase
//        {
//            public string TestCaseName { get; set; }
//            public bool CacheFindsToken { get; set; }
//            public bool FindByNameReturnsUser { get; set; }
//            public bool FindByEmailReturnsUser { get; set; }
//            public string CreateAsyncResult { get; set; } // "Success" or "Failed"
//            public string CreateAsyncError { get; set; }
//            public string AddToRoleResult { get; set; } // "Success" or "Failed"
//            public string AddToRoleError { get; set; }
//            public string ExpectedExceptionType { get; set; }
//            public string ExpectedMessage { get; set; }

//            public override string ToString() => TestCaseName;
//        }

//        // 2. Hàm "Data Loader" (đọc file JSON)
//        public static IEnumerable<object[]> GetConfirmSadPathData()
//        {
//            var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "confirm_test_data.json");
//            var jsonText = File.ReadAllText(filePath);
//            var testCases = JsonConvert.DeserializeObject<List<ConfirmTestCase>>(jsonText);
//            return testCases.Select(tc => new object[] { tc });
//        }

//        // 3. HÀM MỚI: Test [Theory] đọc từ file JSON
//        [Theory]
//        [MemberData(nameof(GetConfirmSadPathData))]
//        public async Task ConfirmRegistrationAsync_SadPaths_FromExternalFile(ConfirmTestCase testCase)
//        {
//            // ARRANGE
//            object outValue = _fakeCacheEntry;

//            // 1. Mock Cache
//            _mockMemoryCache
//                .Setup(m => m.TryGetValue(It.IsAny<string>(), out outValue))
//                .Returns(testCase.CacheFindsToken);

//            // 2. Mock Race Condition (FindByName/FindByEmail)
//            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>()))
//                .ReturnsAsync(testCase.FindByNameReturnsUser ? new ApplicationUser() : null);
//            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
//                .ReturnsAsync(testCase.FindByEmailReturnsUser ? new ApplicationUser() : null);

//            // 3. Mock CreateAsync
//            var createErrors = testCase.CreateAsyncResult == "Failed"
//                ? new[] { new IdentityError { Description = testCase.CreateAsyncError } }
//                : Array.Empty<IdentityError>();

//            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
//                .ReturnsAsync(testCase.CreateAsyncResult == "Success"
//                    ? IdentityResult.Success
//                    : IdentityResult.Failed(createErrors));

//            // 4. Mock AddToRoleAsync
//            var roleErrors = testCase.AddToRoleResult == "Failed"
//                ? new[] { new IdentityError { Description = testCase.AddToRoleError } }
//                : Array.Empty<IdentityError>();

//            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "USER"))
//                .ReturnsAsync(testCase.AddToRoleResult == "Success"
//                    ? IdentityResult.Success
//                    : IdentityResult.Failed(roleErrors));

//            // ACT & ASSERT
//            Exception ex;
//            if (testCase.ExpectedExceptionType == "InvalidOperationException")
//            {
//                ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                    _authService.ConfirmRegistrationAsync(_validToken)
//                );
//            }
//            else
//            {
//                ex = await Assert.ThrowsAsync<Exception>(() =>
//                    _authService.ConfirmRegistrationAsync(_validToken)
//                );
//            }

//            // Khẳng định message
//            Assert.Contains(testCase.ExpectedMessage, ex.Message);
//        }
//        // === KỊCH BẢN 1: TOKEN KHÔNG HỢP LỆ / HẾT HẠN ===
//        [Fact]
//        public async Task ConfirmRegistrationAsync_WithInvalidToken_ShouldThrowException()
//        {
//            // ARRANGE
//            string invalidToken = "invalid_or_expired_token";
//            object outValue; // Biến out cho TryGetValue

//            // Dạy Mock: TryGetValue trả về false (không tìm thấy token)
//            _mockMemoryCache
//                .Setup(m => m.TryGetValue(invalidToken, out outValue))
//                .Returns(false);

//            // ACT & ASSERT
//            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                _authService.ConfirmRegistrationAsync(invalidToken)
//            );
//            Assert.Equal("Link xác thực không hợp lệ, đã hết hạn hoặc không tồn tại.", ex.Message);
//        }

//        // === KỊCH BẢN 2: LỖI RACE CONDITION (TRÙNG USERNAME/EMAIL KHI CONFIRM) ===
//        // (User A gửi mail, Admin tạo user B trùng email, User A mới confirm)
//        [Fact]
//        public async Task ConfirmRegistrationAsync_WithRaceCondition_DuplicateEmail_ShouldThrowException()
//        {
//            // ARRANGE
//            object outValue = _fakeCacheEntry;

//            // Dạy Mock: Token hợp lệ
//            _mockMemoryCache
//                .Setup(m => m.TryGetValue(_validToken, out outValue))
//                .Returns(true);

//            // Dạy Mock: Bị trùng email (Race Condition, check ở dòng 134)
//            _mockUserManager.Setup(m => m.FindByNameAsync(_fakeRequestData.Username)).ReturnsAsync((ApplicationUser)null);
//            _mockUserManager.Setup(m => m.FindByEmailAsync(_fakeRequestData.Email)).ReturnsAsync(new ApplicationUser());

//            // ACT & ASSERT
//            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                _authService.ConfirmRegistrationAsync(_validToken)
//            );
//            Assert.Equal("Username or Email already exists.", ex.Message);
//        }

//        // === KỊCH BẢN 3: LỖI CHÍNH SÁCH MẬT KHẨU CỦA IDENTITY ===
//        // (Mật khẩu qua check của bạn, nhưng không qua check của Identity)
//        [Fact]
//        public async Task ConfirmRegistrationAsync_WhenCreateAsyncFails_ShouldThrowException()
//        {
//            // ARRANGE
//            object outValue = _fakeCacheEntry;
//            var identityErrors = new List<IdentityError> { new IdentityError { Description = "Password too weak" } };

//            // Dạy Mock: Token hợp lệ
//            _mockMemoryCache
//                .Setup(m => m.TryGetValue(_validToken, out outValue))
//                .Returns(true);

//            // Dạy Mock: Không trùng (qua vòng check race condition)
//            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
//            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

//            // Dạy Mock: _userManager.CreateAsync BỊ THẤT BẠI
//            _mockUserManager
//                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), _fakeRequestData.Password))
//                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

//            // ACT & ASSERT
//            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                _authService.ConfirmRegistrationAsync(_validToken)
//            );
//            Assert.Contains("Failed to create user: Password too weak", ex.Message);
//        }

//        // === KỊCH BẢN 4: LỖI GÁN ROLE (BỚI LỖI - PHÁT HIỆN BUG) ===
//        // (Tạo user OK, nhưng gán role "User" bị lỗi)
//        [Fact]
//        public async Task ConfirmRegistrationAsync_WhenAddToRoleFails_ShouldThrowException()
//        {
//            // ARRANGE
//            object outValue = _fakeCacheEntry;
//            var identityErrors = new List<IdentityError> { new IdentityError { Description = "Role User does not exist" } };

//            // Dạy Mock: Token hợp lệ, check race condition OK
//            _mockMemoryCache.Setup(m => m.TryGetValue(_validToken, out outValue)).Returns(true);
//            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
//            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

//            // Dạy Mock: CreateAsync THÀNH CÔNG
//            _mockUserManager
//                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), _fakeRequestData.Password))
//                .ReturnsAsync(IdentityResult.Success);

//            // Dạy Mock: AddToRoleAsync BỊ THẤT BẠI
//            _mockUserManager
//                .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "USER"))
//                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

//            // ACT & ASSERT
//            // *** ĐIỂM BỚI LỖI ***
//            // Code AuthService (dòng 161) của bạn không kiểm tra kết quả của AddToRoleAsync.
//            // Do đó, test này sẽ FAILED (báo "No exception was thrown").
//            // Đây là một bug. Bạn CẦN sửa AuthService để check IdentityResult của AddToRoleAsync
//            // và ném ra Exception. Sau khi bạn sửa, test này sẽ PASS.
//            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                _authService.ConfirmRegistrationAsync(_validToken)
//            );
//            Assert.Contains("Role User does not exist", ex.Message);
//        }

//        // === KỊCH BẢN 5: HAPPY PATH (THÀNH CÔNG 100%) ===
//        [Fact]
//        public async Task ConfirmRegistrationAsync_WithValidToken_ShouldCreateUserAndReturnDto()
//        {
//            // ARRANGE
//            object outValue = _fakeCacheEntry;
//            var fakeRoles = new List<string> { "USER" };
//            var fakeJwtToken = "day_la_jwt_token_gia_123";

//            // Dạy Mock: Token hợp lệ, check race condition OK
//            _mockMemoryCache.Setup(m => m.TryGetValue(_validToken, out outValue)).Returns(true);
//            _mockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
//            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

//            // Dạy Mock: CreateAsync THÀNH CÔNG
//            _mockUserManager
//                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), _fakeRequestData.Password))
//                .ReturnsAsync(IdentityResult.Success)
//                .Callback<ApplicationUser, string>((user, pass) => {
//                    // Gán Id cho user giả để các bước sau dùng được
//                    user.Id = _fakeUser.Id;
//                    user.UserName = _fakeUser.UserName;
//                });

//            // Dạy Mock: AddToRoleAsync THÀNH CÔNG
//            _mockUserManager
//                .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "USER"))
//                .ReturnsAsync(IdentityResult.Success);

//            // Dạy Mock: GetRolesAsync
//            _mockUserManager
//                .Setup(m => m.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == _fakeUser.Id)))
//                .ReturnsAsync(fakeRoles);

//            // Dạy Mock: GenerateToken
//            _mockJwtService
//                .Setup(m => m.GenerateToken(It.Is<ApplicationUser>(u => u.Id == _fakeUser.Id), fakeRoles))
//                .Returns(fakeJwtToken);

//            // ACT
//            var result = await _authService.ConfirmRegistrationAsync(_validToken);

//            // ASSERT
//            // 1. Kiểm tra DTO trả về
//            Assert.NotNull(result);
//            Assert.IsType<LoginResponseDto>(result);
//            Assert.Equal(fakeJwtToken, result.Token);
//            Assert.Equal(_fakeRequestData.Username, result.Username);
//            Assert.Equal(fakeRoles.First(), result.Role);
//            Assert.Equal(_fakeUser.Id, result.UserId);

//            // 2. Xác minh (Verify) các hàm quan trọng đã được gọi
//            _mockMemoryCache.Verify(m => m.Remove(_validToken), Times.Once); // Token đã bị xóa?
//            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), _fakeRequestData.Password), Times.Once);
//            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "USER"), Times.Once);
//            _mockJwtService.Verify(m => m.GenerateToken(It.IsAny<ApplicationUser>(), fakeRoles), Times.Once);   
//        }
//    }
//}