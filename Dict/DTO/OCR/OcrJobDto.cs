namespace Dict.DTO.OCR
{
    public class OcrJobDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? MediaId { get; set; }
        public string Status { get; set; }
        public string DetectedText { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
