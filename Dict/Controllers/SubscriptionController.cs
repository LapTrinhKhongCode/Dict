using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Dict.DTO;
using Dict.Service.IService;
using System.Text;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {

        private readonly ResponseDTO _r;
        private readonly ISubscriptionService _ss;
        private readonly ILogger<SubscriptionController> _l;
        public SubscriptionController(ISubscriptionService ss, ILogger<SubscriptionController> l) { _ss = ss; _r = new ResponseDTO(); _l = l; }

        //public SubscriptionController(ISubscriptionService ss, ILogger<SubscriptionController> l)
        //{
        //    _ss = ss;
        //    _r = new ResponseDTO();
        //    _l = l;
        //}

 
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
        [HttpGet("zalopay-status")]
        public async Task<IActionResult> CheckZaloPayStatus([FromQuery] string appTransId, [FromQuery] int userId)
        {
            userId = GetUserId();
            var success = await _ss.QueryOrderAsync(appTransId, userId);
            return Ok(new { success });
        }

        //[Authorize]
        //[HttpPost("checkout-lifetime")]
        //public async Task<IActionResult> CreateZaloPayLifetimeOrder()
        //{
        //    try
        //    {
        //        var userId = GetUserId();
        //        var result = await _ss.CreateZaloPayOrderAsync(userId);
        //        _r.Result = result; _r.Message = "ZaloPay lifetime order created successfully."; return Ok(_r);
        //    }
        //    // Giữ nguyên các catch lỗi cụ thể
        //    catch (UnauthorizedAccessException ex) { _r.IsSuccess = false; _r.Message = ex.Message; return Unauthorized(_r); }
        //    catch (KeyNotFoundException ex) { _r.IsSuccess = false; _r.Message = ex.Message; return NotFound(_r); }
        //    catch (ArgumentException ex) { _r.IsSuccess = false; _r.Message = ex.Message; return BadRequest(_r); } // Thêm catch cho ArgumentException từ service
        //    catch (Exception ex)
        //    { _l.LogError(ex, "Error creating ZaloPay lifetime order via controller for User ID potential: {UserIdClaim}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value); _r.IsSuccess = false; _r.Message = $"Error: {ex.Message}"; return StatusCode(500, _r); }
        //}
        [Authorize]
        [HttpPost("checkout-lifetime")]
        public async Task<IActionResult> CreateZaloPayLifetimeOrder()
        {
            try
            {
                var userId = GetUserId();
                var result = await _ss.CreateZaloPayOrderAsync(userId);

                _r.Result = result;
                _r.Message = "ZaloPay lifetime order created successfully (Sandbox auto-upgraded to PREMIUM).";
                return Ok(_r);
            }
            catch (Exception ex)
            {
                _l.LogError(ex, "Error creating ZaloPay lifetime order for user {UserId}", User.FindFirst("userId")?.Value);
                _r.IsSuccess = false;
                _r.Message = ex.Message;
                return StatusCode(500, _r);
            }
        }


        //[HttpPost("zalopay-callback")]
        //[AllowAnonymous] // Callback không cần xác thực
        //public async Task<IActionResult> ZaloPayCallback()
        //{
        //    string body;
        //    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        //    {
        //        body = await reader.ReadToEndAsync();
        //    }

        //    // ✨ THÊM TRY-CATCH ✨
        //    try
        //    {
        //        var zr = await _ss.HandleZaloPayCallbackAsync(body);

        //        // Trả về kết quả dựa trên xử lý của service
        //        if (zr.ReturnCode == -1) // Lỗi MAC / Format
        //            return BadRequest(zr);
        //        else // Thành công hoặc lỗi nghiệp vụ đã được ghi log bởi service
        //            return Ok(zr);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Lỗi bất ngờ xảy ra trong service mà chưa được xử lý
        //        _l.LogError(ex, "Unhandled exception during ZaloPay callback processing. Body: {CallbackBody}", body);

        //        // Vẫn trả về OK cho ZaloPay nhưng với mã lỗi
        //        var errorResponse = new ZaloPayCallbackResponse
        //        {
        //            ReturnCode = 0, // Hoặc 2 nếu bạn muốn ZaloPay thử lại (xem tài liệu ZP)
        //            ReturnMessage = "Internal server error during callback processing."
        //        };
        //        return Ok(errorResponse);
        //    }
        //}
    }
}
