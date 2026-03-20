using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Reflection.Metadata.BlobBuilder;

namespace Dict.Models
{
    public class Sense
    {
        public int Id { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public string Pos { get; set; }

        public string Field { get; set; }

        public string Misc { get; set; }

        public string SInf { get; set; }

        public string Dialect { get; set; }

        public int? SenseOrder { get; set; }

        public virtual ICollection<Gloss> Glosses { get; set; }
        public virtual ICollection<Example> Examples { get; set; }
        public virtual ICollection<SynsetEntry> SynsetEntries { get; set; } = new List<SynsetEntry>();
    }
}
