using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.OCR
{
    public class CreateOcrResultDto
    {
        public int PageNumber { get; set; }

        [Required]
        public string WordText { get; set; }

        [Required]
        public string BoundingBox { get; set; } // JSON string
    }
}
