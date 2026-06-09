using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class WordComment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Entry.Label của từ được comment (e.g. "食べる", "日本語")</summary>
        [Required]
        public string WordLabel { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
