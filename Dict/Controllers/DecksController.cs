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
    // [Authorize] // Bật dòng này khi đã có hệ thống đăng nhập hoàn chỉnh
    public class DecksController : ControllerBase
    {
        private readonly IDeckService _deckService;
        private ResponseDTO _response;

        public DecksController(IDeckService deckService)
        {
            _deckService = deckService;
            _response = new ResponseDTO();
        }

        // Hàm trợ giúp để lấy UserId. Trong thực tế, nó sẽ đọc từ JWT token.
        private int GetUserId() => 1; // Tạm thời hardcode UserId = 1 cho mục đích phát triển

        #region Deck Read Endpoints

        /// <summary>
        /// Lấy tất cả các bộ thẻ công khai.
        /// </summary>
        [HttpGet("public")] // Route: GET /api/decks/public
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
        [HttpGet("my-decks")] // Route: GET /api/decks/my-decks
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
        [HttpGet("{deckId}")] // Route: GET /api/decks/123
        public async Task<IActionResult> GetDeckDetails(int deckId)
        {
            try
            {
                var userId = GetUserId(); // Cần userId để lấy đúng CardState
                // ✨ SỬA: Truyền thêm userId vào service
                var deck = await _deckService.GetDeckDetailsAsync(deckId, userId);
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
        [HttpPost] // Route: POST /api/decks
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

        /// <summary>
        /// Cập nhật thông tin của một bộ thẻ.
        /// </summary>
        [HttpPut("{deckId}")] // Route: PUT /api/decks/123
        public async Task<IActionResult> UpdateDeck(int deckId, [FromBody] DeckUpdateDto deckDto)
        {
            try
            {
                var success = await _deckService.UpdateDeckAsync(deckId, deckDto, GetUserId());
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy bộ thẻ hoặc bạn không có quyền chỉnh sửa.";
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        /// <summary>
        /// Xóa một bộ thẻ.
        /// </summary>
        [HttpDelete("{deckId}")] // Route: DELETE /api/decks/123
        public async Task<IActionResult> DeleteDeck(int deckId)
        {
            try
            {
                var success = await _deckService.DeleteDeckAsync(deckId, GetUserId());
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy bộ thẻ hoặc bạn không có quyền xóa.";
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        /// <summary>
        /// Cập nhật trạng thái public/private của một bộ thẻ.
        /// </summary>
        [HttpPatch("{deckId}/public-status")] // Route: PATCH /api/decks/123/public-status
        public async Task<IActionResult> SetPublicStatus(int deckId, [FromBody] SetPublicStatusDto dto)
        {
            try
            {
                var success = await _deckService.SetDeckPublicStatusAsync(deckId, dto.IsPublic, GetUserId());
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy bộ thẻ hoặc bạn không có quyền chỉnh sửa.";
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
        #endregion

        #region Card CRUD Endpoints

        /// <summary>
        /// Thêm một thẻ mới vào một bộ thẻ.
        /// </summary>
        [HttpPost("{deckId}/cards")] // Route: POST /api/decks/123/cards
        public async Task<IActionResult> AddCard(int deckId, [FromBody] CardCreateDto cardDto)
        {
            try
            {
                _response.Result = await _deckService.AddCardToDeckAsync(deckId, cardDto, GetUserId());
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        /// <summary>
        /// Cập nhật nội dung một thẻ.
        /// </summary>
        [HttpPut("cards/{cardId}")] // Route: PUT /api/decks/cards/456
        public async Task<IActionResult> UpdateCard(int cardId, [FromBody] CardUpdateDto cardDto)
        {
            try
            {
                var success = await _deckService.UpdateCardAsync(cardId, cardDto, GetUserId());
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy thẻ hoặc bạn không có quyền chỉnh sửa.";
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        /// <summary>
        /// Xóa một thẻ khỏi bộ thẻ.
        /// </summary>
        [HttpDelete("cards/{cardId}")] // Route: DELETE /api/decks/cards/456
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            try
            {
                var success = await _deckService.DeleteCardAsync(cardId, GetUserId());
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Không tìm thấy thẻ hoặc bạn không có quyền xóa.";
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
        #endregion
    }

    // ✨ DTO này cần được thêm vào file DTO của bạn
    public class SetPublicStatusDto
    {
        public bool IsPublic { get; set; }
    }
}