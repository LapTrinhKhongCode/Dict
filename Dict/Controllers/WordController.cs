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
        public WordController(IWordService kanjiService)
        {
            _response = new ResponseDTO();
            _wordService = kanjiService;
        }

        [HttpGet]
        [Route("GetWordJson/{label}")]
        public async Task<IActionResult> GetWordJson(string label)
        {
            var json = await _wordService.GetWordJson(label);

            if (string.IsNullOrEmpty(json))
                return NotFound();

            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }
    }
}
