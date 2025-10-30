using Dict.DTO;
using Dict.Service;
using Dict.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Text.Json;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuildJsonController : ControllerBase
    {
        private ResponseDTO _response;
        private readonly IJsonBuilderService _jsonBuilderService;
        public BuildJsonController(IJsonBuilderService jsonBuilderService)
        {
            _response = new ResponseDTO();
            _jsonBuilderService = jsonBuilderService;
        }
        [HttpGet]
        [Route("BuildKanjiJson/{label}")]
        public async Task<IActionResult> BuildKanjiJson(string label)
        {
            var json = await _jsonBuilderService.RebuildJsonForKanjiAsync(label);

            //if (string.IsNullOrEmpty(json))
            //    return NotFound();

            //var doc = JsonDocument.Parse(json);
            // return Ok(doc.RootElement);
            return Ok(json);
        }

        [HttpGet]
        [Route("BuildWordJson/{label}")]
        public async Task<IActionResult> BuildWordJson(string label)
        {
            var json = await _jsonBuilderService.RebuildJsonForWordAsync(label);

            //if (string.IsNullOrEmpty(json))
            //    return NotFound();

            //var doc = JsonDocument.Parse(json);
            // return Ok(doc.RootElement);
            return Ok(json);
        }

    }
}
