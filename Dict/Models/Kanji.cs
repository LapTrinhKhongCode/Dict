using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Kanji
    {
        public int Id { get; set; }

        public string Character { get; set; }

        public int? StrokeCount { get; set; }
        public int? Grade { get; set; }

        public string JlptLevel { get; set; }

        public string Meaning { get; set; }

        public int? Freq { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<WordKanji> WordKanji { get; set; }

        public virtual ICollection<KanjiExample> KanjiExamples { get; set; } = new List<KanjiExample>();
    }
}

