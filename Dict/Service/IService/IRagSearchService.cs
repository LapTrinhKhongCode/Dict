using Dict.DTO;

namespace Dict.Service.IService
{
    public interface IRagSearchService
    {
        Task<RagSearchResponseDto> SearchVectorAsync(string keyword, string context);
        Task<(string word, string best_meaning, string explanation)> ExplainWithGeminiAsync(string keyword, string context, List<RagContextItem> ragContexts);
    }
}
