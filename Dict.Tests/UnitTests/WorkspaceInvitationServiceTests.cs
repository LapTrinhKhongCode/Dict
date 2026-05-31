using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO;
using Dict.Enums;
using Dict.Hubs;
using Dict.Models;
using Dict.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Dict.Tests.Services
{
    public class WorkspaceInvitationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly WorkspaceInvitationService _sut;

        public WorkspaceInvitationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _sut = new WorkspaceInvitationService(_db, _mockHubContext.Object, _mockHttpContextAccessor.Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task InviteMemberAsync_WhenInviteeIsSelf_ShouldThrowException()
        {
            // Arrange
            int myUserId = 1;
            // ĐÃ FIX: Thêm AvatarUrl
            _db.Users.Add(new ApplicationUser { Id = myUserId, Email = "test@abc.com", UserName = "test_user", AvatarUrl = "" });
            await _db.SaveChangesAsync();

            var dto = new CreateInvitationDTO
            {
                WorkspaceId = 1,
                InviteeIdentifier = "test@abc.com",
                ExpectedRole = "MEMBER"
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.InviteMemberAsync(myUserId, dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Bạn không thể tự mời chính mình vào dự án.");
        }

        [Fact]
        public async Task RespondToInvitationAsync_WhenAccepted_ShouldAddMemberAndFireSignalR()
        {
            // Arrange
            int inviteeId = 2;
            int inviterId = 1;
            int workspaceId = 10;
            int invitationId = 99;

            // ĐÃ FIX: Thêm AvatarUrl và Description
            _db.Users.Add(new ApplicationUser { Id = inviterId, Email = "admin@abc.com", UserName = "admin", AvatarUrl = "" });
            _db.Users.Add(new ApplicationUser { Id = inviteeId, Email = "user@abc.com", UserName = "user", AvatarUrl = "" });
            _db.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Dự án VIP", Description = "" });

            _db.WorkspaceInvitations.Add(new WorkspaceInvitation
            {
                Id = invitationId,
                WorkspaceId = workspaceId,
                InviteeId = inviteeId,
                InviterId = inviterId,
                ExpectedRole = "MEMBER",
                Status = InvitationStatus.PENDING
            });
            await _db.SaveChangesAsync();

            // Act
            var result = await _sut.RespondToInvitationAsync(inviteeId, invitationId, isAccepted: true);

            // Assert
            result.Should().BeTrue();

            var inviteInDb = await _db.WorkspaceInvitations.FindAsync(invitationId);
            inviteInDb!.Status.Should().Be(InvitationStatus.ACCEPTED);

            var member = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.UserId == inviteeId && m.WorkspaceId == workspaceId);
            member.Should().NotBeNull();
            member!.Role.Should().Be("MEMBER");

            _mockClientProxy.Verify(
                client => client.SendCoreAsync(
                    "InvitationResponded",
                    It.Is<object[]>(o => o != null && o.Length == 1),
                    It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPendingInvitationsAsync_ShouldReadUserIdFromHttpContext()
        {
            // Arrange
            int loggedInUserId = 5;

            var claims = new List<Claim> { new Claim("userId", loggedInUserId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);

            // ĐÃ FIX: Thêm AvatarUrl và Description
            _db.Users.Add(new ApplicationUser { Id = 1, UserName = "Inviter", AvatarUrl = "" });
            _db.Workspaces.Add(new Workspace { Id = 10, Name = "Workspace Test", Description = "" });
            _db.WorkspaceInvitations.Add(new WorkspaceInvitation
            {
                Id = 1,
                WorkspaceId = 10,
                InviterId = 1,
                InviteeId = loggedInUserId,
                ExpectedRole = "MEMBER",
                Status = InvitationStatus.PENDING
            });
            await _db.SaveChangesAsync();

            // Act
            var results = await _sut.GetMyPendingInvitationsAsync(loggedInUserId);

            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCount(1);
            results.First().WorkspaceName.Should().Be("Workspace Test");
        }
    }
}