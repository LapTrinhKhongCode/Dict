using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    [Table("chat_sessions")]
    public class ChatSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>"file" | "project" | "workspace"</summary>
        [MaxLength(20)]
        public string ScopeType { get; set; } = string.Empty;

        public int ScopeId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = "Hội thoại mới";
        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    [Table("chat_messages")]
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ChatSessionId { get; set; }
        public virtual ChatSession Session { get; set; } = null!;

        /// <summary>"user" | "assistant"</summary>
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        /// <summary>JSON array of sources (nullable for user messages)</summary>
        public string? SourcesJson { get; set; }

        /// <summary>JSON array of citations (nullable for user messages)</summary>
        public string? CitationsJson { get; set; }

        /// <summary>True if answer was served from semantic cache</summary>
        public bool CacheHit { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
