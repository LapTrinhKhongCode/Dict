using Dict.Data;
using Dict.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bắt buộc đăng nhập
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectsController(ApplicationDbContext context)
    {
        _context = context;
    }
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
        }
        return userId;
    }
    // GET: api/Projects/1/files
    [HttpGet("{projectId}/files")]
    public async Task<IActionResult> GetProjectFiles(int projectId)
    {
        // Kiểm tra Project tồn tại
        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == projectId);

        if (!projectExists)
        {
            return NotFound(new
            {
                message = "Không tìm thấy dự án này!"
            });
        }

        // Lấy danh sách file
        var files = await _context.OcrJobs
            .Include(j => j.Media)
            .Where(j => j.ProjectId == projectId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new ProjectFileDto
            {
                Id = j.Id,

                Name = j.Media != null &&
                       !string.IsNullOrEmpty(j.Media.FileName)
                    ? j.Media.FileName
                    : "Tài liệu không tên",

                Type = j.Media != null &&
                       !string.IsNullOrEmpty(j.Media.FileName)
                    ? Path.GetExtension(j.Media.FileName)
                        .Replace(".", "")
                        .ToLower()
                    : "unknown",

                Status = j.Status,

                CreatedAt = (DateTime)j.CreatedAt,

                // QUAN TRỌNG
                ImageUrl = j.Media != null
                    ? j.Media.StorageUrl
                    : null
            })
            .ToListAsync();

        return Ok(files);
    }
    [HttpPut("{projectId}/files/{fileId}")]
    public async Task<IActionResult> UpdateProjectFile(int projectId, int fileId, [FromBody] UpdateProjectFileDto dto)
    {
        var userId = GetUserId();
        if (userId <= 0)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập." });
        }

        var job = await _context.OcrJobs
            .Include(j => j.Project)
            .Include(j => j.Media)
            .FirstOrDefaultAsync(j => j.Id == fileId && j.ProjectId == projectId);

        if (job == null)
        {
            return NotFound(new { message = "Không tìm thấy file trong dự án này!" });
        }

        var isAdmin = await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == job.Project.WorkspaceId
                         && wm.UserId == userId
                         && wm.Role == "Admin");

        if (!isAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Chỉ Admin mới có quyền chỉnh sửa file." });
        }

        // ✅ CHỈ update nếu có giá trị mới
        if (!string.IsNullOrWhiteSpace(dto.FileName) && job.Media != null)
        {
            _context.Entry(job.Media).Property(x => x.FileName).IsModified = true;
            job.Media.FileName = dto.FileName;
        }

        // ✅ chỉ update timestamp
        job.UpdatedAt = DateTime.UtcNow;
        _context.Entry(job).Property(x => x.UpdatedAt).IsModified = true;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật file thành công!" });
    }
    [HttpDelete("{projectId}/files/{fileId}")]
    public async Task<IActionResult> DeleteProjectFile(int projectId, int fileId)
    {
        var userId = GetUserId();
        if (userId <= 0)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập." });
        }

        var job = await _context.OcrJobs
            .Include(j => j.Project)
            .FirstOrDefaultAsync(j => j.Id == fileId && j.ProjectId == projectId);

        if (job == null)
        {
            return NotFound(new { message = "Không tìm thấy file!" });
        }

        var isAdmin = await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == job.Project.WorkspaceId
                         && wm.UserId == userId
                         && wm.Role == "Admin");

        if (!isAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Chỉ Admin mới có quyền xóa file." });
        }

        // 🔥 XÓA OCR RESULTS TRƯỚC
        var results = await _context.OcrResults
            .Where(r => r.OcrJobId == job.Id)
            .ToListAsync();

        if (results.Any())
        {
            _context.OcrResults.RemoveRange(results);
        }

        // 🔥 XÓA MEDIA
        if (job.MediaId.HasValue)
        {
            var mediaRecord = await _context.MediaStore.FindAsync(job.MediaId.Value);
            if (mediaRecord != null)
            {
                _context.MediaStore.Remove(mediaRecord);
            }
        }

        // 🔥 CUỐI CÙNG MỚI XÓA JOB
        _context.OcrJobs.Remove(job);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa file thành công!" });
    }
}