using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ProjectVocabulary
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public string WordText { get; set; }

        public string ContextMeaning { get; set; }

        public int AddedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Project Project { get; set; }

        public virtual ApplicationUser UserAdded { get; set; }
    }
}
