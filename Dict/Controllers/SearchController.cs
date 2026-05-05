using Dict.Data;
using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IJsonBuilderService _jsonBuilder;
        private readonly IRagSearchService _ragSearchService;

        public SearchController(ISearchService searchService, IJsonBuilderService jsonBuilder, IRagSearchService ragSearchService)
        {
            _searchService = searchService;
            _jsonBuilder = jsonBuilder;
            _ragSearchService = ragSearchService;
        }

        // =====================================================================
        // API 1: TÌM KIẾM VECTOR (SIÊU TỐC)
        // Dùng để hiển thị kết quả ngay lập tức trên UI
        // =====================================================================
        [HttpPost("rag/search")]
        public async Task<IActionResult> RagSemanticSearch([FromBody] RagSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Keyword) || string.IsNullOrWhiteSpace(request.Context))
            {
                return BadRequest(new { Message = "Yêu cầu cung cấp đầy đủ 'Keyword' và 'Context'." });
            }

            try
            {
                var result = await _ragSearchService.SearchVectorAsync(request.Keyword, request.Context);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RAG VECTOR ERROR] {ex.Message}");
                return StatusCode(500, new { Message = "Lỗi xử lý Vector Database.", Error = ex.Message });
            }
        }

        // =====================================================================
        // API 2: NÚT "LẤP LÁNH" HỎI GEMINI 
        // Chỉ gọi khi user click. Truyền lại danh sách RagContexts vừa tìm được
        // =====================================================================
        [HttpPost("rag/explain")]
        public async Task<IActionResult> RagExplainWithAi([FromBody] RagExplainRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Keyword) || string.IsNullOrWhiteSpace(request.Context))
            {
                return BadRequest(new { Message = "Thiếu Keyword hoặc Context." });
            }

            if (request.RagContexts == null || request.RagContexts.Count == 0)
            {
                return BadRequest(new { Message = "Không có ngữ cảnh RAG nào được truyền lên để AI phân tích." });
            }

            try
            {
                var (word, best_meaning, explanation) = await _ragSearchService.ExplainWithGeminiAsync(request.Keyword, request.Context, request.RagContexts);
                
                // Trả về DTO chuẩn thay vì object vô danh
                var response = new RagExplainResponseDto 
                {
                    Word = word,
                    BestMeaning = best_meaning,
                    Explanation = explanation
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GEMINI ERROR] {ex.Message}");
                return StatusCode(500, new { Message = "Lỗi khi gọi API AI.", Error = ex.Message });
            }
        }

        // =====================================================================
        // AUTOCOMPLETE (GIỮ NGUYÊN)
        // =====================================================================
        [HttpGet("autocomplete/{term}")]
        public async Task<ActionResult<List<AutocompleteSuggestionDto>>> GetAutocompleteSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length > 50)
            {
                return BadRequest("Input invalid");
            }
            if (ContainsTrash(term))
            {
                return Ok(new List<AutocompleteSuggestionDto>());
            }
            var suggestions = await _searchService.GetAutocompleteSuggestionsAsync(term);
            return Ok(suggestions);
        }

        private bool ContainsTrash(string input)
        {
            return input.Contains("Executed DbCommand") || input.Contains("[Parameters=");
        }
    }
}