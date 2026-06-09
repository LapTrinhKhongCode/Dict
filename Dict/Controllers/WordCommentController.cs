using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [Route("api/word-comments")]
    [ApiController]
    [Authorize]
    public class WordCommentController : ControllerBase
    {
        private readonly IWordCommentService _service;
        private ResponseDTO _response;

        public WordCommentController(IWordCommentService service)
        {
            _service = service;
            _response = new ResponseDTO();
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst("userId")?.Value ?? "0");

        // GET /api/word-comments?word=食べる
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromQuery] string word)
        {
            try
            {
                _response.Result = await _service.GetByWordAsync(word);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        // POST /api/word-comments
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateWordCommentDTO dto)
        {
            try
            {
                _response.Result = await _service.AddAsync(GetUserId(), dto);
                _response.Message = "Đã thêm bình luận.";
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        // DELETE /api/word-comments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteAsync(GetUserId(), id);
                if (!ok)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không thể xóa bình luận này.";
                }
                else
                {
                    _response.Message = "Đã xóa bình luận.";
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
    }
}
