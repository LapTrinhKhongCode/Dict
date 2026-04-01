using Dict.DTO;

namespace Dict.Service.IService
{
    public interface IWorkspaceInvitationService
    {
        Task<WorkspaceInvitationDTO> InviteMemberAsync(int inviterId, CreateInvitationDTO dto);
        Task<IEnumerable<WorkspaceInvitationDTO>> GetMyPendingInvitationsAsync(int userId);
        Task<bool> RespondToInvitationAsync(int userId, int invitationId, bool isAccepted);
    }
}
