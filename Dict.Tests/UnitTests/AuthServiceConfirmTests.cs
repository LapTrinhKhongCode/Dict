using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dict.Data;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Dict.Tests.UnitTests
{
    public class AuthServiceConfirmTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly IMemoryCache _memoryCache;
        private readonly AuthService _sut;

        public AuthServiceConfirmTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
            identityOptionsMock.Setup(x => x.Value).Returns(new IdentityOptions());
            var signInLoggerMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var authenticationSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
            var userConfirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();

            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                httpContextAccessorMock.Object,
                userClaimsPrincipalFactoryMock.Object,
                identityOptionsMock.Object,
                signInLoggerMock.Object,
                authenticationSchemeProviderMock.Object,
                userConfirmationMock.Object);

            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object,
                null!,
                null!,
                null!,
                null!);

            _jwtServiceMock = new Mock<IJwtService>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _sut = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _roleManagerMock.Object,
                _jwtServiceMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _loggerMock.Object,
                _memoryCache,
                _db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
            _memoryCache.Dispose();
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenUserDoesNotExist_ShouldThrowInvalidOperationException()
        {
            const string email = "missing@dict.test";
            const string token = "confirm-token";

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);

            Func<Task> act = async () => await _sut.ConfirmEmailAsync(email, token);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email không tồn tại.");
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenConfirmationFails_ShouldThrowInvalidOperationException()
        {
            const string email = "user@dict.test";
            const string token = "expired-token";
            var user = CreateUser(101, "tester", email);

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "bad token" }));

            Func<Task> act = async () => await _sut.ConfirmEmailAsync(email, token);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Link xác nhận không hợp lệ hoặc đã hết hạn.");
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenWorkspaceDoesNotExist_ShouldCreateWorkspaceAndReturnLoginResponse()
        {
            const string email = "user@dict.test";
            const string token = "valid-token";
            var user = CreateUser(202, "tester", email);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "USER" });
            _jwtServiceMock.Setup(x => x.GenerateToken(user, It.Is<IList<string>>(roles => roles.Count == 1 && roles[0] == "USER")))
                .Returns("jwt-token");

            var result = await _sut.ConfirmEmailAsync(email, token);

            result.Token.Should().Be("jwt-token");
            result.Username.Should().Be(user.UserName);
            result.Email.Should().Be(user.Email);
            result.Role.Should().Be("USER");
            result.AvatarUrl.Should().Be(user.AvatarUrl);
            result.UserId.Should().Be(user.Id);

            var workspace = await _db.Workspaces.SingleAsync();
            workspace.Name.Should().Be("Personal - tester");
            workspace.Description.Should().Be("Không gian làm việc cá nhân");
            workspace.OwnerType.Should().Be("PERSONAL");

            var member = await _db.WorkspaceMembers.SingleAsync();
            member.WorkspaceId.Should().Be(workspace.Id);
            member.UserId.Should().Be(user.Id);
            member.Role.Should().Be(WorkspaceRole.OWNER);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenWorkspaceAlreadyExists_ShouldNotCreateDuplicateWorkspace()
        {
            const string email = "user@dict.test";
            const string token = "valid-token";
            var user = CreateUser(303, "existing", email);
            var workspace = new Workspace
            {
                Name = "Personal - existing",
                Description = "Không gian làm việc cá nhân",
                OwnerType = "PERSONAL",
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.Workspaces.Add(workspace);
            await _db.SaveChangesAsync();

            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = user.Id,
                Role = WorkspaceRole.OWNER
            });
            await _db.SaveChangesAsync();

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "USER" });
            _jwtServiceMock.Setup(x => x.GenerateToken(user, It.IsAny<IList<string>>())).Returns("jwt-token");

            var result = await _sut.ConfirmEmailAsync(email, token);

            result.Token.Should().Be("jwt-token");
            (await _db.Workspaces.CountAsync()).Should().Be(1);
            (await _db.WorkspaceMembers.CountAsync()).Should().Be(1);
        }

        private static ApplicationUser CreateUser(int id, string userName, string email)
        {
            return new ApplicationUser
            {
                Id = id,
                AvatarUrl = "",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
        }
    }
}
