namespace Dict.Models
{
    public class WorkspaceMember
    {
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }

        // Vai trò trong công ty: "Admin" (Quản lý), "Member" (Nhân viên)
        public string Role { get; set; }

        // --- Navigation Properties ---
        public virtual Workspace Workspace { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
