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

        public SearchController(ISearchService searchService, IJsonBuilderService jsonBuilder)
        {
            _searchService = searchService;
            _jsonBuilder = jsonBuilder;
        }

        //[HttpGet("{term}")]
        //public async Task<IActionResult> Search(string term)
        //{
        //    List<Dict.Models.Entry> mainEntries = new List<Dict.Models.Entry>();
        //    List<Dict.Models.Entry> suggestionEntries;

        //    // --- Áp dụng Thuật toán Tìm kiếm Hợp nhất ---
        //    var exactLabelMatch = await _searchService.FindExactLabelMatchAsync(term);

        //    if (exactLabelMatch != null)
        //    {
        //        mainEntries.Add(exactLabelMatch);
        //        suggestionEntries = await _searchService.GetSuggestionEntriesAsync(term, 20, new List<int> { exactLabelMatch.Id });
        //    }
        //    else
        //    {
        //        var homophoneMatches = await _searchService.FindHomophonesAsync(term);
        //        if (homophoneMatches.Any())
        //        {
        //            mainEntries = homophoneMatches;
        //            var homophoneIds = homophoneMatches.Select(e => e.Id).ToList();
        //            suggestionEntries = await _searchService.GetSuggestionEntriesAsync(term, 20, homophoneIds);
        //        }
        //        else
        //        {
        //            mainEntries = new List<Dict.Models.Entry>(); // Rỗng
        //            suggestionEntries = await _searchService.GetSuggestionEntriesAsync(term, 20);
        //        }
        //    }

        //    // --- Gọi JsonBuilder để tạo JSON cuối cùng ---
        //    var jsonResult = _jsonBuilder.RebuildJsonForWordAsync(mainEntries, suggestionEntries);

        //    return Content(jsonResult, "application/json");
        //}


        [HttpGet("autocomplete/{term}")]
        public async Task<ActionResult<List<AutocompleteSuggestionDto>>> GetAutocompleteSuggestions(string term)
        {
            var suggestions = await _searchService.GetAutocompleteSuggestionsAsync(term);
            return Ok(suggestions);
        }
    }
}
