using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;

namespace Dict.Service
{
    public class OcrJobService : IOcrJobService
    {
        private readonly ApplicationDbContext _db;

        // Inject DbContext của bạn vào
        public OcrJobService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Tạo một OcrJob mới và trả về DTO của nó.
        /// </summary>
        public async Task<OcrJobDto> CreateAsync(OcrJobCreateDto createDto)
        {
            // 1. Chuyển đổi DTO sang Entity
            var newJob = new OcrJob
            {
                UserId = createDto.UserId,
                MediaId = createDto.MediaId,
                Status = createDto.Status,
                DetectedText = createDto.DetectedText,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 2. Thêm vào DbContext và Lưu
            _db.OcrJobs.Add(newJob);
            await _db.SaveChangesAsync();

            // 3. Chuyển đổi Entity đã lưu (đã có Id) sang DTO để trả về
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

        /// <summary>
        // Thêm một danh sách các kết quả (từ/dòng) vào một OcrJob đã có.
        /// </summary>
        public async Task AppendResultsAsync(int jobId, List<CreateOcrResultDto> results)
        {
            if (results == null || !results.Any())
            {
                return; // Không có gì để thêm
            }

            // Kiểm tra xem Job có tồn tại không
            var job = await _db.OcrJobs.FindAsync(jobId);
            if (job == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy OcrJob với ID: {jobId}");
            }

            // Chuyển đổi danh sách DTO sang danh sách Entity
            var newResultEntities = results.Select(dto => new OcrResult
            {
                OcrJobId = jobId, // Gán khóa ngoại
                PageNumber = dto.PageNumber,
                WordText = dto.WordText,
                BoundingBox = dto.BoundingBox,
                Confidence = null, // Giả sử DTO không có
                LinkWordId = null  // Giả sử DTO không có
            }).ToList();

            // Thêm hàng loạt (hiệu quả hơn)
            await _db.OcrResults.AddRangeAsync(newResultEntities);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Cập nhật trạng thái và/hoặc văn bản đã phát hiện của một job.
        /// </summary>
        public async Task UpdateStatusAsync(int jobId, OcrJobUpdateStatusDto updateDto)
        {
            var job = await _db.OcrJobs.FindAsync(jobId);

            if (job == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy OcrJob với ID: {jobId}");
            }

            // Cập nhật các trường
            job.Status = updateDto.Status;

            // Chỉ cập nhật DetectedText nếu nó được cung cấp (không phải null)
            // Code trong OcrProcessingService có truyền null, nên cần kiểm tra
            if (updateDto.DetectedText != null)
            {
                job.DetectedText = updateDto.DetectedText;
            }

            job.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }
}
