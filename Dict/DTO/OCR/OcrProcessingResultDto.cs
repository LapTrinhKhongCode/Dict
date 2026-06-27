namespace Dict.DTO.OCR
{
    public class OcrProcessingResultDto
    {
        public int JobId { get; set; }
        public string Status { get; set; }
        public string DetectedText { get; set; }
        public int MediaId { get; set; }
        public string ImageUrl { get; set; }
        public int? AnnotatedMediaId { get; set; }
        public string? AnnotatedImageUrl { get; set; }
        public List<CreateOcrResultDto> Results { get; set; } = new List<CreateOcrResultDto>();

        /// <summary>
        /// True khi PDF không có text native (scan/ảnh) → FE nên gọi upload-document-ai.
        /// False hoặc null khi text đã đủ, không cần Azure.
        /// </summary>
        public bool? NeedsAzureDocumentAi { get; set; }
    }   
}
