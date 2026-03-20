using Dict.DTO;

namespace Dict.Service.IService
{
    public interface IWorkspaceService
    {
        // Workspace CRUD
        Task<List<WorkspaceDto>> GetMyWorkspacesAsync(int userId);
        Task<WorkspaceDto> GetByIdAsync(int workspaceId, int userId);
        Task<WorkspaceDto> CreateAsync(int userId, CreateWorkspaceDto dto);
        Task<WorkspaceDto> UpdateAsync(int workspaceId, int userId, UpdateWorkspaceDto dto);
        Task DeleteAsync(int workspaceId, int userId);

        // Members
        Task<List<WorkspaceMemberDto>> GetMembersAsync(int workspaceId, int userId);
        Task InviteMemberAsync(int workspaceId, int requesterId, InviteMemberDto dto);
        Task UpdateMemberRoleAsync(int workspaceId, int requesterId, int targetUserId, UpdateMemberRoleDto dto);
        Task RemoveMemberAsync(int workspaceId, int requesterId, int targetUserId);
        Task LeaveWorkspaceAsync(int workspaceId, int userId);
    }
}
