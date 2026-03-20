namespace Dict.Models
{
    public class KanjiTag
    {
        public int KanjiId { get; set; }
        public Kanji Kanji { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
