namespace Dict.DTO.Admin
{
    /// <summary>Chi tiết đầy đủ 1 Entry để admin edit bảng con</summary>
    public class EntryDetailDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = null!;
        public string? Type { get; set; }
        public string? ShortMean { get; set; }
        public string? Phonetic { get; set; }
        public string? Romaji { get; set; }
        public string? EntryCategory { get; set; }
        public int? Weight { get; set; }

        // --- Word type ---
        public List<WordAdminDto> Words { get; set; } = new();
        public List<SenseAdminDto> Senses { get; set; } = new();

        // --- Kanji type ---
        public KanjiAdminDto? Kanji { get; set; }
        public List<ReadingElementAdminDto> ReadingElements { get; set; } = new();
    }

    public class WordAdminDto
    {
        public int Id { get; set; }
        public string? WordText { get; set; }
        public string? Phonetic { get; set; }
        public string? Romaji { get; set; }
        public string? ShortMean { get; set; }
        public int? Weight { get; set; }
        public List<WordRelationAdminDto> Opposites { get; set; } = new();
    }

    public class WordRelationAdminDto
    {
        public int Id { get; set; }
        public int RelatedWordId { get; set; }
        public string? RelatedWordText { get; set; }
        public string RelationType { get; set; } = "opposite";
    }

    public class SenseAdminDto
    {
        public int Id { get; set; }
        public string? Pos { get; set; }
        public int? SenseOrder { get; set; }
        public List<GlossAdminDto> Glosses { get; set; } = new();
        public List<ExampleAdminDto> Examples { get; set; } = new();
    }

    public class GlossAdminDto
    {
        public int Id { get; set; }
        public string? Text { get; set; }
    }

    public class ExampleAdminDto
    {
        public int Id { get; set; }
        public string? ContentJp { get; set; }
        public string? ContentTranslated { get; set; }
        public string? Transcription { get; set; }
    }

    public class ReadingElementAdminDto
    {
        public int Id { get; set; }
        public string? Reb { get; set; }        // Âm đọc (On/Kun)
        public string? ReNoKanji { get; set; }  // Kana-only flag
        public string? RePri { get; set; }
    }

    public class KanjiAdminDto
    {
        public int Id { get; set; }
        public string? Character { get; set; }
        public int? StrokeCount { get; set; }
        public string? JlptLevel { get; set; }
        public string? Meaning { get; set; }
        public int? Freq { get; set; }
        public List<KanjiExampleAdminDto> Examples { get; set; } = new();
    }

    public class KanjiExampleAdminDto
    {
        public int Id { get; set; }
        public string? ExampleType { get; set; }  // "on" / "kun"
        public string? ReadingGroup { get; set; }
        public string? Word { get; set; }
        public string? Meaning { get; set; }
        public string? Reading { get; set; }
        public string? HanViet { get; set; }
    }

    // --- Patch DTOs ---
    public class PatchWordDto
    {
        public string? WordText { get; set; }
        public string? Phonetic { get; set; }
        public string? Romaji { get; set; }
        public string? ShortMean { get; set; }
        public int? Weight { get; set; }
    }

    public class PatchSenseDto
    {
        public string? Pos { get; set; }
        public int? SenseOrder { get; set; }
    }

    public class PatchGlossDto
    {
        public string? Text { get; set; }
    }

    public class PatchExampleDto
    {
        public string? ContentJp { get; set; }
        public string? ContentTranslated { get; set; }
        public string? Transcription { get; set; }
    }

    public class PatchReadingElementDto
    {
        public string? Reb { get; set; }
        public string? ReNoKanji { get; set; }
        public string? RePri { get; set; }
    }

    public class PatchKanjiDto
    {
        public int? StrokeCount { get; set; }
        public string? JlptLevel { get; set; }
        public string? Meaning { get; set; }
        public int? Freq { get; set; }
    }

    public class PatchKanjiExampleDto
    {
        public string? ExampleType { get; set; }
        public string? ReadingGroup { get; set; }
        public string? Word { get; set; }
        public string? Meaning { get; set; }
        public string? Reading { get; set; }
        public string? HanViet { get; set; }
    }
}
