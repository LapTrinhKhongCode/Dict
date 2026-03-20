using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/workspaces")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _service;

        public WorkspaceController(IWorkspaceService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
            return userId;
        }

        // ── Workspace ─────────────────────────────────────────────

        /// <summary>GET /api/workspaces — Lấy danh sách workspace của tôi</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyWorkspaces()
        {
            var result = await _service.GetMyWorkspacesAsync(GetUserId());
            return Ok(result);
        }

        /// <summary>GET /api/workspaces/{id}</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, GetUserId());
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        /// <summary>POST /api/workspaces — Tạo workspace mới</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkspaceDto dto)
        {
            var result = await _service.CreateAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>PUT /api/workspaces/{id}</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkspaceDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, GetUserId(), dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        /// <summary>DELETE /api/workspaces/{id}</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id, GetUserId());
                return NoContent();
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        // ── Members ───────────────────────────────────────────────

        /// <summary>GET /api/workspaces/{id}/members</summary>
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            try
            {
                var result = await _service.GetMembersAsync(id, GetUserId());
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        }

        /// <summary>POST /api/workspaces/{id}/members — Mời thành viên</summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> InviteMember(int id, [FromBody] InviteMemberDto dto)
        {
            try
            {
                await _service.InviteMemberAsync(id, GetUserId(), dto);
                return Ok(new { message = "Đã mời thành viên thành công." });
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        /// <summary>PUT /api/workspaces/{id}/members/{userId}/role — Đổi role</summary>
        [HttpPut("{id}/members/{userId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, int userId, [FromBody] UpdateMemberRoleDto dto)
        {
            try
            {
                await _service.UpdateMemberRoleAsync(id, GetUserId(), userId, dto);
                return Ok(new { message = "Đã cập nhật role." });
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        /// <summary>DELETE /api/workspaces/{id}/members/{userId} — Xóa thành viên</summary>
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            try
            {
                await _service.RemoveMemberAsync(id, GetUserId(), userId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        /// <summary>POST /api/workspaces/{id}/leave — Tự rời workspace</summary>
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> Leave(int id)
        {
            try
            {
                await _service.LeaveWorkspaceAsync(id, GetUserId());
                return Ok(new { message = "Đã rời workspace." });
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }
    }
}
