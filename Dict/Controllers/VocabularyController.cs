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
                throw new InvalidOperationException("User ID không hợp lệ.");
            return id;
        }

        // GET /api/projects/{projectId}/vocabularies
        [HttpGet("api/projects/{projectId}/vocabularies")]
        public async Task<IActionResult> GetVocabs(int projectId)
        {
            var userId = GetUserId();

            // Kiểm tra quyền truy cập
            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return NotFound("Project không tồn tại.");

            var isMember = await _db.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
            if (!isMember) return Forbid();

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

            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return NotFound("Project không tồn tại.");

            var isMember = await _db.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
            if (!isMember) return Forbid();

            // Kiểm tra đã có từ này chưa
            var existing = await _db.ProjectVocabularies
                .FirstOrDefaultAsync(v => v.ProjectId == projectId
                    && v.WordText.ToLower() == dto.WordText.ToLower());
            if (existing != null)
            {
                // Cập nhật nghĩa nếu từ đã tồn tại
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

        // PUT /api/projects/{projectId}/vocabularies/{vocabId} (CẬP NHẬT NGHĨA TỪ VỰNG)
        [HttpPut("api/projects/{projectId}/vocabularies/{vocabId}")]
        public async Task<IActionResult> UpdateVocab(int projectId, int vocabId, [FromBody] string ContextMeaning )
        {
            var userId = GetUserId();

            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return NotFound("Project không tồn tại.");

            var isMember = await _db.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
            if (!isMember) return Forbid();

            var vocab = await _db.ProjectVocabularies
                .FirstOrDefaultAsync(v => v.Id == vocabId && v.ProjectId == projectId);

            if (vocab == null) return NotFound("Không tìm thấy từ vựng.");

            // Cho phép người dùng cập nhật lại nghĩa của từ
            vocab.ContextMeaning = ContextMeaning;
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

        // DELETE /api/projects/{projectId}/vocabularies (XÓA NHIỀU TỪ VỰNG CÙNG LÚC)
        [HttpDelete("api/projects/{projectId}/vocabularies")]
        public async Task<IActionResult> DeleteVocabs(int projectId, [FromQuery] List<int> vocabIds)
        {
            var userId = GetUserId();

            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return NotFound("Project không tồn tại.");

            // Lấy thông tin thành viên và Role trong Workspace
            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);

            if (member == null) return Forbid();
            var isAdmin = member.Role == Models.Enum.Role.ADMIN;

            // Tìm danh sách từ vựng cần xóa
            var vocabsToDelete = await _db.ProjectVocabularies
                .Where(v => v.ProjectId == projectId && vocabIds.Contains(v.Id))
                .ToListAsync();

            if (!vocabsToDelete.Any()) return NoContent();

            foreach (var vocab in vocabsToDelete)
            {
                // Chỉ người tạo ra từ đó hoặc Admin mới có quyền xóa
                if (vocab.AddedBy == userId || isAdmin)
                {
                    _db.ProjectVocabularies.Remove(vocab);
                }
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}