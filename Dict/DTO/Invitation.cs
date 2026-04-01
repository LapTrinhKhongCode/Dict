namespace Dict.DTO
{
    // DTO để Admin gửi request mời
    public class CreateInvitationDTO
    {
        public int WorkspaceId { get; set; }
        public string InviteeEmail { get; set; }
        public string ExpectedRole { get; set; } // WorkspaceRole.ADMIN hoặc WorkspaceRole.MEMBER
    }

    // DTO để trả về danh sách lời mời cho User xem
    public class WorkspaceInvitationDTO
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public string WorkspaceName { get; set; }
        public string InviterName { get; set; }
        public string ExpectedRole { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}