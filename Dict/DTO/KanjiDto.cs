namespace Dict.DTO
{
    public class KanjiDto
    {
        public string Character { get; set; }
        public int? StrokeCount { get; set; }
        public int? Grade { get; set; }
        public string JlptLevel { get; set; }
        public string Meaning { get; set; }
        public int? Freq { get; set; }
        public List<WordSummaryDto> Words { get; set; } = new();
    }
}
