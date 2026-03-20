using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class CardState
    {
        public int Id { get; set; }

        public int? CardId { get; set; }
        public virtual Card Card { get; set; }

        public int? UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime? DueDate { get; set; }
        public int? Interval { get; set; }
        public float? Ease { get; set; }
        public int? Reps { get; set; }
        public int? Lapses { get; set; }
        public int? DeckPosition { get; set; }
        public DateTime? LastReviewedAt { get; set; }
        public bool? Suspended { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ReviewLog> ReviewLogs { get; set; }
    }
}
