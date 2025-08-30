using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Card
    {
        public int Id { get; set; }

        public int? DeckId { get; set; }
        public virtual Deck Deck { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public string Template { get; set; } // json
        public string FrontText { get; set; }
        public string BackText { get; set; }
        public string Tags { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<CardState> CardStates { get; set; }
        public virtual ICollection<ReviewLog> ReviewLogs { get; set; }
    }
}
