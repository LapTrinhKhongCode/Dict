using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Models.Enum;
using Dict.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.Services
{
    public class WorkspaceServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly WorkspaceService _sut;

        public WorkspaceServiceTests()
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

        [Fact]
        public async Task CreateAsync_ShouldCreateWorkspace_AndAssignCreatorAsAdmin()
        {
            // Arrange
            int userId = 100;
            var dto = new CreateWorkspaceDto { Name = "Dự án A", Description = "Mô tả A" };

            // Act
            var result = await _sut.CreateAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Dự án A");
            result.MyRole.Should().Be(Role.ADMIN);

            var savedWorkspace = await _db.Workspaces.Include(w => w.Members).FirstOrDefaultAsync();
            savedWorkspace.Should().NotBeNull();
            savedWorkspace!.Members.Should().HaveCount(1);
            savedWorkspace.Members.First().UserId.Should().Be(userId);
            savedWorkspace.Members.First().Role.Should().Be(Role.ADMIN);
        }

        [Fact]
        public async Task UpdateAsync_WhenUserIsNotAdmin_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            int workspaceId = 1;
            int nonAdminUserId = 200;

            // ĐÃ FIX: Thêm Description
            _db.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Dự án Cũ", Description = "" });
            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = nonAdminUserId,
                Role = "MEMBER"
            });
            await _db.SaveChangesAsync();

            var updateDto = new UpdateWorkspaceDto { Name = "Dự án Mới" };

            // Act & Assert
            Func<Task> act = async () => await _sut.UpdateAsync(workspaceId, nonAdminUserId, updateDto);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Admin mới có quyền thực hiện thao tác này.");
        }

        [Fact]
        public async Task LeaveWorkspaceAsync_WhenUserIsOnlyAdmin_ShouldThrowInvalidOperationException()
        {
            // Arrange
            int workspaceId = 1;
            int onlyAdminId = 100;

            // ĐÃ FIX: Thêm Description
            _db.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Dự án 1", Description = "" });
            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = onlyAdminId,
                Role = "ADMIN"
            });
            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = 200,
                Role = "MEMBER"
            });
            await _db.SaveChangesAsync();

            // Act & Assert
            Func<Task> act = async () => await _sut.LeaveWorkspaceAsync(workspaceId, onlyAdminId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cần có ít nhất 1 Admin. Hãy chỉ định Admin khác trước khi rời.");
        }
    }
}