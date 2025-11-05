using System;
// Đã loại bỏ using System.ComponentModel.DataAnnotations...

namespace Dict.Models
{
    /// <summary>
    /// Model lưu trữ các từ khóa người dùng tìm kiếm nhưng không ra kết quả (Search Misses).
    /// Model này "sạch" (POCO) và không chứa các Data Annotations.
    /// </summary>
    public class SearchMiss
    {
        public int Id { get; set; }

        /// <summary>
        /// Từ khóa gốc mà người dùng đã gõ.
        /// </summary>
        public string SearchTerm { get; set; }

        /// <summary>
        /// Từ khóa đã được chuẩn hóa (ví dụ: viết thường, bỏ dấu) để nhóm các từ giống nhau.
        /// </summary>
        public string NormalizedTerm { get; set; }

        /// <summary>
        /// Tổng số lần từ khóa này được tìm kiếm (mà không thấy).
        /// </summary>
        public int SearchCount { get; set; } = 1;

        /// <summary>
        /// Lần cuối cùng từ khóa này được tìm.
        /// </summary>
        public DateTime LastSearchedAt { get; set; }
    }
}