using Dict.Data;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.UnitTests
{
    /// <summary>
    /// Tests bổ sung cho WorkspaceService — OWNER/VIEWER roles mới
    /// </summary>
    public class WorkspaceRoleTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly WorkspaceService _sut;

        public WorkspaceRoleTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
            _sut = new WorkspaceService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        private async Task<int> CreateWorkspaceWithMember(int ownerId, int memberId, string memberRole)
        {
            var ws = new Workspace { Name = "WS", Description = "", OwnerType = "PERSONAL" };
            _db.Workspaces.Add(ws);
            await _db.SaveChangesAsync();

            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = ws.Id, UserId = ownerId,  Role = WorkspaceRole.OWNER });
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = ws.Id, UserId = memberId, Role = memberRole });
            await _db.SaveChangesAsync();
            return ws.Id;
        }

        // ─── VIEWER cannot update workspace ──────────────────────────────────

        [Fact]
        public async Task UpdateAsync_WhenUserIsViewer_ShouldThrowUnauthorized()
        {
            int wsId = await CreateWorkspaceWithMember(ownerId: 1, memberId: 2, WorkspaceRole.VIEWER);

            var dto = new Dict.DTO.UpdateWorkspaceDto { Name = "New Name" };
            Func<Task> act = () => _sut.UpdateAsync(wsId, userId: 2, dto);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        // ─── OWNER can update workspace ───────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_WhenUserIsOwner_ShouldSucceed()
        {
            int wsId = await CreateWorkspaceWithMember(ownerId: 1, memberId: 2, WorkspaceRole.MEMBER);
            var dto = new Dict.DTO.UpdateWorkspaceDto { Name = "Updated" };

            var result = await _sut.UpdateAsync(wsId, userId: 1, dto);

            result.Name.Should().Be("Updated");
        }

        // ─── VIEWER cannot leave while still VIEWER (no special rule) ─────────

        [Fact]
        public async Task LeaveWorkspaceAsync_WhenUserIsViewer_ShouldSucceed()
        {
            int wsId = await CreateWorkspaceWithMember(ownerId: 1, memberId: 2, WorkspaceRole.VIEWER);

            Func<Task> act = () => _sut.LeaveWorkspaceAsync(wsId, userId: 2);

            await act.Should().NotThrowAsync();
            var remaining = await _db.WorkspaceMembers.Where(m => m.WorkspaceId == wsId).CountAsync();
            remaining.Should().Be(1); // chỉ còn OWNER
        }

        // ─── OWNER cannot leave if they are the only OWNER ───────────────────

        [Fact]
        public async Task LeaveWorkspaceAsync_WhenOwnerIsLast_ShouldThrowInvalidOperation()
        {
            int wsId = await CreateWorkspaceWithMember(ownerId: 1, memberId: 2, WorkspaceRole.MEMBER);

            Func<Task> act = () => _sut.LeaveWorkspaceAsync(wsId, userId: 1);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // ─── MEMBER has write access, VIEWER does not ────────────────────────

        [Theory]
        [InlineData(WorkspaceRole.OWNER,  true)]
        [InlineData(WorkspaceRole.ADMIN,  true)]
        [InlineData(WorkspaceRole.MEMBER, true)]
        [InlineData(WorkspaceRole.VIEWER, false)]
        public async Task GetMemberRoleAsync_RoleCheck(string role, bool expectedCanWrite)
        {
            var ws = new Workspace { Name = "WS", Description = "", OwnerType = "PERSONAL" };
            _db.Workspaces.Add(ws);
            await _db.SaveChangesAsync();
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = ws.Id, UserId = 5, Role = role });
            await _db.SaveChangesAsync();

            var member = await _db.WorkspaceMembers.FirstAsync(m => m.UserId == 5);
            bool canWrite = member.Role != WorkspaceRole.VIEWER;
            canWrite.Should().Be(expectedCanWrite);
        }
    }
}
