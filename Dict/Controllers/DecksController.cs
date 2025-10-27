using Dict.DTO;
using Dict.DTO.Deck; // Namespace chứa các DTO trên
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Cần cho ClaimTypes

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bảo vệ toàn bộ controller
    public class DecksController : ControllerBase
    {
        private readonly IDeckService _deckService;
        private readonly ResponseDTO _response;
        private readonly ILogger<DecksController> _logger; // Thêm Logger

        public DecksController(IDeckService deckService, ILogger<DecksController> logger) // Inject Logger
        {
            _deckService = deckService;
            _response = new ResponseDTO();
            _logger = logger; // Lưu Logger
        }

        // SỬA LẠI: Lấy UserId động từ JWT token bằng ClaimTypes.NameIdentifier
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

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicDecks()
        {
            try
            {
                _response.Result = await _deckService.GetPublicDecksAsync();
                _response.Message = "Successfully retrieved public decks.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public decks");
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        [HttpGet("my-decks")]
        public async Task<IActionResult> GetMyDecks() // Sửa tên hàm cho rõ nghĩa
        {
            try
            {
                var userId = GetUserId();
                _response.Result = await _deckService.GetUserDecksAsync(userId);
                _response.Message = "Successfully retrieved user's decks.";
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting decks for user {UserId}", GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        // SỬA LẠI: GetDeckDetails dùng UserId từ token
        [HttpGet("{deckId}")]
        [AllowAnonymous] // Cho phép xem chi tiết deck public mà không cần đăng nhập
        public async Task<IActionResult> GetDeckDetails(int deckId)
        {
            try
            {
                //int userId = GetUserId();
                              

                var deck = await _deckService.GetDeckDetailsAsync(deckId, 1);
                if (deck == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Deck not found.";
                    return NotFound(_response);
                }
                // TODO: Thêm kiểm tra nếu deck là private và người dùng không phải chủ sở hữu
                _response.Result = deck;
                _response.Message = "Successfully retrieved deck details.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting details for deck {DeckId}", deckId);
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        [HttpGet("user/{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicDecksByUsername(string username) // Đổi tên tham số + hàm
        {
            try
            {
                _response.Result = await _deckService.GetPublicDecksByUsernameAsync(username);
                _response.Message = $"Successfully retrieved public decks for user '{username}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public decks for username {Username}", username);
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPublicDecks([FromQuery] string name) // Đổi tên tham số
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    _response.Result = Enumerable.Empty<UserDeckSummary>();
                    _response.Message = "Search query cannot be empty.";
                    return Ok(_response); // Trả về mảng rỗng thay vì lỗi
                }

                _response.Result = await _deckService.SearchPublicDecksByNameAsync(name);
                _response.Message = $"Successfully searched public decks for '{name}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching public decks with query '{Query}'", name);
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        #endregion

        #region Deck Write Endpoints

            [HttpPost]
            public async Task<IActionResult> CreateDeck([FromBody] DeckCreateDto deckDto)
            {
                if (!ModelState.IsValid) { return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data", Result = ModelState }); }
                try
                {
                    var userId = GetUserId();
                    _response.Result = await _deckService.CreateDeckAsync(deckDto, userId);
                    _response.Message = "Deck created successfully.";
                    // Trả về 201 Created sẽ tốt hơn, nhưng Ok() cũng được chấp nhận
                    return Ok(_response);
                }
                catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating deck for user {UserId}", GetUserId());
                    _response.IsSuccess = false;
                    _response.Message = ex.Message;
                    return StatusCode(500, _response);
                }
            }

        // ✨ ENDPOINT MỚI: Cập nhật Deck
        [HttpPut("{deckId}")]
        public async Task<IActionResult> UpdateDeck(int deckId, [FromBody] DeckUpdateDto deckDto)
        {
            if (!ModelState.IsValid) { return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data", Result = ModelState }); }
            try
            {
                var userId = GetUserId();
                var success = await _deckService.UpdateDeckAsync(deckId, deckDto, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Deck not found or you do not have permission to update it.";
                    return NotFound(_response);
                }
                _response.Message = "Deck updated successfully.";
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deck {DeckId} for user {UserId}", deckId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        // ✨ ENDPOINT MỚI: Xóa Deck
        [HttpDelete("{deckId}")]
        public async Task<IActionResult> DeleteDeck(int deckId)
        {
            try
            {
                var userId = GetUserId();
                var success = await _deckService.DeleteDeckAsync(deckId, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Deck not found or you do not have permission to delete it.";
                    return NotFound(_response);
                }
                _response.Message = "Deck deleted successfully.";
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deck {DeckId} for user {UserId}", deckId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response); // Hoặc trả về NoContent() (204) nếu muốn
        }

        // ✨ ENDPOINT MỚI: Đặt trạng thái Public/Private cho Deck
        [HttpPatch("{deckId}/public")] // Dùng PATCH vì chỉ cập nhật một phần
        public async Task<IActionResult> SetDeckPublicStatus(int deckId, [FromBody] SetPublicStatusDto statusDto)
        {
            if (!ModelState.IsValid) { return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data", Result = ModelState }); }
            try
            {
                var userId = GetUserId();
                var success = await _deckService.SetDeckPublicStatusAsync(deckId, statusDto.IsPublic, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Deck not found or you do not have permission to change its status.";
                    return NotFound(_response);
                }
                _response.Message = $"Deck status set to {(statusDto.IsPublic ? "public" : "private")} successfully.";
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting public status for deck {DeckId} for user {UserId}", deckId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        #endregion

        #region Card Endpoints

        // ✨ ENDPOINT MỚI: Thêm Card vào Deck
        [HttpPost("{deckId}/cards")]
        public async Task<IActionResult> AddCardToDeck(int deckId, [FromBody] List<CardCreateDto> cardDto)
        {
            if (!ModelState.IsValid) { return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data", Result = ModelState }); }
            try
            {
                var userId = GetUserId();
                _response.Result = await _deckService.AddCardToDeckAsync(deckId, cardDto, userId);
                _response.Message = "Card added successfully.";
                // Trả về 201 Created sẽ tốt hơn
                // return CreatedAtAction(nameof(GetCardDetails), new { cardId = ((CardDto)_response.Result).Id }, _response);
                return Ok(_response); // Tạm thời dùng OK
            }
            catch (UnauthorizedAccessException ex) // Bắt lỗi cụ thể từ service
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Unauthorized(_response); // 403 Forbidden
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is InvalidOperationException) // Bắt lỗi GetUserId
            {
                return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding card to deck {DeckId} for user {UserId}", deckId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // ✨ ENDPOINT MỚI: Cập nhật Card (Độc lập với Deck)
        // Cân nhắc tạo một CardsController riêng nếu logic phức tạp hơn
        [HttpPut("/api/cards/{cardId}")] // Route độc lập
        public async Task<IActionResult> UpdateCard(int cardId, [FromBody] CardUpdateDto cardDto)
        {
            if (!ModelState.IsValid) { return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data", Result = ModelState }); }
            try
            {
                var userId = GetUserId();
                var success = await _deckService.UpdateCardAsync(cardId, cardDto, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Card not found or you do not have permission to update it.";
                    return NotFound(_response);
                }
                _response.Message = "Card updated successfully.";
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating card {CardId} for user {UserId}", cardId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response);
        }

        // ✨ ENDPOINT MỚI: Xóa Card (Độc lập với Deck)
        [HttpDelete("/api/cards/{cardId}")] // Route độc lập
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            try
            {
                var userId = GetUserId();
                var success = await _deckService.DeleteCardAsync(cardId, userId);
                if (!success)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Card not found or you do not have permission to delete it.";
                    return NotFound(_response);
                }
                _response.Message = "Card deleted successfully.";
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting card {CardId} for user {UserId}", cardId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
            return Ok(_response); // Hoặc NoContent()
        }

        #endregion
        [HttpPost("{deckId}/save")]
        public async Task<IActionResult> SaveDeck(int deckId)
        {
            try
            {
                var userId = GetUserId(); // Lấy ID người dùng hiện tại
                var savedDeckDto = await _deckService.SaveDeckForUserAsync(deckId, userId);

                _response.Result = savedDeckDto;
                _response.Message = "Deck saved to your collection successfully.";
                // Trả về 201 Created cùng với DTO của deck mới
                return CreatedAtAction(nameof(GetDeckDetails), new { deckId = savedDeckDto.Id }, _response);
            }
            catch (KeyNotFoundException ex) // Bắt lỗi không tìm thấy deck/user
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return NotFound(_response);
            }
            catch (InvalidOperationException ex) // Bắt lỗi logic (deck không public, lưu deck của chính mình)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }
            catch (UnauthorizedAccessException ex) // Bắt lỗi GetUserId
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Unauthorized(_response);
            }
            catch (Exception ex) // Bắt các lỗi khác
            {
                _logger.LogError(ex, "Error saving deck {DeckId} for user {UserId}", deckId, GetUserId());
                _response.IsSuccess = false;
                _response.Message = "An error occurred while saving the deck.";
                return StatusCode(500, _response);
            }
        }
    }
}