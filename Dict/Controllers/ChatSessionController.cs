using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Dict.Controllers
{
    [Route("api/chat/sessions")]
    [ApiController]
    [Authorize]
    public class ChatSessionController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ChatSessionController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /api/chat/sessions?scopeType=file&scopeId=84
        [HttpGet]
        public async Task<IActionResult> ListSessions([FromQuery] string scopeType, [FromQuery] int scopeId)
        {
            int userId = GetUserId();
            var sessions = await _db.ChatSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.ScopeType == scopeType && s.ScopeId == scopeId)
                .OrderByDescending(s => s.IsPinned)
                .ThenByDescending(s => s.UpdatedAt)
                .Select(s => new ChatSessionDto
                {
                    Id = s.Id,
                    ScopeType = s.ScopeType,
                    ScopeId = s.ScopeId,
                    Title = s.Title,
                    IsPinned = s.IsPinned,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    MessageCount = s.Messages.Count
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // GET /api/chat/sessions/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSession(int id)
        {
            int userId = GetUserId();
            var session = await _db.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null) return NotFound();

            return Ok(new ChatSessionDetailDto
            {
                Id = session.Id,
                Title = session.Title,
                ScopeType = session.ScopeType,
                ScopeId = session.ScopeId,
                CreatedAt = session.CreatedAt,
                Messages = session.Messages.Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    SourcesJson = m.SourcesJson,
                    CitationsJson = m.CitationsJson,
                    CacheHit = m.CacheHit,
                    CreatedAt = m.CreatedAt
                }).ToList()
            });
        }

        // POST /api/chat/sessions
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequestDto request)
        {
            int userId = GetUserId();
            var session = new ChatSession
            {
                UserId = userId,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? "Hội thoại mới" : request.Title.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync();
            return Ok(new ChatSessionDto { Id = session.Id, Title = session.Title, ScopeType = session.ScopeType, ScopeId = session.ScopeId, CreatedAt = session.CreatedAt, UpdatedAt = session.UpdatedAt });
        }

        // POST /api/chat/sessions/{id}/turn  — save one Q&A turn
        [HttpPost("{id:int}/turn")]
        public async Task<IActionResult> SaveTurn(int id, [FromBody] SaveChatTurnRequestDto request)
        {
            int userId = GetUserId();
            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();

            var now = DateTime.UtcNow;
            _db.ChatMessages.AddRange(
                new ChatMessage { ChatSessionId = id, Role = "user", Content = request.UserMessage, CreatedAt = now },
                new ChatMessage { ChatSessionId = id, Role = "assistant", Content = request.AssistantMessage, SourcesJson = request.SourcesJson, CitationsJson = request.CitationsJson, CacheHit = request.CacheHit, CreatedAt = now.AddMilliseconds(1) }
            );

            // Auto-title from first user message
            if (session.Title == "Hội thoại mới" && !string.IsNullOrWhiteSpace(request.UserMessage))
                session.Title = request.UserMessage.Length > 60 ? request.UserMessage[..60] + "..." : request.UserMessage;

            session.UpdatedAt = now;
            await _db.SaveChangesAsync();
            return Ok(new { sessionId = id, title = session.Title });
        }

        // PATCH /api/chat/sessions/{id}/title
        [HttpPatch("{id:int}/title")]
        public async Task<IActionResult> UpdateTitle(int id, [FromBody] UpdateSessionTitleDto request)
        {
            int userId = GetUserId();
            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();
            session.Title = request.Title.Trim();
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { title = session.Title });
        }

        // PATCH /api/chat/sessions/{id}/pin
        [HttpPatch("{id:int}/pin")]
        public async Task<IActionResult> TogglePin(int id)
        {
            int userId = GetUserId();
            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();
            session.IsPinned = !session.IsPinned;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { isPinned = session.IsPinned });
        }

        // DELETE /api/chat/sessions/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            int userId = GetUserId();
            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();
            _db.ChatSessions.Remove(session);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã xóa hội thoại." });
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim == null || !int.TryParse(claim.Value, out int userId) || userId <= 0)
                throw new UnauthorizedAccessException("Yêu cầu đăng nhập.");
            return userId;
        }
    }
}
