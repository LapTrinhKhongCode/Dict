using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Dict.DTO;
using System;

namespace Dict.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private const string PdfContainer = "pdfs"; // Azure Blob container name

        public ProjectService(ApplicationDbContext db, IBlobService blobService)
        {
            _db = db;
            _blobService = blobService;
        }

        // ── Helpers ──────────────────────────────────────────────
        private async Task<WorkspaceMember> RequireMemberAsync(int projectId, int userId)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId)
                ?? throw new KeyNotFoundException("Project không tồn tại.");

            return await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId)
                ?? throw new UnauthorizedAccessException("Bạn không thuộc workspace này.");
        }

        private async Task RequireWorkspaceMemberAsync(int workspaceId, int userId)
        {
            _ = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId)
                ?? throw new UnauthorizedAccessException("Bạn không thuộc workspace này.");
        }

        private static ProjectDto ToDto(Project p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            WorkspaceId = p.WorkspaceId,
            CreatedByUserId = p.CreatedByUserId,
            CreatedByUserName = p.CreatedByUser?.UserName ?? "",
            CreatedAt = p.CreatedAt,
            MediaCount = p.OcrJobs?.Count ?? 0,
            VocabularyCount = p.ProjectVocabularies?.Count ?? 0,
        };

        private static MediaDtos ToProjectMediaDto(MediaStore m) => new()
        {
            Id = m.Id,
            FileName = m.FileName,
            MimeType = m.MimeType,
            SizeBytes = m.SizeBytes,
            StorageUrl = m.StorageUrl,
            OwnerId = m.OwnerId,
            OwnerName = m.Owner?.UserName ?? "",
            CreatedAt = m.CreatedAt,
        };

        private static string ComputeSha256(Stream stream)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream);
            stream.Position = 0;
            return Convert.ToHexString(hash).ToLower();
        }

        // ── Project CRUD ──────────────────────────────────────────
        public async Task<List<ProjectDto>> GetByWorkspaceAsync(int workspaceId, int userId)
        {
            await RequireWorkspaceMemberAsync(workspaceId, userId);

            return await _db.Projects
                .Where(p => p.WorkspaceId == workspaceId)
                .Include(p => p.CreatedByUser)
                .Include(p => p.ProjectVocabularies)
                .Include(p => p.OcrJobs)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    WorkspaceId = p.WorkspaceId,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedByUserName = p.CreatedByUser.UserName,
                    CreatedAt = p.CreatedAt,
                    MediaCount = p.OcrJobs.Count,
                    VocabularyCount = p.ProjectVocabularies.Count,
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProjectDto> GetByIdAsync(int projectId, int userId)
        {
            await RequireMemberAsync(projectId, userId);

            var project = await _db.Projects
                .Include(p => p.CreatedByUser)
                .Include(p => p.ProjectVocabularies)
                .Include(p => p.OcrJobs)
                .FirstOrDefaultAsync(p => p.Id == projectId)
                ?? throw new KeyNotFoundException("Project không tồn tại.");

            return ToDto(project);
        }

        public async Task<ProjectDto> CreateAsync(int workspaceId, int userId, CreateProjectDto dto)
        {
            await RequireWorkspaceMemberAsync(workspaceId, userId);

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                WorkspaceId = workspaceId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            await _db.Entry(project).Reference(p => p.CreatedByUser).LoadAsync();
            return ToDto(project);
        }

        public async Task<ProjectDto> UpdateAsync(int projectId, int userId, UpdateProjectDto dto)
        {
            var member = await RequireMemberAsync(projectId, userId);

            var project = await _db.Projects
                .Include(p => p.CreatedByUser)
                .Include(p => p.ProjectVocabularies)
                .Include(p => p.OcrJobs)
                .FirstOrDefaultAsync(p => p.Id == projectId)
                ?? throw new KeyNotFoundException("Project không tồn tại.");

            if (project.CreatedByUserId != userId && member.Role != "Admin")
                throw new UnauthorizedAccessException("Chỉ người tạo hoặc Admin mới sửa được.");

            project.Name = dto.Name ?? project.Name;
            project.Description = dto.Description ?? project.Description;
            await _db.SaveChangesAsync();

            return ToDto(project);
        }

        public async Task DeleteAsync(int projectId, int userId)
        {
            var member = await RequireMemberAsync(projectId, userId);

            var project = await _db.Projects.FindAsync(projectId)
                ?? throw new KeyNotFoundException("Project không tồn tại.");

            if (project.CreatedByUserId != userId && member.Role != "Admin")
                throw new UnauthorizedAccessException("Chỉ người tạo hoặc Admin mới xóa được.");

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }

        // ── Media ─────────────────────────────────────────────────
        public async Task<List<MediaDtos>> GetMediaAsync(int projectId, int userId)
        {
            await RequireMemberAsync(projectId, userId);

            // Truy vấn trực tiếp bằng ProjectId (Nhanh và tối ưu hơn rất nhiều)
            return await _db.MediaStore
                .Where(m => m.ProjectId == projectId)
                .Include(m => m.Owner)
                .Select(m => new MediaDtos
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    MimeType = m.MimeType,
                    SizeBytes = m.SizeBytes,
                    StorageUrl = m.StorageUrl,
                    OwnerId = m.OwnerId,
                    OwnerName = m.Owner.UserName,
                    CreatedAt = m.CreatedAt,
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<MediaDtos> UploadMediaAsync(int projectId, int userId, IFormFile file)
        {
            await RequireMemberAsync(projectId, userId);

            var project = await _db.Projects.FindAsync(projectId)
                ?? throw new KeyNotFoundException("Project không tồn tại.");

            // Tính SHA256 để tránh upload trùng
            using var stream = file.OpenReadStream();
            var sha256 = ComputeSha256(stream);

            // SỬA LẠI: Chỉ kiểm tra trùng lặp trong phạm vi CÙNG Project
            var existing = await _db.MediaStore
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.Sha256 == sha256);

            MediaStore media;
            if (existing != null)
            {
                media = existing;
            }
            else
            {
                // Upload lên Azure Blob Storage
                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                string storageUrl;
                using (var uploadStream = file.OpenReadStream())
                {
                    storageUrl = await _blobService.UploadFileBlobAsync(
                        PdfContainer,
                        uploadStream,
                        file.ContentType,
                        blobName
                    );
                }

                media = new MediaStore
                {
                    WorkspaceId = project.WorkspaceId,
                    ProjectId = projectId, // ĐÃ THÊM: Ánh xạ cứng file này vào Project
                    OwnerId = userId,
                    FileName = file.FileName,
                    MimeType = file.ContentType,
                    SizeBytes = file.Length,
                    StorageUrl = storageUrl,
                    Sha256 = sha256,
                    CreatedAt = DateTime.UtcNow,
                };

                _db.MediaStore.Add(media);
                await _db.SaveChangesAsync();
            }

            // Vẫn giữ logic tạo OcrJob vì nó phục vụ cho tiến trình AI/OCR chạy ngầm
            var alreadyLinked = await _db.OcrJobs
                .AnyAsync(j => j.MediaId == media.Id && j.ProjectId == projectId);

            if (!alreadyLinked)
            {
                _db.OcrJobs.Add(new OcrJob
                {
                    MediaId = media.Id,
                    ProjectId = projectId,
                    UserId = userId,
                    Status = "pending",
                    DetectedText = "",
                    CreatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync();
            }

            await _db.Entry(media).Reference(m => m.Owner).LoadAsync();

            // Lưu ý: Mình giữ nguyên hàm ToProjectMediaDto(media) theo code cũ của bạn
            return ToProjectMediaDto(media);
        }

        public async Task DeleteMediaAsync(int mediaId, int userId)
        {
            var media = await _db.MediaStore
                .Include(m => m.OcrJobs)
                .FirstOrDefaultAsync(m => m.Id == mediaId)
                ?? throw new KeyNotFoundException("File không tồn tại.");

            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == media.WorkspaceId && m.UserId == userId)
                ?? throw new UnauthorizedAccessException("Bạn không có quyền.");

            if (media.OwnerId != userId && member.Role != "Admin")
                throw new UnauthorizedAccessException("Chỉ người upload hoặc Admin mới xóa được.");

            // Xóa file trên Azure Blob
            if (!string.IsNullOrEmpty(media.StorageUrl))
            {
                // Lấy blob name từ URL
                var blobName = Path.GetFileName(new Uri(media.StorageUrl).LocalPath);
                await _blobService.DeleteFileBlobAsync(PdfContainer, blobName);
            }

            _db.MediaStore.Remove(media);
            await _db.SaveChangesAsync();
        }
    }
}