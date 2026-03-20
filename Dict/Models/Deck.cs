using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Deck
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        public bool? IsPublic { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Card> Cards { get; set; }
    }

}
