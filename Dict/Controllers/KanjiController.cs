using Dict.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KanjiController : ControllerBase
    {
        private ResponseDTO _response;
        private readonly IKanjiService _kanjiService;
        public KanjiController(IKanjiService kanjiService)
        {
            _response = new ResponseDTO();
            _kanjiService = kanjiService;
        }

        [HttpGet]
        [Route("GetKanji/{character}")]
        public IActionResult GetKanji(string character)
        {
            try
            {
                _response.Result = _kanjiService.GetKanjiInfoAsync(character).Result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        [HttpGet]
        [Route("GetKanjiJson/{label}")]
        public async Task<IActionResult> GetKanjiJson(string label)
        {
            var json = await _kanjiService.GetKanjiJson(label);

            if (string.IsNullOrEmpty(json))
                return NotFound();

            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }


    }
}