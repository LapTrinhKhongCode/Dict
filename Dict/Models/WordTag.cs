using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class WordTag
    {
        public int Id { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public int? TagId { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
