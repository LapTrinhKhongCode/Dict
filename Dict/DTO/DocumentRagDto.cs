namespace Dict.DTO
{
    public class DocumentRagIndexResponseDto
    {
        public int JobId { get; set; }
        public string Collection { get; set; } = string.Empty;
        public int PagesIndexed { get; set; }
        public int ChunksIndexed { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DocumentRagAskRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public int TopK { get; set; } = 5;
        public List<DocumentRagTurnDto> ConversationHistory { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
        /// <summary>"fast" | "balance" | "high" (default)</summary>
        public string Mode { get; set; } = "high";
        /// <summary>If true, skip clarification prompts (useful for automated evaluation).</summary>
        public bool SkipClarify { get; set; } = false;
    }

    public class DocumentRagTurnDto
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DocumentRagAskResponseDto
    {
        public int JobId { get; set; }
        public string Collection { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string AttributedAnswer { get; set; } = string.Empty;
        public List<DocumentRagSourceDto> Sources { get; set; } = new();
        public List<DocumentRagCitationDto> Citations { get; set; } = new();
    }

    public class DocumentRagSourceDto
    {
        public int SourceId { get; set; }
        public int JobId { get; set; }
        public int ProjectId { get; set; }
        public int PageNumber { get; set; }
        public int ChunkIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text";
        public double Score { get; set; }
        public string DocumentName { get; set; } = string.Empty;
    }

    public class DocumentRagCitationDto
    {
        public int SourceId { get; set; }
        public int PageNumber { get; set; }
        public int ChunkIndex { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class RagStreamEvent
    {
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public class DocumentRagBulkIndexResponseDto
    {
        public int TotalJobs { get; set; }
        public int IndexedJobs { get; set; }
        public int SkippedJobs { get; set; }
        public int TotalChunks { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
