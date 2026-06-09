namespace Dict.DTO
{
    public class WordCommentDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateWordCommentDTO
    {
        public string WordLabel { get; set; } = null!;
        public string Content { get; set; } = null!;
    }
}
