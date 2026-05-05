using Dict.Data;
using Dict.DTO.OCR;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class OcrJobService : IOcrJobService
    {
        private readonly ApplicationDbContext _db;

        public OcrJobService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<OcrJobDto> CreateAsync(OcrJobCreateDto createDto)
        {
            var newJob = new OcrJob
            {
                UserId = createDto.UserId,
                MediaId = createDto.MediaId,
                ProjectId = createDto.ProjectId,
                Status = createDto.Status,
                DetectedText = createDto.DetectedText,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.OcrJobs.Add(newJob);
            await _db.SaveChangesAsync();

            return new OcrJobDto
            {
                Id = newJob.Id,
                UserId = newJob.UserId,
                MediaId = newJob.MediaId,
                Status = newJob.Status,
                DetectedText = newJob.DetectedText,
                CreatedAt = newJob.CreatedAt
            };
        }

        public async Task AppendResultsAsync(int jobId, List<CreateOcrResultDto> results)
        {
            if (results == null || !results.Any()) return;

            var job = await _db.OcrJobs.FindAsync(jobId);
            if (job == null)
                throw new KeyNotFoundException($"Không tìm thấy OcrJob với ID: {jobId}");

            var newResultEntities = results.Select(dto => new OcrResult
            {
                OcrJobId = jobId,
                PageNumber = dto.PageNumber,
                WordText = dto.WordText,
                BoundingBox = dto.BoundingBox,
                Confidence = null,
                LinkWordId = null
            }).ToList();

            await _db.OcrResults.AddRangeAsync(newResultEntities);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int jobId, OcrJobUpdateStatusDto updateDto)
        {
            var job = await _db.OcrJobs.FindAsync(jobId);
            if (job == null)
                throw new KeyNotFoundException($"Không tìm thấy OcrJob với ID: {jobId}");

            job.Status = updateDto.Status;
            if (updateDto.DetectedText != null)
                job.DetectedText = updateDto.DetectedText;

            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // 🆕 Append text từng trang vào DetectedText — không ghi đè toàn bộ
        public async Task AppendDetectedTextAsync(int jobId, string newText)
        {
            if (string.IsNullOrEmpty(newText)) return;

            // Dùng ExecuteUpdateAsync để tránh race condition khi nhiều trang
            // upload song song cùng lúc (tránh lost update)
            var affected = await _db.OcrJobs
                .Where(j => j.Id == jobId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(j => j.DetectedText,
                        j => (j.DetectedText ?? "") + newText)
                    .SetProperty(j => j.UpdatedAt,
                        _ => DateTime.UtcNow)
                );

            if (affected == 0)
                throw new KeyNotFoundException($"Không tìm thấy OcrJob với ID: {jobId}");
        }

        // 🆕 Cập nhật status thành "completed" chỉ khi TẤT CẢ trang đã xong
        // Gọi sau mỗi lần upload trang để check xem đã đủ chưa
        public async Task TryCompleteJobAsync(int jobId, int totalPages)
        {
            // Đếm số trang DISTINCT đã có trong DB
            var completedPages = await _db.OcrResults
                .Where(r => r.OcrJobId == jobId)
                .Select(r => r.PageNumber)
                .Distinct()
                .CountAsync();

            if (completedPages >= totalPages)
            {
                await _db.OcrJobs
                    .Where(j => j.Id == jobId)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(j => j.Status, "completed")
                        .SetProperty(j => j.UpdatedAt, DateTime.UtcNow)
                    );

                // Không cần log ở đây vì service không có ILogger
                // Controller/OcrProcessingService sẽ log
            }
        }
    }
}