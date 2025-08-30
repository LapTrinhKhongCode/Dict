using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Intrinsics.X86;

namespace Dict.Models
{
    public class Entry
    {
        public int Id { get; set; }

        public long? EntSeq { get; set; }

        public string Type { get; set; }

        public string Label { get; set; }

        // raw jmDictionary JSON — keep as string, provider-specific JSON column can be configured
        public string RawJson { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<KanjiElement> KanjiElements { get; set; }
        public virtual ICollection<ReadingElement> ReadingElements { get; set; }
        public virtual ICollection<Sense> Senses { get; set; }
        public virtual ICollection<Word> Words { get; set; }
        public virtual ICollection<WordToEntry> WordToEntries { get; set; }
        public virtual ICollection<Media> Media { get; set; }
        public virtual ICollection<Translation> Translations { get; set; }
    }
}
