using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Translation
    {
        public int Id { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public int? LanguageId { get; set; }
        public virtual Language Language { get; set; }

        public string Definition { get; set; }

        public string Kind { get; set; }

        public string ExamplesJson { get; set; }

        public string Source { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
