using Microsoft.AspNetCore.Http.HttpResults;

namespace Dict.DTO.OCR
{
    public class OcrJobCreateDto
    {
        public int? UserId { get; set; }
        public int? MediaId { get; set; }
        public int? ProjectId { get; set; }
        public string Status { get; set; }
        public string DetectedText { get; set; }
    }
}
