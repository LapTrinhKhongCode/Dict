using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class OcrResult
    {
        public int Id { get; set; }

        public int? OcrJobId { get; set; }
        public virtual OcrJob OcrJob { get; set; }

        public int? PageNumber { get; set; }

        public string WordText { get; set; }

        public string BoundingBox { get; set; } // json
        public float? Confidence { get; set; }

        public int? LinkWordId { get; set; }
        [ForeignKey(nameof(LinkWordId))]
        public virtual Word LinkWord { get; set; }
    }
}
