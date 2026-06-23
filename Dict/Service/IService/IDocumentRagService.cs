using Dict.DTO;

namespace Dict.Service.IService
{
    public interface IDocumentRagService
    {
        Task<DocumentRagIndexResponseDto> IndexDocumentAsync(int jobId, int userId);
        Task<DocumentRagBulkIndexResponseDto> IndexAllInScopeAsync(int scopeId, string scopeType, int userId);
        Task DeleteJobArtifactsAsync(int jobId);
        Task<DocumentRagAskResponseDto> AskDocumentAsync(int jobId, int userId, string question, int topK = 5, List<DocumentRagTurnDto>? history = null, string? sessionId = null);
        IAsyncEnumerable<RagStreamEvent> AskDocumentStreamAsync(int jobId, int userId, string question, int topK = 5, List<DocumentRagTurnDto>? history = null, string? sessionId = null, string mode = "high");
        IAsyncEnumerable<RagStreamEvent> AskWorkspaceStreamAsync(int workspaceId, int userId, string question, int topK = 5, List<DocumentRagTurnDto>? history = null, string? sessionId = null, string mode = "high");
        IAsyncEnumerable<RagStreamEvent> AskProjectStreamAsync(int projectId, int userId, string question, int topK = 5, List<DocumentRagTurnDto>? history = null, string? sessionId = null, string mode = "high");
    }
}
