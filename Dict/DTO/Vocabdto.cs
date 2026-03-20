// ── DTOs ──────────────────────────────────────────────────────────
namespace Dict.DTO
{
    public class SaveVocabDto
    {
        public string WordText { get; set; }
        public string ContextMeaning { get; set; }
    }

    public class VocabDto
    {
        public int Id { get; set; }
        public string WordText { get; set; }
        public string ContextMeaning { get; set; }
        public int AddedBy { get; set; }
        public string AddedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}