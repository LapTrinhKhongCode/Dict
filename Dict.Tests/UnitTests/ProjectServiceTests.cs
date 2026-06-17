using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
using Dict.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Dict.Tests.UnitTests
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly ProjectService _sut;

        public ProjectServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _sut = new ProjectService(_db, new Mock<IBlobService>().Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_WhenUserIsViewer_ShouldThrowUnauthorizedAccessException()
        {
            var user = CreateUser(1, "viewer", "viewer@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");

            _db.Users.Add(user);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = user.Id, Role = WorkspaceRole.VIEWER });
            await _db.SaveChangesAsync();

            Func<Task> act = async () => await _sut.CreateAsync(workspace.Id, user.Id, new CreateProjectDto
            {
                Name = "Project A",
                Description = "Description"
            });

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Viewer không có quyền chỉnh sửa. Liên hệ Admin để được nâng quyền.");
        }

        [Fact]
        public async Task CreateAsync_WhenUserIsMember_ShouldCreateProject()
        {
            var user = CreateUser(1, "member", "member@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");

            _db.Users.Add(user);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = user.Id, Role = WorkspaceRole.MEMBER });
            await _db.SaveChangesAsync();

            var result = await _sut.CreateAsync(workspace.Id, user.Id, new CreateProjectDto
            {
                Name = "Project A",
                Description = "Description"
            });

            result.Name.Should().Be("Project A");
            result.Description.Should().Be("Description");
            result.WorkspaceId.Should().Be(workspace.Id);
            result.CreatedByUserId.Should().Be(user.Id);
            result.CreatedByUserName.Should().Be(user.UserName);

            var savedProject = await _db.Projects.SingleAsync();
            savedProject.Name.Should().Be("Project A");
            savedProject.CreatedByUserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task UpdateAsync_WhenMemberUpdatesOwnProject_ShouldSucceed()
        {
            var user = CreateUser(1, "member", "member@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");
            var project = CreateProject(100, workspace.Id, user.Id, "Old name", "Old description");

            _db.Users.Add(user);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = user.Id, Role = WorkspaceRole.MEMBER });
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            var result = await _sut.UpdateAsync(project.Id, user.Id, new UpdateProjectDto
            {
                Name = "New name",
                Description = "New description"
            });

            result.Name.Should().Be("New name");
            result.Description.Should().Be("New description");

            var savedProject = await _db.Projects.SingleAsync(p => p.Id == project.Id);
            savedProject.Name.Should().Be("New name");
            savedProject.Description.Should().Be("New description");
        }

        [Fact]
        public async Task UpdateAsync_WhenMemberUpdatesOtherProject_ShouldThrowUnauthorized()
        {
            var caller = CreateUser(1, "member", "member@dict.test");
            var creator = CreateUser(2, "creator", "creator@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");
            var project = CreateProject(100, workspace.Id, creator.Id, "Original", "Description");

            _db.Users.AddRange(caller, creator);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = caller.Id, Role = WorkspaceRole.MEMBER });
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            Func<Task> act = async () => await _sut.UpdateAsync(project.Id, caller.Id, new UpdateProjectDto
            {
                Name = "Updated by member",
                Description = "New description"
            });

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ người tạo, Owner hoặc Admin mới sửa được.");
        }

        [Fact]
        public async Task UpdateAsync_WhenAdminUpdatesAnyProject_ShouldSucceed()
        {
            var admin = CreateUser(1, "admin", "admin@dict.test");
            var creator = CreateUser(2, "creator", "creator@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");
            var project = CreateProject(100, workspace.Id, creator.Id, "Original", "Description");

            _db.Users.AddRange(admin, creator);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = admin.Id, Role = WorkspaceRole.ADMIN });
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            var result = await _sut.UpdateAsync(project.Id, admin.Id, new UpdateProjectDto
            {
                Name = "Admin updated",
                Description = "Changed"
            });

            result.Name.Should().Be("Admin updated");
            result.Description.Should().Be("Changed");

            var savedProject = await _db.Projects.SingleAsync(p => p.Id == project.Id);
            savedProject.Name.Should().Be("Admin updated");
            savedProject.Description.Should().Be("Changed");
        }

        [Fact]
        public async Task UpdateAsync_WhenViewerTriesUpdate_ShouldThrowUnauthorized()
        {
            var viewer = CreateUser(1, "viewer", "viewer@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");
            var project = CreateProject(100, workspace.Id, viewer.Id, "Original", "Description");

            _db.Users.Add(viewer);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = viewer.Id, Role = WorkspaceRole.VIEWER });
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            Func<Task> act = async () => await _sut.UpdateAsync(project.Id, viewer.Id, new UpdateProjectDto
            {
                Name = "Viewer updated",
                Description = "Changed"
            });

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Viewer không có quyền chỉnh sửa. Liên hệ Admin để được nâng quyền.");
        }

        [Fact]
        public async Task GetByWorkspaceAsync_WhenNonMember_ShouldThrowUnauthorized()
        {
            var caller = CreateUser(1, "outsider", "outsider@dict.test");
            var creator = CreateUser(2, "creator", "creator@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");
            var project = CreateProject(100, workspace.Id, creator.Id, "Project A", "Description");

            _db.Users.AddRange(caller, creator);
            _db.Workspaces.Add(workspace);
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            Func<Task> act = async () => await _sut.GetByWorkspaceAsync(workspace.Id, caller.Id);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không thuộc workspace này.");
        }

        [Fact]
        public async Task GetByWorkspaceAsync_WhenMember_ShouldReturnProjects()
        {
            var member = CreateUser(1, "member", "member@dict.test");
            var otherCreator = CreateUser(2, "creator", "creator@dict.test");
            var workspace = CreateWorkspace(10, "Workspace A");
            var olderProject = CreateProject(100, workspace.Id, member.Id, "Older project", "Older description", DateTime.UtcNow.AddMinutes(-10));
            var newerProject = CreateProject(101, workspace.Id, otherCreator.Id, "Newer project", "Newer description", DateTime.UtcNow);

            _db.Users.AddRange(member, otherCreator);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = member.Id, Role = WorkspaceRole.MEMBER });
            _db.Projects.AddRange(olderProject, newerProject);
            await _db.SaveChangesAsync();

            var result = await _sut.GetByWorkspaceAsync(workspace.Id, member.Id);

            result.Should().HaveCount(2);
            result.Select(p => p.Id).Should().Equal(newerProject.Id, olderProject.Id);
            result[0].CreatedByUserName.Should().Be(otherCreator.UserName);
            result[0].MediaCount.Should().Be(0);
            result[0].VocabularyCount.Should().Be(0);
        }

        private static ApplicationUser CreateUser(int id, string userName, string email)
        {
            return new ApplicationUser
            {
                Id = id,
                AvatarUrl = string.Empty,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
        }

        private static Workspace CreateWorkspace(int id, string name)
        {
            return new Workspace
            {
                Id = id,
                Name = name,
                Description = string.Empty,
                CreatedAt = DateTime.UtcNow,
                OwnerType = "PERSONAL"
            };
        }

        private static Project CreateProject(int id, int workspaceId, int createdByUserId, string name, string description, DateTime? createdAt = null)
        {
            return new Project
            {
                Id = id,
                WorkspaceId = workspaceId,
                CreatedByUserId = createdByUserId,
                Name = name,
                Description = description,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                OcrJobs = new List<OcrJob>(),
                ProjectVocabularies = new List<ProjectVocabulary>(),
                MediaFiles = new List<MediaStore>()
            };
        }
    }
}
