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

        private async Task RequireAdminAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);
            if (member.Role != Role.ADMIN)
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền thực hiện thao tác này.");
        }

        private static WorkspaceDto ToDto(Workspace w, string myRole) => new()
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            CreatedAt = w.CreatedAt,
            MyRole = myRole,
            MemberCount = w.Members?.Count ?? 0
        };

        // ── Workspace CRUD ────────────────────────────────────────
        public async Task<List<WorkspaceDto>> GetMyWorkspacesAsync(int userId)
        {
            return await _db.WorkspaceMembers
                .Where(m => m.UserId == userId)
                .Include(m => m.Workspace)
                    .ThenInclude(w => w.Members)
                .Select(m => new WorkspaceDto
                {
                    Id = m.Workspace.Id,
                    Name = m.Workspace.Name,
                    Description = m.Workspace.Description,
                    CreatedAt = m.Workspace.CreatedAt,
                    MyRole = m.Role,
                    MemberCount = m.Workspace.Members.Count
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

        public async Task<WorkspaceDto> CreateAsync(int userId, CreateWorkspaceDto dto)
        {
            var workspace = new Workspace
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Workspaces.Add(workspace);
            await _db.SaveChangesAsync();

            // Người tạo tự động trở thành Admin
            var member = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = Role.ADMIN
            };
            _db.WorkspaceMembers.Add(member);
            await _db.SaveChangesAsync();

            return ToDto(workspace, Role.ADMIN);
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
            await RequireAdminAsync(workspaceId, userId);

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

        public async Task UpdateMemberRoleAsync(int workspaceId, int requesterId, int targetUserId, UpdateMemberRoleDto dto)
        {
            await RequireAdminAsync(workspaceId, requesterId);

            // Không tự đổi role của chính mình
            if (requesterId == targetUserId)
                throw new InvalidOperationException("Không thể tự đổi role của chính mình.");

            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId)
                ?? throw new KeyNotFoundException("Thành viên không tồn tại.");

            member.Role = dto.Role;
            await _db.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(int workspaceId, int requesterId, int targetUserId)
        {
            await RequireAdminAsync(workspaceId, requesterId);

            // Admin không thể tự xóa mình
            if (requesterId == targetUserId)
                throw new InvalidOperationException("Dùng 'Rời workspace' để tự xóa mình.");

            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId)
                ?? throw new KeyNotFoundException("Thành viên không tồn tại.");

            _db.WorkspaceMembers.Remove(member);
            await _db.SaveChangesAsync();
        }

        public async Task LeaveWorkspaceAsync(int workspaceId, int userId)
        {
            var member = await GetMemberOrThrowAsync(workspaceId, userId);

            // Nếu là Admin duy nhất thì không được rời
            if (member.Role == "Admin")
            {
                var adminCount = await _db.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspaceId && m.Role == "Admin");
                if (adminCount <= 1)
                    throw new InvalidOperationException("Cần có ít nhất 1 Admin. Hãy chỉ định Admin khác trước khi rời.");
            }

            _db.WorkspaceMembers.Remove(member);
            await _db.SaveChangesAsync();
        }
    }
}
