// ── DTOs ──────────────────────────────────────────────────────────
namespace Dict.DTO
{
    public class SaveVocabDto
    {
        public string WordText { get; set; }
        public string ContextMeaning { get; set; }
        // Optional source traceability — sent from reader when available
        public int? SourceOcrJobId { get; set; }
        public int? SourcePage { get; set; }
        public string? SourceSentence { get; set; }
    }

    public class VocabDto
    {
        public int Id { get; set; }
        public string WordText { get; set; }
        public string ContextMeaning { get; set; }
        public int AddedBy { get; set; }
        public string AddedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        // Source info
        public int? SourceOcrJobId { get; set; }
        public int? SourcePage { get; set; }
        public string? SourceSentence { get; set; }
        public string? SourceFileName { get; set; }
    }

    public class VocabOccurrenceDto
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public int Page { get; set; }
        public int MatchCount { get; set; }
        public string? Snippet { get; set; }
    }

    public class MyVocabDto
    {
        public int Id { get; set; }
        public string WordText { get; set; }
        public string ContextMeaning { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? SourceOcrJobId { get; set; }
        public int? SourcePage { get; set; }
        public string? SourceSentence { get; set; }
        public string? SourceFileName { get; set; }
        // SRS state
        public int? CardId { get; set; }
        public int? ReviewReps { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? LastReviewedAt { get; set; }
    }
}
