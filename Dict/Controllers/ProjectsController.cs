using Dict.Data;
using Dict.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

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

    // GET: api/Projects/1/files
    [HttpGet("{projectId}/files")]
    public async Task<IActionResult> GetProjectFiles(int projectId)
    {
        // Kiểm tra xem Project có tồn tại không
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
        {
            return NotFound(new { message = "Không tìm thấy dự án này!" });
        }

        // Lấy danh sách Job thuộc Project này, join với bảng Media để lấy Tên file
        var files = await _context.OcrJobs
            .Include(j => j.Media) // Phải có Navigation Property trong Entity Framework
            .Where(j => j.ProjectId == projectId)
            .OrderByDescending(j => j.CreatedAt) // File mới nhất lên đầu
            .Select(j => new ProjectFileDto
            {
                Id = j.Id, // ID của Job (Frontend dùng cái này để chuyển trang)
                Name = j.Media.FileName ?? "Tài liệu không tên",
                // Lấy đuôi file (ví dụ ".jpg" -> "jpg")
                Type = !string.IsNullOrEmpty(j.Media.FileName)
                        ? Path.GetExtension(j.Media.FileName).Replace(".", "").ToLower()
                        : "unknown",
                Status = j.Status, // "pending", "processing", "completed", "failed"
                CreatedAt = (DateTime)j.CreatedAt
            })
            .ToListAsync();

        return Ok(files);
    }
}