using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ReviewLog
    {
        public int Id { get; set; }

        public int? CardStateId { get; set; }
        public virtual CardState CardState { get; set; }

        public int? UserId { get; set; }
        public virtual User User { get; set; }

        public int? CardId { get; set; }
        public virtual Card Card { get; set; }

        public DateTime? Timestamp { get; set; }
        public int? Quality { get; set; }
        public int? TimeTakenMs { get; set; }
        public int? PreviousInterval { get; set; }
        public int? NewInterval { get; set; }
        public float? Ease { get; set; }
        public string Note { get; set; }
    }
    
}
