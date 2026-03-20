using System.ComponentModel.DataAnnotations.Schema;
namespace Dict.Models
{
    public class KanjiExample
    {
        public int Id { get; set; }
        public int KanjiId { get; set; }
        public string ExampleType { get; set; }
        public string? ReadingGroup { get; set; }
        public string Word { get; set; }
        public string Meaning { get; set; }
        public string Reading { get; set; }
        public string? HanViet { get; set; }
        public virtual Kanji Kanji { get; set; }
    }
}