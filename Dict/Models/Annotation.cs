using System.ComponentModel.DataAnnotations;

namespace Dict.Models
{
    public class Annotation
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        public int? OcrJobId { get; set; }
        public virtual OcrJob OcrJob { get; set; }

        public int PageNumber { get; set; } = 1;

        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // JSON array of stroke paths: [{tool, color, width, points:[{x,y},...]}]
        [Required]
        public string Data { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
