using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectController(IProjectService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim == null || !int.TryParse(claim.Value, out var id))
                throw new InvalidOperationException("User ID không hợp lệ.");
            return id;
        }

        // ── Project ───────────────────────────────────────────────

        [HttpGet("api/workspaces/{workspaceId}/projects")]
        public async Task<IActionResult> GetByWorkspace(int workspaceId)
        {
            try { return Ok(await _service.GetByWorkspaceAsync(workspaceId, GetUserId())); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        }

        [HttpGet("api/projects/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try { return Ok(await _service.GetByIdAsync(id, GetUserId())); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpPost("api/workspaces/{workspaceId}/projects")]
        public async Task<IActionResult> Create(int workspaceId, [FromBody] CreateProjectDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(workspaceId, GetUserId(), dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        }

        [HttpPut("api/projects/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto)
        {
            try { return Ok(await _service.UpdateAsync(id, GetUserId(), dto)); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpDelete("api/projects/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _service.DeleteAsync(id, GetUserId()); return NoContent(); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        // ── Media ─────────────────────────────────────────────────

        [HttpGet("api/projects/{projectId}/media")]
        public async Task<IActionResult> GetMedia(int projectId)
        {
            try { return Ok(await _service.GetMediaAsync(projectId, GetUserId())); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpPost("api/projects/{projectId}/media")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
        public async Task<IActionResult> UploadMedia(int projectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file.");
            if (file.ContentType != "application/pdf")
                return BadRequest("Chỉ hỗ trợ file PDF.");
            try { return Ok(await _service.UploadMediaAsync(projectId, GetUserId(), file)); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpDelete("api/media/{mediaId}")]
        public async Task<IActionResult> DeleteMedia(int mediaId)
        {
            try { await _service.DeleteMediaAsync(mediaId, GetUserId()); return NoContent(); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }
    }
}
