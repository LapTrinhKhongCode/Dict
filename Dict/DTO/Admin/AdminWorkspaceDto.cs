namespace Dict.DTO.Admin
{
    public class AdminWorkspaceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Hiển thị tên của người có Role "Admin" trong Workspace
        public string OwnerName { get; set; } = string.Empty;

        // Đếm số lượng thành viên và số lượng dự án
        public int MemberCount { get; set; }
        public int ProjectCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
