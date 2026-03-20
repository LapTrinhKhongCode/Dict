using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class WordToEntry
    {
        public int Id { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public string MappingType { get; set; }
    }
}
