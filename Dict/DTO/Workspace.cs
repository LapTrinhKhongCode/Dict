namespace Dict.DTO
{
    public class CreateWorkspaceDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>Neu co -> tao workspace thuoc Organization (B2B)</summary>
        public int? OrganizationId { get; set; }
    }

    public class UpdateWorkspaceDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class WorkspaceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MyRole { get; set; }
        public int MemberCount { get; set; }
        public string OwnerType { get; set; } = "PERSONAL";
        public int? OrganizationId { get; set; }
        public string? OrgName { get; set; }
        public string? OrgPlan { get; set; }
    }

    public class WorkspaceMemberDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }
    }

    public class InviteMemberDto
    {
        public string Email { get; set; }
        public string Role { get; set; } = "MEMBER";
    }

    public class UpdateMemberRoleDto
    {
        public string Role { get; set; }
    }
}
