using Dict.Data;
using Dict.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dict.Controllers
{
    [ApiController]
    [Authorize]
    public class AnnotationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AnnotationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim == null || !int.TryParse(claim.Value, out var id))
                throw new InvalidOperationException("User ID không hợp lệ.");
            return id;
        }

        private async Task<bool> IsMemberAsync(int projectId, int userId)
        {
            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return false;
            return await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
        }

        // GET /api/projects/{projectId}/annotations?ocrJobId=&pageNumber=
        [HttpGet("api/projects/{projectId}/annotations")]
        public async Task<IActionResult> GetAnnotations(int projectId, [FromQuery] int? ocrJobId, [FromQuery] int pageNumber = 1)
        {
            var userId = GetUserId();
            if (!await IsMemberAsync(projectId, userId)) return Forbid();

            var query = _db.Annotations
                .Where(a => a.ProjectId == projectId && a.PageNumber == pageNumber);

            if (ocrJobId.HasValue)
                query = query.Where(a => a.OcrJobId == ocrJobId);

            var annotations = await query
                .Include(a => a.User)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.PageNumber,
                    a.OcrJobId,
                    a.UserId,
                    userName = a.User.UserName,
                    a.Data,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            return Ok(annotations);
        }

        // POST /api/projects/{projectId}/annotations
        [HttpPost("api/projects/{projectId}/annotations")]
        public async Task<IActionResult> SaveAnnotation(int projectId, [FromBody] SaveAnnotationDto dto)
        {
            var userId = GetUserId();
            if (!await IsMemberAsync(projectId, userId)) return Forbid();

            // Mỗi user chỉ có 1 annotation record per (project, ocrJob, page) — upsert
            var existing = await _db.Annotations.FirstOrDefaultAsync(a =>
                a.ProjectId == projectId &&
                a.OcrJobId == dto.OcrJobId &&
                a.PageNumber == dto.PageNumber &&
                a.UserId == userId);

            if (existing != null)
            {
                existing.Data = dto.Data;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Annotations.Add(new Annotation
                {
                    ProjectId = projectId,
                    OcrJobId = dto.OcrJobId,
                    PageNumber = dto.PageNumber,
                    UserId = userId,
                    Data = dto.Data,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        // DELETE /api/projects/{projectId}/annotations?ocrJobId=&pageNumber=
        [HttpDelete("api/projects/{projectId}/annotations")]
        public async Task<IActionResult> ClearMyAnnotations(int projectId, [FromQuery] int? ocrJobId, [FromQuery] int pageNumber = 1)
        {
            var userId = GetUserId();
            if (!await IsMemberAsync(projectId, userId)) return Forbid();

            var toDelete = await _db.Annotations.Where(a =>
                a.ProjectId == projectId &&
                a.OcrJobId == ocrJobId &&
                a.PageNumber == pageNumber &&
                a.UserId == userId).ToListAsync();

            _db.Annotations.RemoveRange(toDelete);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public class SaveAnnotationDto
    {
        public int? OcrJobId { get; set; }
        public int PageNumber { get; set; } = 1;
        public string Data { get; set; } // JSON array of strokes
    }
}
