namespace Dict.DTO
{
    public class OcrJobCreateDto
    {
        public int? UserId { get; set; }
        public int? MediaId { get; set; }
        public string Status { get; set; }
        public string DetectedText { get; set; }
    }
}
