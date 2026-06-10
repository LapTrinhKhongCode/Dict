using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public WordController(IWordService kanjiService, IJsonBuilderService jsonBuilderService, IServiceProvider serviceProvider, ILogger<WordController> logger, IAdminService adminService)
        {
            _response = new ResponseDTO();
            _wordService = kanjiService;
            _jsonBuilderService = jsonBuilderService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _adminService = adminService;
        }

        [HttpGet]
        [Route("GetWordJson/{label}")]
        public async Task<IActionResult> GetWordJson(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || label.Length > 50)
            {
                //_logger.LogWarning("Phát hiện request quá dài hoặc rỗng: {Label}", label?.Substring(0, Math.Min(label?.Length ?? 0, 20)));
                return BadRequest("Từ khóa không hợp lệ (quá dài hoặc rỗng).");
            }
            // 1. Thử lấy từ cache (RawJson)
            var json = await _wordService.GetWordJson(label);
            bool isRebuilt = false;

            // 2. Nếu không có -> Build lại
            if (string.IsNullOrEmpty(json))
            {
                int missCount = await _wordService.GetSearchMissCountAsync(label);

                if (missCount >= 5) // Ngưỡng Threshold: Chỉ build nếu bị miss từ 5 lần trở lên
                {
                    _logger.LogInformation("Từ '{Label}' đã bị miss {Count} lần. Tiến hành Build JSON...", label, missCount);
                    json = await _jsonBuilderService.RebuildJsonForWordAsync(label);
                    isRebuilt = true;
                }
                else
                {
                    // Chưa đủ 5 lần — ghi nhận thêm 1 miss rồi trả về NotFound ngay
                    _ = Task.Run(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IWordService>();
                        await svc.IncrementSearchMissAsync(label);
                    });
                    return NotFound();
                }
            }

            // 3. Kiểm tra lần cuối (nếu build lại vẫn rỗng)
            if (string.IsNullOrEmpty(json))
            {
                return NotFound();
            }

            if (isRebuilt)
            {
                string categoryMarker = "Homophone_Build";

                // Chạy tác vụ ngầm trong một Scope mới
                _ = Task.Run(async () =>
                {
                    // Tạo một scope service mới
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // Lấy một WordService MỚI (nó sẽ có DbContext MỚI)
                        var scopedWordService = scope.ServiceProvider.GetRequiredService<IWordService>();

                        // Dùng service mới này để chạy ngầm
                        await scopedWordService.UpsertCacheForLabelAsync(label, json, categoryMarker);
                    }
                });
            }

            // 5. Parse và trả về
            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }
    }
}
