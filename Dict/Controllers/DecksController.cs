using Dict.DTO;
using Dict.DTO.Deck;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DecksController : ControllerBase
    {
        private readonly IDeckService _deckService;
        private ResponseDTO _response;

        public DecksController(IDeckService deckService)
        {
            _deckService = deckService;
            _response = new ResponseDTO();
        }

        // SỬA ĐỔI 2: Lấy UserId động từ JWT token
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

        #region Deck Read Endpoints

        // ... (KHÔNG CẦN THAY ĐỔI BẤT KỲ CODE NÀO KHÁC BÊN DƯỚI) ...
        // Tất cả các lệnh gọi tới GetUserId() bây giờ sẽ tự động hoạt động đúng.

        /// <summary>
        /// Lấy tất cả các bộ thẻ công khai.
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous] // ✨ LƯU Ý: Nếu bạn muốn endpoint này không cần đăng nhập, hãy thêm dòng này.
        public async Task<IActionResult> GetPublicDecks()
        {
            try
            {
                _response.Result = await _deckService.GetPublicDecksAsync();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        /// <summary>
        /// ✨ ENDPOINT MỚI: Lấy tất cả các bộ thẻ của người dùng đang đăng nhập.
        /// </summary>
        [HttpGet("my-decks")]
        public async Task<IActionResult> GetUserDecks()
        {
            try
            {
                var userId = GetUserId();
                _response.Result = await _deckService.GetUserDecksAsync(userId);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một bộ thẻ.
        /// </summary>
        [HttpGet("{deckId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDeckDetails(int deckId)
        {
            try
            {
                //var userId = GetUserId();
                var deck = await _deckService.GetDeckDetailsAsync(deckId, 1);
                if (deck == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy bộ thẻ";
                    return NotFound(_response);
                }
                _response.Result = deck;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
        #endregion

        #region Deck CRUD Endpoints

        /// <summary>
        /// Tạo một bộ thẻ mới.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDeck([FromBody] DeckCreateDto deckDto)
        {
            try
            {
                _response.Result = await _deckService.CreateDeckAsync(deckDto, GetUserId());
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
        [HttpGet("user/{username}")] // Route: GET /api/decks/user/some_username
        [AllowAnonymous] // Cho phép truy cập mà không cần token
        public async Task<IActionResult> GetDecksByUser(string username)
        {
            try
            {
                _response.Result = await _deckService.GetPublicDecksByUsernameAsync(username);
                // Nếu không tìm thấy deck nào, response vẫn là 200 OK với một mảng rỗng.
                // Đây là hành vi chuẩn của REST API.
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response); // Trả về lỗi nếu có vấn đề
            }
            return Ok(_response);
        }

        [HttpGet("search")] // Route: GET /api/decks/search?name=N5 Kanji
        [AllowAnonymous]
        public async Task<IActionResult> SearchDecks([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    // Trả về mảng rỗng nếu không có từ khóa
                    _response.Result = Enumerable.Empty<DeckSummaryDto>();
                    return Ok(_response);
                }

                _response.Result = await _deckService.SearchPublicDecksByNameAsync(name);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }
            return Ok(_response);
        }


        // ... (Các endpoint còn lại giữ nguyên) ...

        #endregion
    }

    // DTO này nên được đặt trong thư mục DTOs
}