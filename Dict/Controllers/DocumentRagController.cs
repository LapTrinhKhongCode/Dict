using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [Route("api/rag/documents")]
    [ApiController]
    [Authorize]
    public class DocumentRagController : ControllerBase
    {
        private readonly IDocumentRagService _documentRagService;

        public DocumentRagController(IDocumentRagService documentRagService)
        {
            _documentRagService = documentRagService;
        }

        [HttpPost("{jobId:int}/index")]
        public async Task<IActionResult> IndexDocument(int jobId)
        {
            try
            {
                int userId = GetUserId();
                var result = await _documentRagService.IndexDocumentAsync(jobId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        }

        [HttpPost("{jobId:int}/ask")]
        public async Task<IActionResult> AskDocument(int jobId, [FromBody] DocumentRagAskRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { message = "Yêu cầu cung cấp 'question'." });

            try
            {
                int userId = GetUserId();
                var result = await _documentRagService.AskDocumentAsync(
                    jobId, userId, request.Question, request.TopK,
                    request.ConversationHistory, request.SessionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        }

        [HttpPost("{jobId:int}/ask/stream")]
        public async Task AskDocumentStream(int jobId, [FromBody] DocumentRagAskRequestDto request)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("X-Accel-Buffering", "no");

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                await Response.WriteAsync("event: error\ndata: Yêu cầu cung cấp câu hỏi.\n\n");
                return;
            }

            int userId;
            try { userId = GetUserId(); }
            catch { await Response.WriteAsync("event: error\ndata: Yêu cầu đăng nhập.\n\n"); return; }

            try
            {
                await foreach (var ev in _documentRagService.AskDocumentStreamAsync(
                    jobId, userId, request.Question, request.TopK,
                    request.ConversationHistory, request.SessionId, request.Mode))
                {
                    await Response.WriteAsync($"event: {ev.Type}\ndata: {ev.Data}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (KeyNotFoundException ex) { await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n"); }
            catch (UnauthorizedAccessException ex) { await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n"); }
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim == null || !int.TryParse(claim.Value, out int userId) || userId <= 0)
                throw new UnauthorizedAccessException("Yêu cầu đăng nhập.");
            return userId;
        }
    }

    [Route("api/rag/workspace")]
    [ApiController]
    [Authorize]
    public class WorkspaceRagController : ControllerBase
    {
        private readonly IDocumentRagService _documentRagService;
        public WorkspaceRagController(IDocumentRagService documentRagService) => _documentRagService = documentRagService;

        [HttpPost("{workspaceId:int}/index-all")]
        public async Task<IActionResult> IndexAll(int workspaceId)
        {
            try
            {
                var claim = User.FindFirst("userId");
                if (claim == null || !int.TryParse(claim.Value, out int userId)) return Unauthorized();
                var result = await _documentRagService.IndexAllInScopeAsync(workspaceId, "workspace", userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (Exception ex) { return Problem(detail: ex.Message, statusCode: 500); }
        }

        [HttpPost("{workspaceId:int}/ask/stream")]
        public async Task AskWorkspaceStream(int workspaceId, [FromBody] DocumentRagAskRequestDto request)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("X-Accel-Buffering", "no");

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                await Response.WriteAsync("event: error\ndata: Yêu cầu cung cấp câu hỏi.\n\n");
                return;
            }

            int userId;
            try
            {
                var claim = User.FindFirst("userId");
                if (claim == null || !int.TryParse(claim.Value, out userId) || userId <= 0)
                    throw new UnauthorizedAccessException();
            }
            catch { await Response.WriteAsync("event: error\ndata: Yêu cầu đăng nhập.\n\n"); return; }

            try
            {
                await foreach (var ev in _documentRagService.AskWorkspaceStreamAsync(
                    workspaceId, userId, request.Question, request.TopK,
                    request.ConversationHistory, request.SessionId, request.Mode))
                {
                    await Response.WriteAsync($"event: {ev.Type}\ndata: {ev.Data}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            }
        }
    }

    [Route("api/rag/project")]
    [ApiController]
    [Authorize]
    public class ProjectRagController : ControllerBase
    {
        private readonly IDocumentRagService _documentRagService;
        public ProjectRagController(IDocumentRagService documentRagService) => _documentRagService = documentRagService;

        [HttpPost("{projectId:int}/index-all")]
        public async Task<IActionResult> IndexAll(int projectId)
        {
            try
            {
                var claim = User.FindFirst("userId");
                if (claim == null || !int.TryParse(claim.Value, out int userId)) return Unauthorized();
                var result = await _documentRagService.IndexAllInScopeAsync(projectId, "project", userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (Exception ex) { return Problem(detail: ex.Message, statusCode: 500); }
        }

        [HttpPost("{projectId:int}/ask/stream")]
        public async Task AskProjectStream(int projectId, [FromBody] DocumentRagAskRequestDto request)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("X-Accel-Buffering", "no");

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                await Response.WriteAsync("event: error\ndata: Yêu cầu cung cấp câu hỏi.\n\n");
                return;
            }

            int userId;
            try
            {
                var claim = User.FindFirst("userId");
                if (claim == null || !int.TryParse(claim.Value, out userId) || userId <= 0)
                    throw new UnauthorizedAccessException();
            }
            catch { await Response.WriteAsync("event: error\ndata: Yêu cầu đăng nhập.\n\n"); return; }

            try
            {
                await foreach (var ev in _documentRagService.AskProjectStreamAsync(
                    projectId, userId, request.Question, request.TopK,
                    request.ConversationHistory, request.SessionId, request.Mode))
                {
                    await Response.WriteAsync($"event: {ev.Type}\ndata: {ev.Data}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            }
        }
    }
}
