namespace Dict.Models
{
    public class Workspace
    {
        public int Id { get; set; }
        public string Name { get; set; } // Tên công ty/Tổ chức (VD: FPT Software)
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // --- Navigation Properties ---
        public virtual ICollection<WorkspaceMember> Members { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
        public virtual ICollection<MediaStore> MediaFiles { get; set; } // File upload lên phải thuộc về Workspace
    }
}
