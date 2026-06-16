namespace Dict.Models
{
    public class Workspace
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>PERSONAL | ORGANIZATION</summary>
        public string OwnerType { get; set; } = "PERSONAL";

        /// <summary>FK đến Organization nếu OwnerType = ORGANIZATION, null nếu personal</summary>
        public int? OrganizationId { get; set; }
        public virtual Organization? Organization { get; set; }

        // --- Navigation Properties ---
        public virtual ICollection<WorkspaceMember> Members { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
        public virtual ICollection<MediaStore> MediaFiles { get; set; }
    }
}
