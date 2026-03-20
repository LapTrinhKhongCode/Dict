using Dict.DTO.Deck;
using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
     [Authorize] // Bật dòng này khi bạn đã có hệ thống đăng nhập
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private ResponseDTO _response;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
            _response = new ResponseDTO();
        }
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                // Dòng này sẽ được kích hoạt nếu token không hợp lệ hoặc không chứa userId,
                // mặc dù [Authorize] thường sẽ chặn các request này trước.
                throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
            }
            return userId;
        }

        [HttpGet]
        [Route("GetQueue/{deckId}")]
        public async Task<IActionResult> GetQueue(int deckId)
        {
            try
            {
                // Tạm thời hardcode UserId. Trong thực tế, bạn sẽ lấy từ JWT token
                // var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userId = GetUserId();

                _response.Result = await _reviewService.GetReviewQueueAsync(deckId, userId);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        [HttpPost]
        [Route("PostAnswer")]
        public async Task<IActionResult> PostAnswer([FromBody] AnswerRequestDto answer)
        {
            try
            {
                var userId = GetUserId(); // Tạm thời hardcode

                var success = await _reviewService.ProcessAnswerAsync(answer, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Failed to process answer";
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
        [HttpPost]
        [Route("Cards/{cardId}/Reset")]
        public async Task<IActionResult> ResetCardProgress(int cardId)
        {
            try
            {
                var userId = GetUserId();
                var success = await _reviewService.ResetCardProgressAsync(cardId, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không thể đặt lại tiến độ thẻ.";
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
