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

        // Source traceability
        public int? SourceOcrJobId { get; set; }
        public int? SourcePage { get; set; }
        public string? SourceSentence { get; set; }

        public int? CardId { get; set; }

        public virtual Card LinkedCard { get; set; }

        public virtual Project Project { get; set; }
        public virtual ApplicationUser UserAdded { get; set; }
        [ForeignKey(nameof(SourceOcrJobId))]
        public virtual OcrJob SourceOcrJob { get; set; }
    }
}
