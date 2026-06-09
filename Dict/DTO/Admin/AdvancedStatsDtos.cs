using System.Collections.Generic;

namespace Dict.DTO.Admin
{
    // === 1. THỐNG KÊ TÌM KIẾM (Search Statistics) ===

    /// <summary>
    /// Item chi tiết cho từ được tra cứu nhiều nhất hoặc bị tìm hụt
    /// </summary>
    public class TopSearchItemDto
    {
        public string Term { get; set; }
        public int Count { get; set; }
        public int? WordId { get; set; }
        public string? ActualWord { get; set; } // WordText nếu thành công, null nếu thất bại
    }

    // === 2. THỐNG KÊ HIỆU SUẤT (Performance) ===

    /// <summary>
    /// Item chi tiết cho các API bị lỗi hoặc chạy chậm
    /// </summary>
    public class ApiStatItemDto
    {
        public string Endpoint { get; set; }
        public int TotalCalls { get; set; }
        public int ErrorCount { get; set; }
        public double ErrorRate { get; set; } // Tỷ lệ lỗi (0.0 - 1.0)
        public int AverageResponseTimeMs { get; set; }
    }

    /// <summary>
    /// Item chi tiết cho các Job hệ thống bị lỗi
    /// </summary>
    public class FailedJobItemDto
    {
        public int Id { get; set; }
        public string JobType { get; set; } // Import, OCR
        public string Status { get; set; }
        public string? ErrorMessage { get; set; } // Trích xuất từ Meta/JsonErrorMessage
        public DateTime FailedAt { get; set; }
    }
}