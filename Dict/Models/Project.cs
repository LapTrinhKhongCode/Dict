using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int WorkspaceId { get; set; }
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Workspace Workspace { get; set; }

        public virtual ApplicationUser CreatedByUser { get; set; }

        public virtual ICollection<ProjectVocabulary> ProjectVocabularies { get; set; }

        public virtual ICollection<OcrJob> OcrJobs { get; set; }
        public virtual ICollection<MediaStore> MediaFiles { get; set; }
    }
}
