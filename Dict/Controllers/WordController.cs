using Dict.Data;
using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WordController : ControllerBase
    {
        private ResponseDTO _response;
        private readonly IWordService _wordService;
        private readonly IJsonBuilderService _jsonBuilderService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WordController> _logger;
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _db;

        public WordController(IWordService kanjiService, IJsonBuilderService jsonBuilderService, IServiceProvider serviceProvider, ILogger<WordController> logger, IAdminService adminService, ApplicationDbContext db)
        {
            _response = new ResponseDTO();
            _wordService = kanjiService;
            _jsonBuilderService = jsonBuilderService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _adminService = adminService;
            _db = db;
        }

        [HttpGet]
        [Route("GetWordJson/{label}")]
        public async Task<IActionResult> GetWordJson(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || label.Length > 50)
            {
                return BadRequest("Từ khóa không hợp lệ (quá dài hoặc rỗng).");
            }
            // 1. Thử lấy từ cache (RawJson)
            var json = await _wordService.GetWordJson(label);
            bool isRebuilt = false;

            // 2. Nếu không có -> Build lại
            if (string.IsNullOrEmpty(json))
            {
                int missCount = await _wordService.GetSearchMissCountAsync(label);

                if (missCount >= 5)
                {
                    _logger.LogInformation("Từ '{Label}' đã bị miss {Count} lần. Tiến hành Build JSON...", label, missCount);
                    json = await _jsonBuilderService.RebuildJsonForWordAsync(label);
                    isRebuilt = true;
                }
                else
                {
                    _ = Task.Run(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IWordService>();
                        await svc.IncrementSearchMissAsync(label);
                    });
                    return NotFound();
                }
            }

            if (string.IsNullOrEmpty(json))
                return NotFound();

            if (isRebuilt)
            {
                _ = Task.Run(async () =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedWordService = scope.ServiceProvider.GetRequiredService<IWordService>();
                        await scopedWordService.UpsertCacheForLabelAsync(label, json, "Homophone_Build");
                    }
                });
            }

            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }

        /// <summary>
        /// Tìm từ Nhật theo nghĩa tiếng Việt (cột ShortMean trong bảng entries).
        /// Trả về cùng format với GetWordJson — kết quả nằm trong data.suggestWords.
        /// </summary>
        [HttpGet]
        [Route("SearchByViMeaning/{term}")]
        public async Task<IActionResult> SearchByViMeaning(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length > 100)
                return BadRequest("Từ khóa không hợp lệ.");

            var clean = term.Trim();

            var matches = await _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "word"
                         && e.RawJson != null
                         && e.ShortMean != null
                         && EF.Functions.Like(
                             EF.Functions.Collate(e.ShortMean, "Vietnamese_CI_AI"),
                             $"%{clean}%"))
                .OrderBy(e => EF.Functions.Collate(e.ShortMean, "Vietnamese_CI_AI") == clean ? 0
                            : EF.Functions.Like(EF.Functions.Collate(e.ShortMean, "Vietnamese_CI_AI"), clean + "%") ? 1
                            : 2)
                .ThenBy(e => e.Weight)
                .Take(20)
                .Select(e => new { e.Label, e.Phonetic, e.ShortMean })
                .ToListAsync();

            if (!matches.Any())
                return NotFound();

            // Build response cùng format GetWordJson — suggestWords cho FE dùng chung
            var suggestWords = matches.Select(e => new
            {
                word    = e.Label,
                phonetic = e.Phonetic ?? "",
                short_mean = e.ShortMean ?? "",
                means   = Array.Empty<object>()
            });

            var response = new
            {
                status = 200,
                data   = new
                {
                    words        = Array.Empty<object>(),
                    suggestWords = suggestWords
                }
            };

            return Ok(response);
        }
    }
}
