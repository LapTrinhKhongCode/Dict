using Dict.Data;
using Dict.Models;
using Dict.Service; // Cần cho AdminService
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Dict.Tests.UnitTests
{
    public class UpdateUserRolesTests
    {
        // === THAY ĐỔI 1: MOCK CÁC DEPENDENCY CỦA ADMINSERVICE ===
        private readonly Mock<ApplicationDbContext> _mockDbContext;
        private readonly Mock<ILogger<AdminService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;

        // Đối tượng đang được test (SUT)
        private readonly AdminService _adminService; // <-- Sửa thành AdminService

        // Dữ liệu test chung
        private readonly ApplicationUser _fakeUser;
        private readonly int _userId = 123;
        private readonly List<string> _newRoles;
        private readonly List<string> _currentRoles;

        // Constructor (chạy thay cho [SetUp])
        public UpdateUserRolesTests()
        {
            // === THAY ĐỔI 2: KHỞI TẠO CÁC MOCK MỚI ===

            // 1. Mock DbContext
            var options = new DbContextOptions<ApplicationDbContext>();
            _mockDbContext = new Mock<ApplicationDbContext>(options);

            // 2. Mock Logger
            _mockLogger = new Mock<ILogger<AdminService>>();

            // 3. Mock HttpContextAccessor (cho hàm GetAdminId())
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "AdminTestUser") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // 4. Mock UserManager (Boilerplate)
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // 5. Mock RoleManager (Boilerplate)
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);


            // === THAY ĐỔI 3: KHỞI TẠO ĐÚNG ADMINSERVICE ===
            _adminService = new AdminService(
                _mockDbContext.Object,
                _mockLogger.Object,
                _mockHttpContextAccessor.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object
            );

            // === Dữ liệu test (giữ nguyên) ===
            _fakeUser = new ApplicationUser { Id = _userId, UserName = "TestUser" };
            _newRoles = new List<string> { "USER", "MODERATOR" };
            _currentRoles = new List<string> { "GUEST" };
        }
        public class UpdateRolesTestCase
        {
            public string TestCaseName { get; set; }
            public bool FindByIdReturnsUser { get; set; }
            public bool TargetIsAdmin { get; set; }
            public Dictionary<string, bool> RoleExistsSetup { get; set; }
            public List<string> NewRoles { get; set; }
            public string RemoveRolesResult { get; set; } // "Success" or "Failed"
            public string AddRolesResult { get; set; } // "Success" or "Failed"
            public string ExpectedResultType { get; set; } // "ReturnsFalse", "ThrowsException", "ThrowsArgumentException"
            public string ExpectedExceptionMessage { get; set; }

            public override string ToString() => TestCaseName;
        }

        // 2. Hàm "Data Loader" (đọc file JSON)
        public static IEnumerable<object[]> GetUpdateRolesSadPathData()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "update_roles_test_data.json");
            var jsonText = File.ReadAllText(filePath);
            var testCases = JsonConvert.DeserializeObject<List<UpdateRolesTestCase>>(jsonText);
            return testCases.Select(tc => new object[] { tc });
        }

        // 3. HÀM MỚI: Test [Theory] đọc từ file JSON
        [Theory]
        [MemberData(nameof(GetUpdateRolesSadPathData))]
        public async Task UpdateUserRolesAsync_SadPaths_FromExternalFile(UpdateRolesTestCase testCase)
        {
            // ARRANGE

            // 1. Mock FindByIdAsync
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString()))
                .ReturnsAsync(testCase.FindByIdReturnsUser ? _fakeUser : null);

            // 2. Mock IsInRoleAsync
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN"))
                .ReturnsAsync(testCase.TargetIsAdmin);

            // 3. Mock RoleExistsAsync (động)
            _mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync((string roleName) =>
                    testCase.RoleExistsSetup.ContainsKey(roleName) && testCase.RoleExistsSetup[roleName]
                );

            // 4. Mock GetRolesAsync (chỉ cần cho các kịch bản sau)
            _mockUserManager.Setup(m => m.GetRolesAsync(_fakeUser)).ReturnsAsync(_currentRoles);

            // 5. Mock RemoveFromRolesAsync
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles))
                .ReturnsAsync(testCase.RemoveRolesResult == "Success" ? IdentityResult.Success : IdentityResult.Failed());

            // 6. Mock AddToRolesAsync
            _mockUserManager.Setup(m => m.AddToRolesAsync(_fakeUser, testCase.NewRoles))
               .ReturnsAsync(testCase.AddRolesResult == "Success" ? IdentityResult.Success : IdentityResult.Failed());

            // ACT & ASSERT
            if (testCase.ExpectedResultType == "ReturnsFalse")
            {
                var result = await _adminService.UpdateUserRolesAsync(_userId, testCase.NewRoles);
                Assert.False(result);
            }
            else
            {
                Exception ex;
                if (testCase.ExpectedResultType == "ThrowsArgumentException")
                {
                    ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                        _adminService.UpdateUserRolesAsync(_userId, testCase.NewRoles)
                    );
                }
                else // "ThrowsException"
                {
                    ex = await Assert.ThrowsAsync<Exception>(() =>
                        _adminService.UpdateUserRolesAsync(_userId, testCase.NewRoles)
                    );
                }
                Assert.Contains(testCase.ExpectedExceptionMessage, ex.Message);
            }
        }
        // === 6 KỊCH BẢN TEST (LOGIC GIỮ NGUYÊN, CHỈ SỬA TÊN BIẾN) ===

        // === KỊCH BẢN 1: USER KHÔNG TỒN TẠI ===
        [Fact]
        public async Task UpdateUserRolesAsync_WhenUserNotFound_ReturnsFalse()
        {
            // ARRANGE
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString()))
                .ReturnsAsync((ApplicationUser)null);

            // ACT
            var result = await _adminService.UpdateUserRolesAsync(_userId, _newRoles); // Sửa

            // ASSERT
            Assert.False(result);
        }

        // === KỊCH BẢN 2: CỐ GẮNG ĐỔI ROLE CỦA ADMIN ===
        [Fact]
        public async Task UpdateUserRolesAsync_WhenTargetUserIsAdmin_ReturnsFalse()
        {
            // ARRANGE
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString()))
                .ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN"))
                .ReturnsAsync(true);

            // ACT
            var result = await _adminService.UpdateUserRolesAsync(_userId, _newRoles); // Sửa

            // ASSERT
            Assert.False(result);

            // Xác minh (Verify) rằng LogWarning đã được gọi
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("attempted to change role of ADMIN")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // === KỊCH BẢN 3: TÊN ROLE MỚI KHÔNG HỢP LỆ ===
        [Fact]
        public async Task UpdateUserRolesAsync_WithInvalidRoleName_ThrowsArgumentException()
        {
            // ARRANGE
            var rolesWithInvalid = new List<string> { "USER", "INVALID_ROLE" };

            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN")).ReturnsAsync(false);

            _mockRoleManager.Setup(m => m.RoleExistsAsync("USER")).ReturnsAsync(true);
            _mockRoleManager.Setup(m => m.RoleExistsAsync("INVALID_ROLE")).ReturnsAsync(false);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _adminService.UpdateUserRolesAsync(_userId, rolesWithInvalid) // Sửa
            );
            Assert.Contains("Invalid role specified: INVALID_ROLE", ex.Message);
        }

        // === KỊCH BẢN 4: LỖI KHI XÓA ROLE CŨ ===
        [Fact]
        public async Task UpdateUserRolesAsync_WhenRemoveFromRolesFails_ThrowsException()
        {
            // ARRANGE
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN")).ReturnsAsync(false);
            _mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(_fakeUser)).ReturnsAsync(_currentRoles);

            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles))
                .ReturnsAsync(IdentityResult.Failed());

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _adminService.UpdateUserRolesAsync(_userId, _newRoles) // Sửa
            );
            Assert.Equal("Failed to remove old roles.", ex.Message);
        }

        // === KỊCH BẢN 5: LỖI KHI THÊM ROLE MỚI ===
        [Fact]
        public async Task UpdateUserRolesAsync_WhenAddToRolesFails_ThrowsException()
        {
            // ARRANGE
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN")).ReturnsAsync(false);
            _mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(_fakeUser)).ReturnsAsync(_currentRoles);
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRolesAsync(_fakeUser, _newRoles))
                .ReturnsAsync(IdentityResult.Failed());

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _adminService.UpdateUserRolesAsync(_userId, _newRoles) // Sửa
            );
            Assert.Equal("Failed to add new roles.", ex.Message);
        }

        // === KỊCH BẢN 6: HAPPY PATH (THÀNH CÔNG 100%) ===
        [Fact]
        public async Task UpdateUserRolesAsync_WithValidData_ReturnsTrue()
        {
            // ARRANGE
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN")).ReturnsAsync(false);
            _mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(_fakeUser)).ReturnsAsync(_currentRoles);
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRolesAsync(_fakeUser, _newRoles))
                .ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _adminService.UpdateUserRolesAsync(_userId, _newRoles); // Sửa

            // ASSERT
            Assert.True(result);
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles), Times.Once);
            _mockUserManager.Verify(m => m.AddToRolesAsync(_fakeUser, _newRoles), Times.Once);

            // Xác minh LogInformation
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("updated User 123 roles")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        [Fact]
        public async Task UpdateUserRolesAsync_WhenNewRolesListIsNull_ThrowsArgumentNullException()
        {
            // ARRANGE
            // (Không cần mock gì nhiều, vì nó sẽ lỗi ngay lập tức)
            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN")).ReturnsAsync(false);

            // ACT & ASSERT
            // Khẳng định rằng nó ném ra ArgumentNullException
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _adminService.UpdateUserRolesAsync(_userId, null) // Truyền vào NULL
            );
        }
        [Fact]
        public async Task UpdateUserRolesAsync_WithEmptyRoleList_ShouldRemoveAllRolesAndReturnTrue()
        {
            // ARRANGE
            var emptyRolesList = new List<string>(); // Danh sách rỗng

            _mockUserManager.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(_fakeUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(_fakeUser, "ADMIN")).ReturnsAsync(false);
            _mockUserManager.Setup(m => m.GetRolesAsync(_fakeUser)).ReturnsAsync(_currentRoles); // Giả sử có role cũ

            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRolesAsync(_fakeUser, emptyRolesList))
                .ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _adminService.UpdateUserRolesAsync(_userId, emptyRolesList);

            // ASSERT
            Assert.True(result);
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(_fakeUser, _currentRoles), Times.Once); // Phải gọi Remove
            _mockUserManager.Verify(m => m.AddToRolesAsync(_fakeUser, emptyRolesList), Times.Once); // Phải gọi Add (với list rỗng)
        }
    }
}