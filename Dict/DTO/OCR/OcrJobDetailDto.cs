namespace Dict.DTO.OCR
{
    public class OcrJobDetailDto
    {
        public int Id { get; set; }
        public string Status { get; set; }

        // URL của ảnh gốc
        public string? ImageUrl { get; set; }

        // Kết quả dạng văn bản đầy đủ
        public string? DetectedText { get; set; }

        // Thời gian tạo
        public DateTime? CreatedAt { get; set; }

        // Danh sách các từ/bounding box
        public List<OcrResultDto> Results { get; set; }
    }
}
