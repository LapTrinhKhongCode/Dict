using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dict.Controllers;
using Dict.Data;
using Dict.DTO;
using Dict.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.UnitTests
{
    public class OrganizationsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public OrganizationsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task Create_ShouldCreateOrg_AndAddCreatorAsOwner()
        {
            var creator = CreateUser(1, "owner", "owner@dict.test");
            _db.Users.Add(creator);
            await _db.SaveChangesAsync();

            var controller = CreateController(creator.Id);

            var result = await controller.Create(new CreateOrgDto
            {
                Name = "Acme Team",
                Description = "Org description"
            });

            result.Should().BeOfType<OkObjectResult>();

            var org = await _db.Organizations.SingleAsync();
            org.Name.Should().Be("Acme Team");
            org.Slug.Should().Be("acme-team");
            org.OwnerId.Should().Be(creator.Id);
            org.OrgPlan.Should().Be(OrgPlan.FREE);

            var member = await _db.OrganizationMembers.SingleAsync();
            member.OrganizationId.Should().Be(org.Id);
            member.UserId.Should().Be(creator.Id);
            member.OrgRole.Should().Be(OrgRole.OWNER);
        }

        [Fact]
        public async Task InviteMember_WhenMemberAlreadyExists_ReturnsConflict()
        {
            var owner = CreateUser(1, "owner", "owner@dict.test");
            var invitee = CreateUser(2, "invitee", "invitee@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);

            _db.Users.AddRange(owner, invitee);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.AddRange(
                CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER),
                CreateOrganizationMember(org.Id, invitee.Id, OrgRole.MEMBER));
            await _db.SaveChangesAsync();

            var controller = CreateController(owner.Id);

            var result = await controller.InviteMember(org.Id, new InviteOrgMemberDto
            {
                Email = invitee.Email!
            });

            var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflict.Value.Should().BeOfType<ResponseDTO>();
            ((ResponseDTO)conflict.Value!).Message.Should().Contain("User đã là thành viên");
        }

        [Fact]
        public async Task InviteMember_WhenUserNotFound_ReturnsNotFound()
        {
            var admin = CreateUser(1, "admin", "admin@dict.test");
            var org = CreateOrganization(10, "Acme", admin.Id);

            _db.Users.Add(admin);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.Add(CreateOrganizationMember(org.Id, admin.Id, OrgRole.ADMIN));
            await _db.SaveChangesAsync();

            var controller = CreateController(admin.Id);

            var result = await controller.InviteMember(org.Id, new InviteOrgMemberDto
            {
                Email = "missing@dict.test"
            });

            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            ((ResponseDTO)notFound.Value!).Message.Should().Contain("Không tìm thấy user");
        }

        [Fact]
        public async Task InviteMember_WhenCallerIsMember_ReturnsForbid()
        {
            var caller = CreateUser(1, "member", "member@dict.test");
            var invitee = CreateUser(2, "invitee", "invitee@dict.test");
            var org = CreateOrganization(10, "Acme", caller.Id);

            _db.Users.AddRange(caller, invitee);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.Add(CreateOrganizationMember(org.Id, caller.Id, OrgRole.MEMBER));
            await _db.SaveChangesAsync();

            var controller = CreateController(caller.Id);

            var result = await controller.InviteMember(org.Id, new InviteOrgMemberDto
            {
                Email = invitee.Email!
            });

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task InviteMember_ShouldAutoAddToOrgWorkspaces()
        {
            var owner = CreateUser(1, "owner", "owner@dict.test");
            var invitee = CreateUser(2, "invitee", "invitee@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);
            var workspaceA = CreateWorkspace(100, "WS A", org.Id, "ORGANIZATION");
            var workspaceB = CreateWorkspace(101, "WS B", org.Id, "ORGANIZATION");
            var personalWorkspace = CreateWorkspace(102, "Personal", null, "PERSONAL");

            _db.Users.AddRange(owner, invitee);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.Add(CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER));
            _db.Workspaces.AddRange(workspaceA, workspaceB, personalWorkspace);
            await _db.SaveChangesAsync();

            var controller = CreateController(owner.Id);

            var result = await controller.InviteMember(org.Id, new InviteOrgMemberDto
            {
                Email = invitee.Email!,
                Role = OrgRole.MEMBER
            });

            result.Should().BeOfType<OkObjectResult>();

            var orgMember = await _db.OrganizationMembers.SingleAsync(m => m.OrganizationId == org.Id && m.UserId == invitee.Id);
            orgMember.OrgRole.Should().Be(OrgRole.MEMBER);

            var workspaceMemberships = await _db.WorkspaceMembers
                .Where(wm => wm.UserId == invitee.Id)
                .OrderBy(wm => wm.WorkspaceId)
                .ToListAsync();

            workspaceMemberships.Should().HaveCount(2);
            workspaceMemberships.Select(wm => wm.WorkspaceId).Should().Equal(workspaceA.Id, workspaceB.Id);
            workspaceMemberships.Should().OnlyContain(wm => wm.Role == WorkspaceRole.MEMBER);
        }

        [Fact]
        public async Task RemoveMember_WhenTargetIsOwner_ReturnsBadRequest()
        {
            var admin = CreateUser(1, "admin", "admin@dict.test");
            var owner = CreateUser(2, "owner", "owner@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);

            _db.Users.AddRange(admin, owner);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.AddRange(
                CreateOrganizationMember(org.Id, admin.Id, OrgRole.ADMIN),
                CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER));
            await _db.SaveChangesAsync();

            var controller = CreateController(admin.Id);

            var result = await controller.RemoveMember(org.Id, owner.Id);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            ((ResponseDTO)badRequest.Value!).Message.Should().Contain("Không thể xoá Owner");
            (await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == org.Id && m.UserId == owner.Id)).Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMember_ShouldAutoRemoveFromOrgWorkspaces()
        {
            var owner = CreateUser(1, "owner", "owner@dict.test");
            var member = CreateUser(2, "member", "member@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);
            var workspaceA = CreateWorkspace(100, "WS A", org.Id, "ORGANIZATION");
            var workspaceB = CreateWorkspace(101, "WS B", org.Id, "ORGANIZATION");
            var unrelatedWorkspace = CreateWorkspace(102, "Other", null, "PERSONAL");

            _db.Users.AddRange(owner, member);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.AddRange(
                CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER),
                CreateOrganizationMember(org.Id, member.Id, OrgRole.MEMBER));
            _db.Workspaces.AddRange(workspaceA, workspaceB, unrelatedWorkspace);
            _db.WorkspaceMembers.AddRange(
                new WorkspaceMember { WorkspaceId = workspaceA.Id, UserId = member.Id, Role = WorkspaceRole.MEMBER },
                new WorkspaceMember { WorkspaceId = workspaceB.Id, UserId = member.Id, Role = WorkspaceRole.ADMIN },
                new WorkspaceMember { WorkspaceId = unrelatedWorkspace.Id, UserId = member.Id, Role = WorkspaceRole.MEMBER });
            await _db.SaveChangesAsync();

            var controller = CreateController(owner.Id);

            var result = await controller.RemoveMember(org.Id, member.Id);

            result.Should().BeOfType<OkObjectResult>();
            (await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == org.Id && m.UserId == member.Id)).Should().BeFalse();

            var remainingWorkspaceMemberships = await _db.WorkspaceMembers
                .Where(wm => wm.UserId == member.Id)
                .OrderBy(wm => wm.WorkspaceId)
                .ToListAsync();

            remainingWorkspaceMemberships.Should().ContainSingle();
            remainingWorkspaceMemberships.Single().WorkspaceId.Should().Be(unrelatedWorkspace.Id);
        }

        [Fact]
        public async Task Delete_WhenCallerIsNotOwner_ReturnsForbidden()
        {
            var owner = CreateUser(1, "owner", "owner@dict.test");
            var admin = CreateUser(2, "admin", "admin@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);

            _db.Users.AddRange(owner, admin);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.AddRange(
                CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER),
                CreateOrganizationMember(org.Id, admin.Id, OrgRole.ADMIN));
            await _db.SaveChangesAsync();

            var controller = CreateController(admin.Id);

            var result = await controller.Delete(org.Id);

            var forbidden = result.Should().BeOfType<ObjectResult>().Subject;
            forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            (await _db.Organizations.AnyAsync(o => o.Id == org.Id)).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WhenCallerIsOwner_ReturnsOk()
        {
            var owner = CreateUser(1, "owner", "owner@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);

            _db.Users.Add(owner);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.Add(CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER));
            await _db.SaveChangesAsync();

            var controller = CreateController(owner.Id);

            var result = await controller.Delete(org.Id);

            result.Should().BeOfType<OkObjectResult>();
            (await _db.Organizations.AnyAsync(o => o.Id == org.Id)).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateMemberRole_WhenCallerIsNotOwner_ReturnsForbidden()
        {
            var owner = CreateUser(1, "owner", "owner@dict.test");
            var admin = CreateUser(2, "admin", "admin@dict.test");
            var member = CreateUser(3, "member", "member@dict.test");
            var org = CreateOrganization(10, "Acme", owner.Id);

            _db.Users.AddRange(owner, admin, member);
            _db.Organizations.Add(org);
            _db.OrganizationMembers.AddRange(
                CreateOrganizationMember(org.Id, owner.Id, OrgRole.OWNER),
                CreateOrganizationMember(org.Id, admin.Id, OrgRole.ADMIN),
                CreateOrganizationMember(org.Id, member.Id, OrgRole.MEMBER));
            await _db.SaveChangesAsync();

            var controller = CreateController(admin.Id);

            var result = await controller.UpdateMemberRole(org.Id, member.Id, new UpdateOrgMemberRoleDto
            {
                OrgRole = OrgRole.ADMIN
            });

            var forbidden = result.Should().BeOfType<ObjectResult>().Subject;
            forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

            var savedMember = await _db.OrganizationMembers.SingleAsync(m => m.OrganizationId == org.Id && m.UserId == member.Id);
            savedMember.OrgRole.Should().Be(OrgRole.MEMBER);
        }

        private OrganizationsController CreateController(int userId)
        {
            var controller = new OrganizationsController(_db);
            var claims = new[] { new Claim("userId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "test");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            return controller;
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

        private static Organization CreateOrganization(int id, string name, int ownerId)
        {
            return new Organization
            {
                Id = id,
                Name = name,
                Slug = name.ToLowerInvariant().Replace(" ", "-"),
                OwnerId = ownerId,
                OrgPlan = OrgPlan.FREE,
                MaxMembers = 10,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static OrganizationMember CreateOrganizationMember(int organizationId, int userId, string role)
        {
            return new OrganizationMember
            {
                OrganizationId = organizationId,
                UserId = userId,
                OrgRole = role,
                JoinedAt = DateTime.UtcNow
            };
        }

        private static Workspace CreateWorkspace(int id, string name, int? organizationId, string ownerType)
        {
            return new Workspace
            {
                Id = id,
                Name = name,
                Description = string.Empty,
                OrganizationId = organizationId,
                OwnerType = ownerType,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
