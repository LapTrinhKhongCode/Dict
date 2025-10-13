using Dict.DTO;
using Dict.Service;
using Dict.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private ResponseDTO _response;
        private readonly ICommentService _commentService;
        public CommentController(ICommentService commentService)
        {
            _response = new ResponseDTO();
            _commentService = commentService;
        }

        [HttpGet]
        [Route("GetCommentJson/{label}")]
        public async Task<IActionResult> GetCommentJson(string label)
        {
            var json = await _commentService.GetCommentJson(label);

            if (string.IsNullOrEmpty(json))
                return NotFound();

            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }
    }
}
