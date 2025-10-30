using Docker.DotNet.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ApplicationUser : IdentityUser<int>
    {

        /// <summary>
        /// Trường tùy chỉnh để khóa/mở tài khoản bằng tay (Admin ban).
        /// Identity có LockoutEnd, nhưng IsActive rõ ràng hơn cho mục đích này.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Đường dẫn avatar tùy chỉnh.
        /// </summary>
        public string AvatarUrl { get; set; }

        /// <summary>
        /// Ngày tạo tài khoản.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Ngày cập nhật thông tin.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Deck> Decks { get; set; }
        public virtual ICollection<CardState> CardStates { get; set; }
        public virtual ICollection<ReviewLog> ReviewLogs { get; set; }
        public virtual ICollection<MediaStore> MediaStore { get; set; }
        public virtual ICollection<OcrJob> OcrJobs { get; set; }

        public ApplicationUser()
        {
            Decks = new HashSet<Deck>();
            CardStates = new HashSet<CardState>();
            ReviewLogs = new HashSet<ReviewLog>();
            MediaStore = new HashSet<MediaStore>();
            OcrJobs = new HashSet<OcrJob>();
        }
    }
}
