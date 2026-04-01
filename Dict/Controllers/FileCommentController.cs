using Microsoft.AspNetCore.Mvc;
using Dict.DTO;
using Microsoft.AspNetCore.Authorization;
using Dict.Service.IService;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FileCommentController : ControllerBase
    {
        private readonly IFileCommentService _commentService;
        private ResponseDTO _response;

        public FileCommentController(IFileCommentService commentService)
        {
            _commentService = commentService;
            _response = new ResponseDTO();
        }

        // Viết bình luận
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDTO dto)
        {
            try
            {
                int currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                _response.Result = await _commentService.AddCommentAsync(currentUserId, dto);
                _response.Message = "Đã thêm bình luận.";
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        // Lấy toàn bộ bình luận của 1 file
        [HttpGet("file/{mediaStoreId}")]
        public async Task<IActionResult> GetComments(int mediaStoreId)
        {
            try
            {
                _response.Result = await _commentService.GetCommentsByFileAsync(mediaStoreId);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        // Xóa (mềm) bình luận
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                int currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var success = await _commentService.DeleteCommentAsync(currentUserId, id);
                if (success)
                    _response.Message = "Bình luận đã được xóa.";
                else
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không thể xóa bình luận này.";
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