namespace Dict.DTO.OCR
{
    public class OcrJobUpdateStatusDto
    {
        public string Status { get; set; }
        public string? DetectedText { get; set; } // Dùng string? vì code của bạn truyền giá trị null
    }
}
