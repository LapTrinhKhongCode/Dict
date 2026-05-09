using Microsoft.AspNetCore.Mvc;
using Dict.DTO;
using Microsoft.AspNetCore.Authorization;
using Dict.Service.IService;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class WorkspaceInvitationController : ControllerBase
    {
        private readonly IWorkspaceInvitationService _invitationService;
        private ResponseDTO _response;

        public WorkspaceInvitationController(IWorkspaceInvitationService invitationService)
        {
            _invitationService = invitationService;
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


        // Tạo lời mời (Dành cho Admin)
        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember([FromBody] CreateInvitationDTO dto)
        {
            try
            {
                int currentUserId = GetUserId(); // Tự thay bằng logic lấy từ Token (VD: int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                _response.Result = await _invitationService.InviteMemberAsync(currentUserId, dto);
                _response.Message = "Đã gửi lời mời thành công.";
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        // Lấy danh sách lời mời đang chờ duyệt của mình
        [HttpGet("my-pending")]
        public async Task<IActionResult> GetMyPendingInvitations()
        {
            try
            {
                int currentUserId = 1; // Lấy từ Token
                _response.Result = await _invitationService.GetMyPendingInvitationsAsync(currentUserId);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }

        // Đồng ý / Từ chối lời mời

        [HttpPost("{id}/respond")]
            public async Task<IActionResult> RespondToInvitation(int id, [FromQuery] bool accept)
              {
            try
            {
            // 🔥 lấy userId từ JWT
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized();

            int currentUserId = int.Parse(userIdClaim);

            var success = await _invitationService.RespondToInvitationAsync(currentUserId, id, accept);

            if (success)
                _response.Message = accept ? "Bạn đã gia nhập Workspace." : "Đã từ chối lời mời.";
            else
            {
                _response.IsSuccess = false;
                _response.Message = "Lời mời không tồn tại hoặc đã được xử lý.";
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