using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class MediaStore
    {
        public int Id { get; set; }

        public int? OwnerId { get; set; }
        public virtual ApplicationUser Owner { get; set; }

        public string FileName { get; set; }

        public string MimeType { get; set; }

        public long? SizeBytes { get; set; }

        public string StorageUrl { get; set; }

        public string Sha256 { get; set; }

        public DateTime? CreatedAt { get; set; }
        public ICollection<OcrJob> OcrJobs { get; set; }
    }
}
