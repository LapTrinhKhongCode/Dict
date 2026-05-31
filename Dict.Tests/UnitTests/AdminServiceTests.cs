using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.Admin;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dict.Tests.Services
{
    public class AdminServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<ILogger<AdminService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly AdminService _sut;

        public AdminServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);

            // 1. Mock UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // 2. Mock RoleManager
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            // 3. Mock Logger & HttpContext
            _mockLogger = new Mock<ILogger<AdminService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // Giả lập Admin đang đăng nhập có ID = 999
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "999") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = claimsPrincipal });

            _sut = new AdminService(_db, _mockLogger.Object, _mockHttpContextAccessor.Object, _mockUserManager.Object, _mockRoleManager.Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task SetUserLockStatusAsync_WhenTargetIsAdmin_ShouldReturnFalse_AndNotLock()
        {
            // Arrange: Cố tình khóa một Admin khác
            int targetUserId = 1;
            var targetUser = new ApplicationUser { Id = targetUserId, UserName = "super_admin", IsActive = true, AvatarUrl = "" };

            _mockUserManager.Setup(u => u.FindByIdAsync(targetUserId.ToString())).ReturnsAsync(targetUser);
            // Báo cho hệ thống biết thằng này đang giữ Role ADMIN
            _mockUserManager.Setup(u => u.IsInRoleAsync(targetUser, "ADMIN")).ReturnsAsync(true);

            // Act
            var result = await _sut.SetUserLockStatusAsync(targetUserId, isLocked: true);

            // Assert
            result.Should().BeFalse(); // Bị chặn lại
            targetUser.IsActive.Should().BeTrue(); // Trạng thái vẫn là kích hoạt (không bị khóa)
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithInvalidRoleName_ShouldThrowArgumentException()
        {
            // Arrange
            int targetUserId = 1;
            var targetUser = new ApplicationUser { Id = targetUserId, UserName = "normal_user", AvatarUrl = "" };
            var newRoles = new List<string> { "HACKER_ROLE" };

            _mockUserManager.Setup(u => u.FindByIdAsync(targetUserId.ToString())).ReturnsAsync(targetUser);
            _mockUserManager.Setup(u => u.IsInRoleAsync(targetUser, "ADMIN")).ReturnsAsync(false);

            // Báo rằng Role này không tồn tại trong hệ thống
            _mockRoleManager.Setup(r => r.RoleExistsAsync("HACKER_ROLE")).ReturnsAsync(false);

            // Act & Assert
            Func<Task> act = async () => await _sut.UpdateUserRolesAsync(targetUserId, newRoles);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid role specified: HACKER_ROLE");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WhenValid_ShouldRemoveOldAndAddNewRoles()
        {
            // Arrange
            int targetUserId = 1;
            var targetUser = new ApplicationUser { Id = targetUserId, UserName = "normal_user", AvatarUrl = "" };
            var oldRoles = new List<string> { "USER" };
            var newRoles = new List<string> { "PREMIUM_USER" };

            _mockUserManager.Setup(u => u.FindByIdAsync(targetUserId.ToString())).ReturnsAsync(targetUser);
            _mockUserManager.Setup(u => u.IsInRoleAsync(targetUser, "ADMIN")).ReturnsAsync(false);
            _mockRoleManager.Setup(r => r.RoleExistsAsync("PREMIUM_USER")).ReturnsAsync(true);

            _mockUserManager.Setup(u => u.GetRolesAsync(targetUser)).ReturnsAsync(oldRoles);

            _mockUserManager.Setup(u => u.RemoveFromRolesAsync(targetUser, oldRoles))
                            .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.AddToRolesAsync(targetUser, newRoles))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _sut.UpdateUserRolesAsync(targetUserId, newRoles);

            // Assert
            result.Should().BeTrue();
            // Xác nhận rằng hàm gỡ role cũ và thêm role mới đã thực sự được gọi
            _mockUserManager.Verify(u => u.RemoveFromRolesAsync(targetUser, oldRoles), Times.Once);
            _mockUserManager.Verify(u => u.AddToRolesAsync(targetUser, newRoles), Times.Once);
        }

        [Fact]
        public async Task SearchAllDecksAsync_ShouldReturnFilteredAndPaginatedResults()
        {
            // Arrange
            // Chú ý: Cần gán đủ các trường Required như AvatarUrl, Description để EF InMemory không báo lỗi
            var author = new ApplicationUser { Id = 1, UserName = "Sensei", AvatarUrl = "" };
            _db.Users.Add(author);

            _db.Decks.Add(new Deck { Id = 1, UserId = 1, Name = "Alpha Japanese", Description = "", IsPublic = true, CreatedAt = DateTime.UtcNow });
            _db.Decks.Add(new Deck { Id = 2, UserId = 1, Name = "Beta Kanji", Description = "", IsPublic = false, CreatedAt = DateTime.UtcNow });
            _db.Decks.Add(new Deck { Id = 3, UserId = 1, Name = "Alpha Vocab", Description = "", IsPublic = true, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            // Act: Tìm chữ "alpha", trang 1, lấy 10 item
            var result = await _sut.SearchAllDecksAsync("alpha", 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2); // Có 2 deck chứa chữ "Alpha"
            result.Items.Should().HaveCount(2);
            result.Items.Should().Contain(d => d.Name == "Alpha Japanese");
            result.Items.Should().Contain(d => d.Name == "Alpha Vocab");
            result.Items.First().AuthorName.Should().Be("Sensei"); // Kiểm tra xem phép Join (Include) có hoạt động không
        }

        [Fact]
        public async Task AdminResetUserPasswordAsync_WhenTargetIsAdmin_ShouldBlockAndReturnFalse()
        {
            // Arrange: Cố tình reset pass của một Admin khác
            int targetUserId = 2;
            var targetUser = new ApplicationUser { Id = targetUserId, UserName = "boss_admin", AvatarUrl = "" };

            _mockUserManager.Setup(u => u.FindByIdAsync(targetUserId.ToString())).ReturnsAsync(targetUser);
            _mockUserManager.Setup(u => u.IsInRoleAsync(targetUser, "ADMIN")).ReturnsAsync(true);

            // Act
            var result = await _sut.AdminResetUserPasswordAsync(targetUserId, "NewPass123!");

            // Assert
            result.Should().BeFalse();
            // Xác nhận rằng hàm xóa pass cũ KHÔNG BAO GIỜ được gọi tới
            _mockUserManager.Verify(u => u.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}