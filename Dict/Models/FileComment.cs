using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class FileComment
    {
        [Key]
        public int Id { get; set; }

        public int MediaStoreId { get; set; }
        public int UserId { get; set; }

        public int? ParentCommentId { get; set; } // Liên kết ID của comment cha (nếu đây là câu trả lời)

        [Required]
        public string Content { get; set; }

        // --- Dữ liệu vị trí trên PDF (Dành cho tính năng bôi đen/ghim chú thích) ---
        public int? PageNumber { get; set; }
        public string? AnnotationData { get; set; } // Lưu chuỗi JSON tọa độ (VD: {x:10, y:20, w:100, h:50})

        // Đánh dấu xóa mềm (Giữ nguyên cấu trúc cây thảo luận trên UI)
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // --- Navigation Properties ---
        [ForeignKey(nameof(MediaStoreId))]
        public virtual MediaStore MediaStore { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey(nameof(ParentCommentId))]
        public virtual FileComment? ParentComment { get; set; }
        public virtual ICollection<FileComment> Replies { get; set; }
    }
}