using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Models.Enum;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using System;

namespace Dict.Service
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly ApplicationDbContext _db;

        public WorkspaceService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── Helpers ──────────────────────────────────────────────
        private async Task<WorkspaceMember> GetMemberOrThrowAsync(int workspaceId, int userId)
        {
            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không thuộc workspace này.");
            return member;
        }

        /// <summary>OWNER | ADMIN | MEMBER — có thể tạo/sửa nội dung. VIEWER bị từ chối.</summary>
        private async Task RequireContributorAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);
            if (member.Role == WorkspaceRole.VIEWER)
                throw new UnauthorizedAccessException("Viewer không có quyền chỉnh sửa. Liên hệ Admin để được nâng quyền.");
        }

        /// <summary>OWNER | ADMIN — có thể quản lý member/project/workspace.</summary>
        private async Task RequireAdminAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);
            if (member.Role != WorkspaceRole.OWNER && member.Role != WorkspaceRole.ADMIN)
                throw new UnauthorizedAccessException("Chỉ Owner/Admin mới có quyền thực hiện thao tác này.");
        }

        /// <summary>OWNER only — xóa workspace, transfer ownership.</summary>
        private async Task RequireOwnerAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);
            if (member.Role != WorkspaceRole.OWNER)
                throw new UnauthorizedAccessException("Chỉ Owner mới có quyền thực hiện thao tác này.");
        }

        private static WorkspaceDto ToDto(Workspace w, string myRole) => new()
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            CreatedAt = w.CreatedAt,
            MyRole = myRole,
            MemberCount = w.Members?.Count ?? 0,
            OwnerType = w.OwnerType ?? "PERSONAL",
            OrganizationId = w.OrganizationId,
            OrgName = w.Organization?.Name,
            OrgPlan = w.Organization?.OrgPlan,
        };

        // ── Workspace CRUD ────────────────────────────────────────
        public async Task<List<WorkspaceDto>> GetMyWorkspacesAsync(int userId)
        {
            return await _db.WorkspaceMembers
                .Where(m => m.UserId == userId)
                .Include(m => m.Workspace)
                    .ThenInclude(w => w.Members)
                .Include(m => m.Workspace)
                    .ThenInclude(w => w.Organization)
                .Select(m => new WorkspaceDto
                {
                    Id = m.Workspace.Id,
                    Name = m.Workspace.Name,
                    Description = m.Workspace.Description,
                    CreatedAt = m.Workspace.CreatedAt,
                    MyRole = m.Role,
                    MemberCount = m.Workspace.Members.Count,
                    OwnerType = m.Workspace.OwnerType ?? "PERSONAL",
                    OrganizationId = m.Workspace.OrganizationId,
                    OrgName = m.Workspace.Organization != null ? m.Workspace.Organization.Name : null,
                    OrgPlan = m.Workspace.Organization != null ? m.Workspace.Organization.OrgPlan : null,
                })
                .ToListAsync();
        }

        public async Task<WorkspaceDto> GetByIdAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);
            var workspace = await _db.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId)
                ?? throw new KeyNotFoundException("Workspace không tồn tại.");

            return ToDto(workspace, member.Role);
        }

        public async Task<WorkspaceDto> CreateAsync(int userId, CreateWorkspaceDto dto, bool isPlatformAdmin = false)
        {
            Organization? org = null;
            if (dto.OrganizationId.HasValue)
            {
                org = await _db.Organizations
                    .Include(o => o.Members)
                    .FirstOrDefaultAsync(o => o.Id == dto.OrganizationId.Value);
                if (org == null)
                    throw new KeyNotFoundException("Organization không tồn tại.");

                if (!isPlatformAdmin)
                {
                    var orgMembership = org.Members.FirstOrDefault(m => m.UserId == userId);
                    if (orgMembership == null || (orgMembership.OrgRole != Models.OrgRole.OWNER && orgMembership.OrgRole != Models.OrgRole.ADMIN))
                        throw new UnauthorizedAccessException("Chỉ Owner/Admin của tổ chức mới có thể tạo workspace cho tổ chức.");
                }
            }

            var workspace = new Workspace
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerType = dto.OrganizationId.HasValue ? "ORGANIZATION" : "PERSONAL",
                OrganizationId = dto.OrganizationId,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Workspaces.Add(workspace);
            await _db.SaveChangesAsync();

            // Người tạo là OWNER
            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = WorkspaceRole.OWNER
            });

            // B2B: auto-add tất cả org members với role MEMBER
            if (org != null)
            {
                foreach (var orgMember in org.Members.Where(m => m.UserId != userId))
                {
                    _db.WorkspaceMembers.Add(new WorkspaceMember
                    {
                        WorkspaceId = workspace.Id,
                        UserId = orgMember.UserId,
                        Role = WorkspaceRole.MEMBER
                    });
                }
            }

            await _db.SaveChangesAsync();

            // Reload với Organization
            var created = await _db.Workspaces
                .Include(w => w.Members)
                .Include(w => w.Organization)
                .FirstAsync(w => w.Id == workspace.Id);

            return ToDto(created, WorkspaceRole.OWNER);
        }

        public async Task<WorkspaceDto> UpdateAsync(int workspaceId, int userId, UpdateWorkspaceDto dto)
        {
            await RequireAdminAsync(workspaceId, userId);

            var workspace = await _db.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId)
                ?? throw new KeyNotFoundException("Workspace không tồn tại.");

            workspace.Name = dto.Name ?? workspace.Name;
            workspace.Description = dto.Description ?? workspace.Description;
            await _db.SaveChangesAsync();

            var myRole = workspace.Members.First(m => m.UserId == userId).Role;
            return ToDto(workspace, myRole);
        }

        public async Task DeleteAsync(int workspaceId, int userId)
        {
            // canDeleteWorkspace → chỉ OWNER mới được xoá
            await RequireOwnerAsync(workspaceId, userId);

            var workspace = await _db.Workspaces.FindAsync(workspaceId)
                ?? throw new KeyNotFoundException("Workspace không tồn tại.");

            _db.Workspaces.Remove(workspace);
            await _db.SaveChangesAsync();
        }

        // ── Members ───────────────────────────────────────────────
        public async Task<List<WorkspaceMemberDto>> GetMembersAsync(int workspaceId, int userId)
        {
            await GetMemberOrThrowAsync(workspaceId, userId);

            return await _db.WorkspaceMembers
                .Where(m => m.WorkspaceId == workspaceId)
                .Include(m => m.User)
                .Select(m => new WorkspaceMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User.UserName,
                    Email = m.User.Email,
                    AvatarUrl = m.User.AvatarUrl,
                    Role = m.Role
                })
                .ToListAsync();
        }

        public async Task InviteMemberAsync(int workspaceId, int requesterId, InviteMemberDto dto)
        {
            await RequireAdminAsync(workspaceId, requesterId);

            // Tìm user theo email
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new KeyNotFoundException($"Không tìm thấy user với email {dto.Email}.");

            // Kiểm tra đã là member chưa
            var exists = await _db.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == user.Id);
            if (exists)
                throw new InvalidOperationException("User đã là thành viên của workspace này.");

            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = user.Id,
                Role = dto.Role
            });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMemberRoleAsync(int workspaceId, int requesterId, int targetUserId, UpdateMemberRoleDto dto, bool isPlatformAdmin = false)
        {
            if (!isPlatformAdmin)
                await RequireAdminAsync(workspaceId, requesterId);

            if (!isPlatformAdmin && requesterId == targetUserId)
                throw new InvalidOperationException("Không thể tự đổi role của chính mình.");

            var validRoles = new[] { WorkspaceRole.ADMIN, WorkspaceRole.MEMBER, WorkspaceRole.VIEWER };
            var normalizedRole = dto.Role?.ToUpper();
            if (!validRoles.Contains(normalizedRole))
                throw new ArgumentException($"Role không hợp lệ: '{dto.Role}'. Các role hợp lệ: ADMIN, MEMBER, VIEWER");

            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId)
                ?? throw new KeyNotFoundException("Thành viên không tồn tại.");

            if (member.Role == WorkspaceRole.OWNER && !isPlatformAdmin)
            {
                var requesterMember = await _db.WorkspaceMembers
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId);
                if (requesterMember?.Role != WorkspaceRole.OWNER)
                    throw new UnauthorizedAccessException("Chỉ Owner mới có thể đổi role của Owner khác.");
            }

            member.Role = normalizedRole!;
            await _db.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(int workspaceId, int requesterId, int targetUserId, bool isPlatformAdmin = false)
        {
            if (!isPlatformAdmin)
                await RequireAdminAsync(workspaceId, requesterId);

            if (!isPlatformAdmin && requesterId == targetUserId)
                throw new InvalidOperationException("Dùng 'Rời workspace' để tự xóa mình.");

            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId)
                ?? throw new KeyNotFoundException("Thành viên không tồn tại.");

            // ADMIN không được xóa OWNER — chỉ OWNER mới xóa được OWNER khác (và platform admin)
            if (member.Role == WorkspaceRole.OWNER && !isPlatformAdmin)
            {
                var requesterMember = await _db.WorkspaceMembers
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId);
                if (requesterMember?.Role != WorkspaceRole.OWNER)
                    throw new UnauthorizedAccessException("Chỉ Owner mới có thể xóa Owner khác.");
            }

            _db.WorkspaceMembers.Remove(member);
            await _db.SaveChangesAsync();
        }

        public async Task LeaveWorkspaceAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);

            // Nếu là Admin duy nhất thì không được rời
            if (member.Role == "ADMIN")
            {
                var adminCount = await _db.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == "ADMIN");
                if (adminCount <= 1)
                    throw new InvalidOperationException("Cần có ít nhất 1 Admin. Hãy chỉ định Admin khác trước khi rời.");
            }

            _db.WorkspaceMembers.Remove(member);
            await _db.SaveChangesAsync();
        }
    }
}
