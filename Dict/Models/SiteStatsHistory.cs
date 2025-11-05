using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    /// <summary>
    /// Model lưu "ảnh chụp" (snapshot) các chỉ số quan trọng của hệ thống theo ngày.
    /// Giúp Admin xem biểu đồ tăng trưởng theo thời gian.
    /// </summary>
    public class SiteStatsHistory
    {
        /// <summary>
        /// Khóa chính là ngày chụp (chỉ 1 bản ghi mỗi ngày).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Tổng số người dùng tích lũy tại ngày hôm đó.
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Số người dùng mới đăng ký trong ngày hôm đó.
        /// </summary>
        public int NewUsersToday { get; set; }

        /// <summary>
        /// Tổng số mục từ (entries) trong từ điển.
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Tổng số lượt ôn tập (review) flashcard trong ngày.
        /// </summary>
        public int TotalReviewsToday { get; set; }

        /// <summary>
        /// Tổng số lỗi API (status >= 400) ghi nhận trong ngày.
        /// </summary>
        public int TotalApiErrorsToday { get; set; }

    }
}