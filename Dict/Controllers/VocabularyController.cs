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
                .Include(v => v.SourceOcrJob).ThenInclude(j => j.Media)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VocabDto
                {
                    Id = v.Id,
                    WordText = v.WordText,
                    ContextMeaning = v.ContextMeaning,
                    AddedBy = v.AddedBy,
                    AddedByName = v.UserAdded.UserName,
                    CreatedAt = v.CreatedAt,
                    SourceOcrJobId = v.SourceOcrJobId,
                    SourcePage = v.SourcePage,
                    SourceSentence = v.SourceSentence,
                    SourceFileName = v.SourceOcrJob != null && v.SourceOcrJob.Media != null
                        ? v.SourceOcrJob.Media.FileName : null,
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

            var existing = await _db.ProjectVocabularies
                .FirstOrDefaultAsync(v => v.ProjectId == projectId
                    && v.WordText.ToLower() == dto.WordText.ToLower());
            if (existing != null)
            {
                existing.ContextMeaning = dto.ContextMeaning;
                // Update source if provided
                if (dto.SourceOcrJobId.HasValue)
                {
                    existing.SourceOcrJobId = dto.SourceOcrJobId;
                    existing.SourcePage = dto.SourcePage;
                    existing.SourceSentence = dto.SourceSentence;
                }
                // If no card linked yet, create one now
                if (!existing.CardId.HasValue)
                {
                    existing.CardId = await CreateLinkedCardAsync(existing.WordText, existing.ContextMeaning);
                }
                else
                {
                    // Update the linked card's back text to reflect new meaning
                    var card = await _db.Cards.FindAsync(existing.CardId);
                    if (card != null) { card.BackText = existing.ContextMeaning; card.UpdatedAt = DateTime.UtcNow; }
                }
                await _db.SaveChangesAsync();
                return Ok(new VocabDto
                {
                    Id = existing.Id,
                    WordText = existing.WordText,
                    ContextMeaning = existing.ContextMeaning,
                    AddedBy = existing.AddedBy,
                    CreatedAt = existing.CreatedAt,
                    SourceOcrJobId = existing.SourceOcrJobId,
                    SourcePage = existing.SourcePage,
                    SourceSentence = existing.SourceSentence,
                });
            }

            var newCardId = await CreateLinkedCardAsync(dto.WordText, dto.ContextMeaning);

            var vocab = new ProjectVocabulary
            {
                ProjectId = projectId,
                WordText = dto.WordText,
                ContextMeaning = dto.ContextMeaning,
                AddedBy = userId,
                CreatedAt = DateTime.UtcNow,
                SourceOcrJobId = dto.SourceOcrJobId,
                SourcePage = dto.SourcePage,
                SourceSentence = dto.SourceSentence,
                CardId = newCardId,
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
                SourceOcrJobId = vocab.SourceOcrJobId,
                SourcePage = vocab.SourcePage,
                SourceSentence = vocab.SourceSentence,
            });
        }

        private async Task<int?> CreateLinkedCardAsync(string wordText, string meaning)
        {
            try
            {
                var card = new Card
                {
                    FrontText = wordText,
                    BackText = meaning ?? "",
                    DeckId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _db.Cards.Add(card);
                await _db.SaveChangesAsync();
                return card.Id;
            }
            catch { return null; }
        }

        // PUT /api/projects/{projectId}/vocabularies/{vocabId}
        [HttpPut("api/projects/{projectId}/vocabularies/{vocabId}")]
        public async Task<IActionResult> UpdateVocab(int projectId, int vocabId, [FromBody] string contextMeaning)
        {
            var userId = GetUserId();
            var member = await GetMemberAsync(projectId, userId);
            if (member == null) return NotFound("Project khong ton tai hoac ban khong phai thanh vien.");

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
                SourceOcrJobId = vocab.SourceOcrJobId,
                SourcePage = vocab.SourcePage,
                SourceSentence = vocab.SourceSentence,
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

        // GET /api/projects/{projectId}/vocabularies/{word}/occurrences
        // Tìm tất cả vị trí trong project chứa từ đó (search OcrResults)
        [HttpGet("api/projects/{projectId}/vocabularies/{word}/occurrences")]
        public async Task<IActionResult> GetOccurrences(int projectId, string word)
        {
            var userId = GetUserId();
            var member = await GetMemberAsync(projectId, userId);
            if (member == null && !IsSystemPrivileged()) return Forbid();

            // Group by OcrJob (file), rồi by page
            var results = await _db.OcrResults
                .Where(r => r.OcrJob.ProjectId == projectId
                         && r.WordText != null
                         && r.WordText.Contains(word))
                .Include(r => r.OcrJob).ThenInclude(j => j.Media)
                .GroupBy(r => new { r.OcrJobId, r.PageNumber })
                .Select(g => new
                {
                    FileId = g.Key.OcrJobId,
                    Page = g.Key.PageNumber ?? 1,
                    MatchCount = g.Count(),
                    FileName = g.First().OcrJob.Media != null
                        ? g.First().OcrJob.Media.FileName : "Tài liệu",
                    DetectedText = g.First().OcrJob.DetectedText,
                })
                .OrderBy(x => x.FileId).ThenBy(x => x.Page)
                .ToListAsync();

            // Extract snippet from DetectedText around the word
            var occurrences = results.Select(r =>
            {
                string? snippet = null;
                if (!string.IsNullOrEmpty(r.DetectedText))
                {
                    var idx = r.DetectedText.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var start = Math.Max(0, idx - 40);
                        var end = Math.Min(r.DetectedText.Length, idx + word.Length + 60);
                        snippet = (start > 0 ? "…" : "") +
                                  r.DetectedText[start..end].Trim() +
                                  (end < r.DetectedText.Length ? "…" : "");
                    }
                }
                return new VocabOccurrenceDto
                {
                    FileId = r.FileId ?? 0,
                    FileName = r.FileName,
                    Page = r.Page,
                    MatchCount = r.MatchCount,
                    Snippet = snippet,
                };
            }).ToList();

            return Ok(occurrences);
        }

        // GET /api/projects/my-vocabs
        // Lấy tất cả từ vựng từ tất cả project user đang tham gia
        [HttpGet("api/projects/my-vocabs")]
        public async Task<IActionResult> GetMyVocabs()
        {
            var userId = GetUserId();

            // Projects user là member (qua workspace membership)
            var myProjectIds = await _db.WorkspaceMembers
                .Where(wm => wm.UserId == userId)
                .SelectMany(wm => _db.Projects
                    .Where(p => p.WorkspaceId == wm.WorkspaceId)
                    .Select(p => p.Id))
                .Distinct()
                .ToListAsync();

            if (!myProjectIds.Any()) return Ok(new List<MyVocabDto>());

            var vocabs = await _db.ProjectVocabularies
                .Where(v => myProjectIds.Contains(v.ProjectId))
                .Include(v => v.Project)
                .Include(v => v.SourceOcrJob).ThenInclude(j => j.Media)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new MyVocabDto
                {
                    Id = v.Id,
                    WordText = v.WordText,
                    ContextMeaning = v.ContextMeaning,
                    ProjectId = v.ProjectId,
                    ProjectName = v.Project.Name,
                    CreatedAt = v.CreatedAt,
                    SourceOcrJobId = v.SourceOcrJobId,
                    SourcePage = v.SourcePage,
                    SourceSentence = v.SourceSentence,
                    SourceFileName = v.SourceOcrJob != null && v.SourceOcrJob.Media != null
                        ? v.SourceOcrJob.Media.FileName : null,
                    CardId = v.CardId,
                    ReviewReps = v.CardId != null
                        ? _db.CardStates
                            .Where(cs => cs.CardId == v.CardId && cs.UserId == userId)
                            .Select(cs => (int?)cs.Reps).FirstOrDefault()
                        : null,
                    DueDate = v.CardId != null
                        ? _db.CardStates
                            .Where(cs => cs.CardId == v.CardId && cs.UserId == userId)
                            .Select(cs => cs.DueDate).FirstOrDefault()
                        : null,
                    LastReviewedAt = v.CardId != null
                        ? _db.CardStates
                            .Where(cs => cs.CardId == v.CardId && cs.UserId == userId)
                            .Select(cs => cs.LastReviewedAt).FirstOrDefault()
                        : null,
                })
                .ToListAsync();

            return Ok(vocabs);
        }
    }
}
