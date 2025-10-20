namespace Dict.DTO.Ocr
{
    // DTO này đại diện cho một kết quả nhận dạng duy nhất (một từ)
    public class OcrResultDto
    {
        public int Id { get; set; }
        public string WordText { get; set; }
        public string BoundingBox { get; set; } // json
        public float? Confidence { get; set; }
    }
}
