namespace Dict.DTO
{
    public class CreateWorkspaceDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
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
        public string MyRole { get; set; } // Role của user đang gọi API
        public int MemberCount { get; set; }
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
        public string Email { get; set; }   // Tìm user theo email
        public string Role { get; set; } = "Member"; // "Admin" | "Member"
    }

    public class UpdateMemberRoleDto
    {
        public string Role { get; set; } // "Admin" | "Member"
    }
}