namespace Dict.DTO.Admin
{
    public class EntryAdminDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = null!;
        public string? ShortMean { get; set; }
        public string? Phonetic { get; set; }
        public string? Romaji { get; set; }
        public string? Type { get; set; }
        public string? EntryCategory { get; set; }
        public int? Weight { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Kanji-specific (join từ bảng Kanji)
        public string? Meaning { get; set; }
        public int? StrokeCount { get; set; }
        public string? JlptLevel { get; set; }
        public int? Freq { get; set; }
        public int? Grade { get; set; }
    }

    public class UpdateEntryDto
    {
        public string? ShortMean { get; set; }
        public string? Phonetic { get; set; }
        public string? Romaji { get; set; }
        public string? EntryCategory { get; set; }
        public int? Weight { get; set; }
    }
}
