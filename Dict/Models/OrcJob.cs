using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class OcrJob
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public int? MediaId { get; set; }
        public virtual MediaStore Media { get; set; }

        public string Status { get; set; }

        public string DetectedText { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<OcrResult> Results { get; set; }
    }
}
