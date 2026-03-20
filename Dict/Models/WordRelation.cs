using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class WordRelation
    {
        public int Id { get; set; }

        public int WordId { get; set; }
        public virtual Word Word { get; set; }

        public int RelatedWordId { get; set; }
        public virtual Word RelatedWord { get; set; }

        public string RelationType { get; set; }

        public string Note { get; set; }
    }
}
