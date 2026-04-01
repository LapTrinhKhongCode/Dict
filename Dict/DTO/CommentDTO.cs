namespace Dict.DTO
{
    // DTO nhận dữ liệu comment từ Frontend
    public class CreateCommentDTO
    {
        public int MediaStoreId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; }
        public int? PageNumber { get; set; }
        public string? AnnotationData { get; set; }
    }

    // DTO trả về danh sách comment (Bao gồm cả người gửi và các reply)
    public class FileCommentDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; }
        public int? PageNumber { get; set; }
        public string? AnnotationData { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }

        // Danh sách các câu trả lời con
        public List<FileCommentDTO> Replies { get; set; } = new List<FileCommentDTO>();
    }
}