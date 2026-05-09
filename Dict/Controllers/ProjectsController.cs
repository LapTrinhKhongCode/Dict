using Dict.Data;
using Dict.DTO;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;
using System.Text.Json;

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
                    ? System.IO.Path.GetExtension(j.Media.FileName)
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

    // =========================================================================
    // API XUẤT SEARCHABLE PDF (HỖ TRỢ CẢ ẢNH GỐC & PDF ĐA TRANG)
    // =========================================================================
    [HttpGet("{projectId}/files/{fileId}/export-pdf")]
    public async Task<IActionResult> ExportSearchablePdf(int projectId, int fileId)
    {
        var userId = GetUserId();
        if (userId <= 0) return Unauthorized(new { message = "Yêu cầu đăng nhập." });

        var job = await _context.OcrJobs
            .Include(j => j.Project)
            .Include(j => j.Media)
            .FirstOrDefaultAsync(j => j.Id == fileId && j.ProjectId == projectId);

        if (job == null || job.Media == null) return NotFound(new { message = "Không tìm thấy file!" });

        var results = await _context.OcrResults
            .Where(r => r.OcrJobId == job.Id)
            .ToListAsync();

        if (!results.Any()) return BadRequest(new { message = "Chưa có dữ liệu OCR." });

        // 1. Lấy file gốc về bộ nhớ
        byte[] fileBytes;
        try
        {
            if (job.Media.StorageUrl.StartsWith("http"))
            {
                using var httpClient = new HttpClient();
                fileBytes = await httpClient.GetByteArrayAsync(job.Media.StorageUrl);
            }
            else
            {
                fileBytes = await System.IO.File.ReadAllBytesAsync(job.Media.StorageUrl);
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Lỗi khi đọc file vật lý!" });
        }

        bool isPdf = job.Media.FileName.ToLower().EndsWith(".pdf") || job.Media.StorageUrl.ToLower().EndsWith(".pdf");
        using var ms = new MemoryStream();

        // 2. Nạp Font tiếng Nhật/Việt từ Windows
        string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msgothic.ttc,0");
        if (!System.IO.File.Exists(fontPath.Replace(",0", "")))
        {
            fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "meiryo.ttc,0");
        }
        if (!System.IO.File.Exists(fontPath.Replace(",0", ""))) // Backup cuối cùng
        {
            fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        }

        PdfFont unicodeFont = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);

        // 3. XỬ LÝ THEO LOẠI FILE
        if (isPdf)
        {
            // === LUỒNG XỬ LÝ CHO FILE PDF ===
            using var reader = new PdfReader(new MemoryStream(fileBytes));
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(reader, writer);

            foreach (var item in results)
            {
                if (string.IsNullOrWhiteSpace(item.WordText) || string.IsNullOrWhiteSpace(item.BoundingBox)) continue;
                int pageNum = item.PageNumber ?? 1;
                if (pageNum < 1 || pageNum > pdf.GetNumberOfPages()) continue;

                var page = pdf.GetPage(pageNum);
                var pageSize = page.GetPageSize();

                // LƯU Ý: PDF từ FE đẩy lên AI với scale 2.0 nên hệ số lùi về là 2.0f
                DrawInvisibleText(new PdfCanvas(page), unicodeFont, item.BoundingBox, item.WordText, pageSize, 2.0f);
            }
            pdf.Close();
        }
        else
        {
            // === LUỒNG XỬ LÝ CHO FILE ẢNH ===
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);

            var imageData = ImageDataFactory.Create(fileBytes);
            float imgHeight = imageData.GetHeight();
            float imgWidth = imageData.GetWidth();

            var page = pdf.AddNewPage(new PageSize(imgWidth, imgHeight));
            var canvas = new PdfCanvas(page);
            canvas.AddImageAt(imageData, 0, 0, false);

            var pageSize = page.GetPageSize();

            foreach (var item in results)
            {
                if (string.IsNullOrWhiteSpace(item.WordText) || string.IsNullOrWhiteSpace(item.BoundingBox)) continue;

                // LƯU Ý: Ảnh gốc thì AI đọc với tỷ lệ 1:1 nên hệ số là 1.0f
                DrawInvisibleText(canvas, unicodeFont, item.BoundingBox, item.WordText, pageSize, 1.0f);
            }
            pdf.Close();
        }

        // 4. Trả file về Frontend
        var outBytes = ms.ToArray();
        string exportName = $"Searchable_{System.IO.Path.GetFileNameWithoutExtension(job.Media.FileName)}.pdf";
        return File(outBytes, "application/pdf", exportName);
    }

    // =========================================================================
    // HÀM VẼ CHỮ TÀNG HÌNH (ĐÃ THÔNG MINH HÓA ĐỂ TRỊ BỆNH LỆCH TỌA ĐỘ)
    // =========================================================================
    private void DrawInvisibleText(PdfCanvas canvas, PdfFont font, string boundingBoxJson, string text, iText.Kernel.Geom.Rectangle pageSize, float aiScale)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(boundingBoxJson);
            var root = jsonDoc.RootElement;
            if (root.ValueKind != JsonValueKind.Array) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var point in root.EnumerateArray())
            {
                if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() >= 2)
                {
                    float px = point[0].GetSingle();
                    float py = point[1].GetSingle();
                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;
                }
            }

            if (minX == float.MaxValue) return;

            // Chuyển tọa độ từ ảnh AI (To) về kích thước chuẩn của PDF (Nhỏ hơn theo scale)
            float x = minX / aiScale;
            float y = minY / aiScale;
            float width = (maxX - minX) / aiScale;
            float height = (maxY - minY) / aiScale;

            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            // Bù trừ lề của PDF và lật ngược trục Y
            float pdfX = x + pageSize.GetLeft();
            float pdfY = pageSize.GetTop() - y - height;

            // Đẩy chữ lên một chút cho khớp nét giữa
            pdfY += height * 0.15f;

            // Ép khung tự động co giãn theo viền Bounding Box
            float textWidth = font.GetWidth(text, height);
            float scaleX = textWidth > 0 ? (width / textWidth) * 100f : 100f;

            canvas.BeginText();
            canvas.SetTextRenderingMode(PdfCanvasConstants.TextRenderingMode.INVISIBLE);
            canvas.SetFontAndSize(font, height);
            canvas.SetHorizontalScaling(scaleX);

            canvas.SetTextMatrix(1, 0, 0, 1, pdfX, pdfY);
            canvas.ShowText(text);
            canvas.EndText();
        }
        catch { /* Bỏ qua chữ lỗi để giữ an toàn cho file PDF */ }
    }
}
public class BoundingBoxDto
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}
// Dùng để hứng JSON dạng mảng Object [{"x": 10, "y": 20}, ...]
public class PointDto
{
    public float? x { get; set; }
    public float? y { get; set; }
}