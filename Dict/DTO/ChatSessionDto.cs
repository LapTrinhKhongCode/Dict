namespace Dict.DTO
{
    public class ChatSessionDto
    {
        public int Id { get; set; }
        public string ScopeType { get; set; } = string.Empty;
        public int ScopeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? SourcesJson { get; set; }
        public string? CitationsJson { get; set; }
        public bool CacheHit { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChatSessionDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ScopeType { get; set; } = string.Empty;
        public int ScopeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class CreateChatSessionRequestDto
    {
        public string ScopeType { get; set; } = string.Empty;
        public int ScopeId { get; set; }
        public string Title { get; set; } = "Hội thoại mới";
    }

    public class SaveChatTurnRequestDto
    {
        public string UserMessage { get; set; } = string.Empty;
        public string AssistantMessage { get; set; } = string.Empty;
        public string? SourcesJson { get; set; }
        public string? CitationsJson { get; set; }
        public bool CacheHit { get; set; } = false;
    }

    public class UpdateSessionTitleDto
    {
        public string Title { get; set; } = string.Empty;
    }
}
