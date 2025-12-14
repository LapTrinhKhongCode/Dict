using Dict.DTO.Deck;
using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Dict.Data;
using Dict.Models;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
     [Authorize] // Bật dòng này khi bạn đã có hệ thống đăng nhập
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private ResponseDTO _response;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        public ReviewController(IReviewService reviewService, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _reviewService = reviewService;
            _response = new ResponseDTO();
            _db = db;
            _userManager = userManager;
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
             

                var userId = GetUserId();
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return Unauthorized(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Người dùng không tồn tại."
                    });
                }

                if (!user.IsActive)
                {
                    return Unauthorized(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Tài khoản bị khóa."
                    });
                }


                // Kiểm tra deck tồn tại
                var deck = await _db.Decks.FindAsync(deckId);
                if (deck == null)
                {
                    return StatusCode(404, new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Deck không tồn tại"
                    });
                }

                // Kiểm tra quyền sở hữu deck
                if (deck.UserId != userId)
                {
                    return StatusCode(403, new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Không có quyền truy cập deck này"
                    });
                }


                var queue = await _reviewService.GetReviewQueueAsync(deckId, userId);

                return Ok(new ResponseDTO
                {
                    IsSuccess = true,
                    Result = queue,
                    Message = "Lấy queue thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Lỗi hệ thống: " + ex.Message
                });
            }
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
