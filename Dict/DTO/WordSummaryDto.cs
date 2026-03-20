namespace Dict.DTO
{
    public class WordSummaryDto
    {
        public int WordId { get; set; }
        public string WordText { get; set; }
        public string Phonetic { get; set; }
        public string Romaji { get; set; }
        public string ShortMean { get; set; }
        public List<string> KanjiInWord { get; set; } = new();      // keb from Entry.kanji_elements
        public List<EntryDto> Entries { get; set; } = new();
        public List<TranslationDto> Translations { get; set; } = new();
        public List<MediaDto> Media { get; set; } = new();
    }
}
