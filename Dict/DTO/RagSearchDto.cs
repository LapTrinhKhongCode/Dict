namespace Dict.DTO
{
    // ==============================================================
    // DÀNH CHO API 1: TÌM KIẾM VECTOR NHANH (RAG)
    // ==============================================================

    // Request gửi lên API 1
    public class RagSearchRequestDto
    {
        public string Keyword { get; set; } = string.Empty; // Ví dụ: "する"
        public string Context { get; set; } = string.Empty; // Ví dụ: "このパソコンは20万円もする。"
    }

    // Response trả về từ API 1
    public class RagSearchResponseDto
    {
        public string Word { get; set; } = string.Empty;
        // Ở API 1, hai trường dưới này sẽ để trống (chưa gọi AI)
        public string BestMeaning { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;

        // Chứa Top 5 ví dụ lấy từ Qdrant
        public List<RagContextItem> ContextUsed { get; set; } = new();
    }

    // ==============================================================
    // DÀNH CHO API 2: NÚT LẤP LÁNH "HỎI GEMINI"
    // ==============================================================

    // Request gửi lên API 2 (Khi user bấm nút)
    public class RagExplainRequestDto
    {
        public string Keyword { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        // Front-end bê nguyên cái mảng ContextUsed (từ API 1) truyền xuống đây cho AI đọc
        public List<RagContextItem> RagContexts { get; set; } = new();
    }

    // Response trả về từ API 2
    public class RagExplainResponseDto
    {
        public string Word { get; set; } = string.Empty;
        public string BestMeaning { get; set; } = string.Empty; // Nghĩa AI chốt
        public string Explanation { get; set; } = string.Empty; // Lời giải thích của AI
    }

    // ==============================================================
    // CẤU TRÚC DÙNG CHUNG
    // ==============================================================
    public class RagContextItem
    {
        public string Label { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string ExampleJp { get; set; } = string.Empty;
        public string ExampleVi { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}