using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dict.Controllers
{
    [ApiController]
    [Authorize]
    public class VocabularyController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public VocabularyController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim == null || !int.TryParse(claim.Value, out var id))
                throw new InvalidOperationException("User ID khong hop le.");
            return id;
        }

        private bool IsSystemPrivileged() =>
            User.IsInRole(Models.Enum.Role.ADMIN) || User.IsInRole(Models.Enum.Role.MODERATOR);

        private async Task<WorkspaceMember?> GetMemberAsync(int projectId, int userId)
        {
            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return null;
            return await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
        }

        // GET /api/projects/{projectId}/vocabularies
        [HttpGet("api/projects/{projectId}/vocabularies")]
        public async Task<IActionResult> GetVocabs(int projectId)
        {
            var userId = GetUserId();
            var member = await GetMemberAsync(projectId, userId);
            if (member == null && !IsSystemPrivileged()) return Forbid();

            var vocabs = await _db.ProjectVocabularies
                .Where(v => v.ProjectId == projectId)
                .Include(v => v.UserAdded)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VocabDto
                {
                    Id = v.Id,
                    WordText = v.WordText,
                    ContextMeaning = v.ContextMeaning,
                    AddedBy = v.AddedBy,
                    AddedByName = v.UserAdded.UserName,
                    CreatedAt = v.CreatedAt,
                })
                .ToListAsync();

            return Ok(vocabs);
        }

        // POST /api/projects/{projectId}/vocabularies
        [HttpPost("api/projects/{projectId}/vocabularies")]
        public async Task<IActionResult> SaveVocab(int projectId, [FromBody] SaveVocabDto dto)
        {
            var userId = GetUserId();
            var member = await GetMemberAsync(projectId, userId);
            if (member == null) return NotFound("Project khong ton tai hoac ban khong phai thanh vien.");

            var canManage = member.Role == WorkspaceRole.ADMIN || IsSystemPrivileged();
            if (!canManage) return Forbid();

            var existing = await _db.ProjectVocabularies
                .FirstOrDefaultAsync(v => v.ProjectId == projectId
                    && v.WordText.ToLower() == dto.WordText.ToLower());
            if (existing != null)
            {
                existing.ContextMeaning = dto.ContextMeaning;
                await _db.SaveChangesAsync();
                return Ok(new VocabDto
                {
                    Id = existing.Id,
                    WordText = existing.WordText,
                    ContextMeaning = existing.ContextMeaning,
                    AddedBy = existing.AddedBy,
                    CreatedAt = existing.CreatedAt,
                });
            }

            var vocab = new ProjectVocabulary
            {
                ProjectId = projectId,
                WordText = dto.WordText,
                ContextMeaning = dto.ContextMeaning,
                AddedBy = userId,
                CreatedAt = DateTime.UtcNow,
            };

            _db.ProjectVocabularies.Add(vocab);
            await _db.SaveChangesAsync();

            return Ok(new VocabDto
            {
                Id = vocab.Id,
                WordText = vocab.WordText,
                ContextMeaning = vocab.ContextMeaning,
                AddedBy = vocab.AddedBy,
                CreatedAt = vocab.CreatedAt,
            });
        }

        // PUT /api/projects/{projectId}/vocabularies/{vocabId}
        [HttpPut("api/projects/{projectId}/vocabularies/{vocabId}")]
        public async Task<IActionResult> UpdateVocab(int projectId, int vocabId, [FromBody] string contextMeaning)
        {
            var userId = GetUserId();
            var member = await GetMemberAsync(projectId, userId);
            if (member == null) return NotFound("Project khong ton tai hoac ban khong phai thanh vien.");

            var canManage = member.Role == WorkspaceRole.ADMIN || IsSystemPrivileged();
            if (!canManage) return Forbid();

            var vocab = await _db.ProjectVocabularies
                .FirstOrDefaultAsync(v => v.Id == vocabId && v.ProjectId == projectId);
            if (vocab == null) return NotFound("Khong tim thay tu vung.");

            vocab.ContextMeaning = contextMeaning;
            await _db.SaveChangesAsync();

            return Ok(new VocabDto
            {
                Id = vocab.Id,
                WordText = vocab.WordText,
                ContextMeaning = vocab.ContextMeaning,
                AddedBy = vocab.AddedBy,
                CreatedAt = vocab.CreatedAt,
            });
        }

        // DELETE /api/projects/{projectId}/vocabularies
        [HttpDelete("api/projects/{projectId}/vocabularies")]
        public async Task<IActionResult> DeleteVocabs(int projectId, [FromQuery] List<int> vocabIds)
        {
            var userId = GetUserId();
            var member = await GetMemberAsync(projectId, userId);
            if (member == null) return NotFound("Project khong ton tai hoac ban khong phai thanh vien.");

            var isPrivileged = member.Role == WorkspaceRole.ADMIN || IsSystemPrivileged();

            var vocabsToDelete = await _db.ProjectVocabularies
                .Where(v => v.ProjectId == projectId && vocabIds.Contains(v.Id))
                .ToListAsync();

            if (!vocabsToDelete.Any()) return NoContent();

            foreach (var vocab in vocabsToDelete)
            {
                if (vocab.AddedBy == userId || isPrivileged)
                    _db.ProjectVocabularies.Remove(vocab);
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
