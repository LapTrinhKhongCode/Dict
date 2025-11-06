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
        public WordController(IWordService kanjiService, IJsonBuilderService jsonBuilderService, IServiceProvider serviceProvider)
        {
            _response = new ResponseDTO();
            _wordService = kanjiService;
            _jsonBuilderService = jsonBuilderService;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        [Route("GetWordJson/{label}")]
        public async Task<IActionResult> GetWordJson(string label)
        {
            // 1. Thử lấy từ cache (RawJson)
            var json = await _wordService.GetWordJson(label);
            bool isRebuilt = false;

            // 2. Nếu không có -> Build lại
            if (string.IsNullOrEmpty(json))
            {
                json = await _jsonBuilderService.RebuildJsonForWordAsync(label);
                isRebuilt = true; // Đánh dấu là vừa build
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
