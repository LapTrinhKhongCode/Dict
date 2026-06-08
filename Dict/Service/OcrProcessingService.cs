using Dict.Data;
using Dict.Hubs;
using Dict.Models;
using Dict.Service.IService;
using ImageMagick;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Dict.DTO.OCR;
using Google.Cloud.Vision.V1;

namespace Dict.Service
{
    public class OcrProcessingService : IOcrProcessingService
    {
        // Định nghĩa các lớp parse JSON nội bộ
        private class MainTextItem { public int[] Bbox { get; set; } public string Text { get; set; } public List<List<int>> BoxPoints { get; set; } public string Type { get; set; } }
        private class FuriganaItem { public string Text { get; set; } public int[] Position { get; set; } public string Type { get; set; } }
        private class ImageInfo { public int Width { get; set; } public int Height { get; set; } }
        private class ApiResult { public List<MainTextItem> Main_Text { get; set; } public List<FuriganaItem> Furigana { get; set; } public ImageInfo Image_Info { get; set; } public string Annotated_Image { get; set; } public string Detected_Text_Lines { get; set; } }

        private static class OcrStatus
        {
            public const string Pending = "pending";
            public const string Processing = "processing";
            public const string Completed = "completed";
            public const string Failed = "failed";
        }

        private static readonly TimeSpan VisionTimeout = TimeSpan.FromSeconds(60);
        private const string OcrCachePrefix = "ocr_job_";
        private static readonly TimeSpan OcrCacheTtl = TimeSpan.FromMinutes(10);

        // Max width ảnh gửi lên Vision — đủ để nhận diện tốt, giảm bandwidth
        private const int MaxImageWidth = 1500;

        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<OcrProcessingService> _logger;
        private readonly IOcrJobService _ocrJobService;
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private readonly ImageAnnotatorClient _visionClient;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub> _hub;

        public OcrProcessingService(
            IHttpClientFactory httpFactory,
            ILogger<OcrProcessingService> logger,
            IOcrJobService ocrJobService,
            ApplicationDbContext db,
            IBlobService blobService,
            ImageAnnotatorClient visionClient,
            IMemoryCache cache,
            IHubContext<NotificationHub> hub)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _ocrJobService = ocrJobService;
            _db = db;
            _blobService = blobService;
            _visionClient = visionClient;
            _cache = cache;
            _hub = hub;
        }
        public async Task<IEnumerable<OcrJobDetailDto>> GetRecentOcrJobsForUserAsync(int userId, int limit = 5)
        {
            var jobs = await _db.OcrJobs
                .AsNoTracking()
                .Where(job => job.UserId == userId)
                .OrderByDescending(job => job.CreatedAt)
                .Take(limit)
                .Include(job => job.Media)
                .Select(job => new OcrJobDetailDto
                {
                    Id = job.Id,
                    Status = job.Status,
                    DetectedText = job.DetectedText,
                    CreatedAt = job.CreatedAt,
                    ImageUrl = job.Media != null ? job.Media.StorageUrl : null,
                    Results = new List<OcrResultDto>() // Không load results ở list view — gọi riêng khi cần
                })
                .ToListAsync();

            return jobs;
        }
        // --- HÀM 1: UPLOAD + GỌI VISION SONG SONG — trả kết quả ngay trong 1 request ---
        public async Task<OcrProcessingResultDto> UploadImageOnlyAsync(IFormFile image, int userId, int workspaceId, int? projectId)
        {
            byte[] originalImageBytes;
            await using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                originalImageBytes = ms.ToArray();
            }

            string sha256 = ComputeSha256(originalImageBytes);

            // ── Chạy Azure upload và Google Vision song song ──────────────────────
            // Vision dùng bytes trong RAM — không cần download lại từ Azure
            var visionTask = CallVisionAsync(originalImageBytes);

            int originalMediaId;
            string uploadedUrl;
            try
            {
                var existing = await _db.MediaStore
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Sha256 == sha256 && m.OwnerId == userId);

                if (existing != null)
                {
                    _logger.LogInformation("♻️ Dedup HIT — tái sử dụng MediaStore {Id}", existing.Id);
                    originalMediaId = existing.Id;
                    uploadedUrl = existing.StorageUrl;
                }
                else
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                    // Compress trước khi upload Azure (không ảnh hưởng Vision vì Vision dùng originalBytes)
                    var compressedBytes = CompressImage(originalImageBytes);
                    await using (var stream = new MemoryStream(compressedBytes))
                    {
                        uploadedUrl = await _blobService.UploadFileBlobAsync(
                            containerName: "ocr-images",
                            content: stream,
                            contentType: "image/jpeg",
                            fileName: uniqueFileName
                        );
                    }

                    var originalMedia = new MediaStore
                    {
                        OwnerId = userId,
                        WorkspaceId = workspaceId,
                        FileName = image.FileName,
                        MimeType = image.ContentType,
                        ProjectId = projectId,
                        SizeBytes = image.Length,
                        StorageUrl = uploadedUrl,
                        Sha256 = sha256,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.MediaStore.Add(originalMedia);
                    await _db.SaveChangesAsync();
                    originalMediaId = originalMedia.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu Azure Blob.");
                throw new Exception("Lỗi lưu ảnh lên Cloud.", ex);
            }

            // ── Tạo job và chờ Vision hoàn thành ─────────────────────────────────
            var jobDto = await _ocrJobService.CreateAsync(new OcrJobCreateDto
            {
                UserId = userId,
                MediaId = originalMediaId,
                ProjectId = projectId,
                Status = OcrStatus.Processing,
                DetectedText = string.Empty
            });

            try
            {
                var (createResults, fullText) = await visionTask;

                if (createResults.Any())
                    await _ocrJobService.AppendResultsAsync(jobDto.Id, createResults);

                await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto
                {
                    Status = OcrStatus.Completed,
                    DetectedText = fullText
                });

                _logger.LogInformation("✅ UploadImageOnlyAsync: Vision xong Job {JobId} — {Count} từ", jobDto.Id, createResults.Count);

                var completedDto = new OcrProcessingResultDto
                {
                    JobId = jobDto.Id,
                    Status = OcrStatus.Completed,
                    DetectedText = fullText,
                    MediaId = originalMediaId,
                    ImageUrl = uploadedUrl,
                    Results = createResults
                };

                _cache.Set($"{OcrCachePrefix}{jobDto.Id}", completedDto, OcrCacheTtl);
                return completedDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Vision thất bại cho Job {JobId}", jobDto.Id);
                await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto
                {
                    Status = OcrStatus.Failed,
                    DetectedText = string.Empty
                });

                return new OcrProcessingResultDto
                {
                    JobId = jobDto.Id,
                    Status = OcrStatus.Failed,
                    MediaId = originalMediaId,
                    ImageUrl = uploadedUrl,
                    Results = new List<CreateOcrResultDto>()
                };
            }
        }

        // Helper: gọi Google Vision với bytes trong RAM
        private async Task<(List<CreateOcrResultDto> results, string fullText)> CallVisionAsync(byte[] imageBytes)
        {
            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
            var response = await _visionClient.DetectDocumentTextAsync(googleImage);

            var createResults = new List<CreateOcrResultDto>();
            var sb = new StringBuilder();

            if (response?.Text != null)
            {
                foreach (var page in response.Pages)
                    foreach (var block in page.Blocks)
                        foreach (var paragraph in block.Paragraphs)
                            foreach (var word in paragraph.Words)
                            {
                                string wordText = string.Join("", word.Symbols.Select(s => s.Text));
                                sb.Append(wordText).Append(" ");
                                var bboxList = word.BoundingBox.Vertices.Select(v => new[] { v.X, v.Y }).ToList();
                                createResults.Add(new CreateOcrResultDto
                                {
                                    PageNumber = 1,
                                    WordText = wordText,
                                    BoundingBox = JsonSerializer.Serialize(bboxList)
                                });
                            }
            }

            return (createResults, sb.ToString().Trim());
        }


        // --- HÀM 2: GỌI GOOGLE VISION KHI FRONTEND YÊU CẦU ---
        public async Task<OcrProcessingResultDto> ProcessOcrLazyAsync(int jobId)
        {
            // 0. Memory Cache — completed jobs không bao giờ thay đổi
            var cacheKey = $"{OcrCachePrefix}{jobId}";
            if (_cache.TryGetValue(cacheKey, out OcrProcessingResultDto cached))
            {
                _logger.LogInformation("⚡ Memory Cache HIT — Job {JobId}", jobId);
                return cached;
            }

            // 1. Lấy thông tin Job kèm kết quả
            var ocrJob = await _db.OcrJobs
                .Include(j => j.Media)
                .Include(j => j.Results)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (ocrJob == null) return null;

            // 2. Completed → cache và trả về
            if (ocrJob.Status == OcrStatus.Completed && ocrJob.Results != null && ocrJob.Results.Any())
            {
                _logger.LogInformation("✅ DB Cache HIT — Job {JobId}", jobId);
                var dto = MapToResultDto(ocrJob);
                _cache.Set(cacheKey, dto, OcrCacheTtl);
                return dto;
            }
            bool isPdf = ocrJob.Media != null &&
                 !string.IsNullOrEmpty(ocrJob.Media.MimeType) &&
                 ocrJob.Media.MimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase);

            if (isPdf)
            {
                _logger.LogInformation("📄 Job {JobId} là file PDF. Nhường quyền OCR cho luồng Lazy Load từng trang của Frontend.", jobId);

                if (ocrJob.Results != null && ocrJob.Results.Any() && ocrJob.Status == OcrStatus.Pending)
                {
                    ocrJob.Status = OcrStatus.Processing;
                    await _db.SaveChangesAsync();
                }

                return MapToResultDto(ocrJob);
            }

            // 3. Nếu đang "processing" (request khác đang xử lý) thì trả về data hiện tại luôn
            if (ocrJob.Status == OcrStatus.Processing)
            {
                _logger.LogInformation("⏳ Job {JobId} đang được xử lý bởi request khác — trả về data hiện tại", jobId);
                return MapToResultDto(ocrJob);
            }

            // 4. Atomic update: claim job nếu status là "pending" HOẶC "failed" (cho phép retry)
            var claimed = await _db.OcrJobs
                .Where(j => j.Id == jobId && (j.Status == OcrStatus.Pending || j.Status == OcrStatus.Failed))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, OcrStatus.Processing)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow));

            if (claimed == 0)
            {
                // Có request khác vừa claim trước — re-fetch và trả về
                var current = await _db.OcrJobs
                    .AsNoTracking()
                    .Include(j => j.Media)
                    .Include(j => j.Results)
                    .FirstOrDefaultAsync(j => j.Id == jobId);
                return current == null ? null : MapToResultDto(current);
            }

            _logger.LogInformation("🚀 Bắt đầu gọi Google Vision cho Job {JobId} (File Ảnh)", jobId);
            // Reload để có Media URL
            ocrJob = await _db.OcrJobs.Include(j => j.Media).Include(j => j.Results).FirstAsync(j => j.Id == jobId);

            try
            {
                using var cts = new CancellationTokenSource(VisionTimeout);
                using var httpClient = _httpFactory.CreateClient();
                byte[] imageBytes = await httpClient.GetByteArrayAsync(ocrJob.Media.StorageUrl, cts.Token);

                var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
                var visionCallCts = new CancellationTokenSource(VisionTimeout);
                var response = await _visionClient.DetectDocumentTextAsync(googleImage);

                var createResults = new List<CreateOcrResultDto>();
                var fullTextBuilder = new StringBuilder();

                if (response?.Text != null)
                {
                    foreach (var page in response.Pages)
                        foreach (var block in page.Blocks)
                            foreach (var paragraph in block.Paragraphs)
                                foreach (var word in paragraph.Words)
                                {
                                    string wordText = string.Join("", word.Symbols.Select(s => s.Text));
                                    fullTextBuilder.Append(wordText).Append(" ");
                                    var bboxList = word.BoundingBox.Vertices.Select(v => new[] { v.X, v.Y }).ToList();

                                    createResults.Add(new CreateOcrResultDto
                                    {
                                        PageNumber = 1,
                                        WordText = wordText,
                                        BoundingBox = JsonSerializer.Serialize(bboxList)
                                    });
                                }
                }

                // Lưu kết quả vào DB
                if (createResults.Any())
                {
                    await _ocrJobService.AppendResultsAsync(jobId, createResults);

                    // Gán ngược lại vào object hiện tại để trả về FE luôn
                    ocrJob.Results = createResults.Select(r => new OcrResult
                    {
                        PageNumber = r.PageNumber,
                        WordText = r.WordText,
                        BoundingBox = r.BoundingBox
                    }).ToList();
                }

                string finalText = fullTextBuilder.ToString().Trim();
                ocrJob.Status = OcrStatus.Completed;
                ocrJob.DetectedText = finalText;

                await _db.SaveChangesAsync();
                _logger.LogInformation("✅ OCR hoàn tất cho Job {JobId}", jobId);

                // Cache kết quả completed
                var completedDto = MapToResultDto(ocrJob);
                _cache.Set(cacheKey, completedDto, OcrCacheTtl);

                // SignalR: push về client đang chờ trong room OcrJob_{jobId}
                await _hub.Clients.Group($"OcrJob_{jobId}")
                    .SendAsync("OcrCompleted", new { jobId, status = OcrStatus.Completed, wordCount = createResults.Count });

                return completedDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi gọi Google API cho Job {JobId}", jobId);
                await _db.OcrJobs.Where(j => j.Id == jobId)
                    .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, OcrStatus.Failed)
                        .SetProperty(j => j.UpdatedAt, DateTime.UtcNow));
                ocrJob.Status = OcrStatus.Failed;

                await _hub.Clients.Group($"OcrJob_{jobId}")
                    .SendAsync("OcrCompleted", new { jobId, status = OcrStatus.Failed });
            }

            return MapToResultDto(ocrJob);
        }

        // Hàm phụ để map dữ liệu đồng nhất
        private OcrProcessingResultDto MapToResultDto(OcrJob job)
        {
            return new OcrProcessingResultDto
            {
                JobId = job.Id,
                Status = job.Status,
                DetectedText = job.DetectedText,
                MediaId = (int)(job.MediaId ?? 0),
                ImageUrl = job.Media?.StorageUrl,
                Results = job.Results?.Select(r => new CreateOcrResultDto
                {
                    PageNumber = r.PageNumber ?? 1,
                    WordText = r.WordText,
                    BoundingBox = r.BoundingBox
                }).ToList() ?? new List<CreateOcrResultDto>()
            };
        }
        // OcrProcessingService.cs

        /// <summary>
        /// Tạo 1 OcrJob trống cho cả file PDF
        /// </summary>
        public async Task<OcrProcessingResultDto> CreatePdfJobAsync(
            int userId, int workspaceId, int? projectId, string fileName, int totalPages)
        {
            var jobDto = await _ocrJobService.CreateAsync(new OcrJobCreateDto
            {
                UserId = userId,
                ProjectId = projectId,
                Status = "pending",
                DetectedText = string.Empty,
                // Lưu tên file + tổng số trang để FE biết
                // (thêm field TotalPages, FileName vào OcrJob model nếu chưa có)
            });

            _logger.LogInformation("Tạo PDF Job {JobId} cho file '{FileName}' ({Pages} trang)",
                jobDto.Id, fileName, totalPages);

            return new OcrProcessingResultDto
            {
                JobId = jobDto.Id,
                Status = "pending",
                Results = new List<CreateOcrResultDto>()
            };
        }

        /// <summary>
        /// Upload 1 trang PNG → compress → lưu Azure → gọi Google Vision → lưu kết quả vào Job
        /// Nếu trang đã có trong DB rồi thì bỏ qua (idempotent)
        /// </summary>
        public async Task<object> UploadAndOcrPageAsync(int jobId, int pageNumber, IFormFile image)
        {
            // ── 1. Check xem trang này đã OCR chưa (idempotent) ──────────────────
            bool alreadyDone = await _db.OcrResults
                .AnyAsync(r => r.OcrJobId == jobId && r.PageNumber == pageNumber);

            if (alreadyDone)
            {
                _logger.LogInformation("✅ Cache HIT trang {Page} Job {JobId} — bỏ qua", pageNumber, jobId);
                var cachedResults = await _db.OcrResults
                    .Where(r => r.OcrJobId == jobId && r.PageNumber == pageNumber)
                    .ToListAsync();
                return new { jobId, pageNumber, status = "cached", results = cachedResults };
            }

            // ── 2. Đọc bytes ảnh ─────────────────────────────────────────────────
            byte[] imageBytes;
            await using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            // ── 3. Compress ảnh trước khi upload (giảm bandwidth ~60-70%) ────────
            byte[] compressedBytes = CompressImage(imageBytes);
            _logger.LogInformation("🗜️ Compress trang {Page}: {Before}KB → {After}KB",
                pageNumber, imageBytes.Length / 1024, compressedBytes.Length / 1024);

            // ── 4. Upload lên Azure ───────────────────────────────────────────────
            var fileName = $"job{jobId}_page{pageNumber}_{Guid.NewGuid()}.jpg";
            string blobUrl;
            await using (var stream = new MemoryStream(compressedBytes))
            {
                blobUrl = await _blobService.UploadFileBlobAsync(
                    containerName: "ocr-images",
                    content: stream,
                    contentType: "image/jpeg",
                    fileName: fileName
                );
            }

            // ── 5. Gọi Google Cloud Vision (dùng ảnh gốc để đảm bảo chất lượng OCR) ──
            using var visionCts = new CancellationTokenSource(VisionTimeout);
            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
            var response = await _visionClient.DetectDocumentTextAsync(googleImage);

            var createResults = new List<CreateOcrResultDto>();
            var fullTextBuilder = new StringBuilder();

            if (response?.Text != null)
            {
                foreach (var page in response.Pages)
                    foreach (var block in page.Blocks)
                        foreach (var paragraph in block.Paragraphs)
                            foreach (var word in paragraph.Words)
                            {
                                string wordText = string.Join("", word.Symbols.Select(s => s.Text));
                                fullTextBuilder.Append(wordText).Append(" ");
                                var bboxList = word.BoundingBox.Vertices
                                    .Select(v => new[] { v.X, v.Y }).ToList();
                                createResults.Add(new CreateOcrResultDto
                                {
                                    PageNumber = pageNumber,
                                    WordText = wordText,
                                    BoundingBox = JsonSerializer.Serialize(bboxList)
                                });
                            }
            }

            // ── 6. Lưu kết quả vào DB ─────────────────────────────────────────────
            if (createResults.Any())
                await _ocrJobService.AppendResultsAsync(jobId, createResults);

            await _ocrJobService.AppendDetectedTextAsync(jobId,
                $"[Trang {pageNumber}]\n{fullTextBuilder.ToString().Trim()}\n");

            _logger.LogInformation("✅ OCR xong trang {Page} Job {JobId} — {Count} từ",
                pageNumber, jobId, createResults.Count);

            // ── 7. SignalR: push progress về client ──────────────────────────────
            await _hub.Clients.Group($"OcrJob_{jobId}")
                .SendAsync("OcrPageCompleted", new { jobId, pageNumber, wordCount = createResults.Count });

            return new { jobId, pageNumber, status = OcrStatus.Completed, results = createResults };
        }

        /// <summary>Resize về max 1500px width và encode JPEG Q85 để giảm size upload.</summary>
        private static byte[] CompressImage(byte[] original)
        {
            try
            {
                using var img = new MagickImage(original);
                if (img.Width > MaxImageWidth)
                {
                    var geo = new MagickGeometry(MaxImageWidth, 0) { IgnoreAspectRatio = false };
                    img.Resize(geo);
                }
                img.Format = MagickFormat.Jpeg;
                img.Quality = 85;
                return img.ToByteArray();
            }
            catch
            {
                // Nếu compress lỗi thì dùng ảnh gốc — không làm hỏng luồng OCR
                return original;
            }
        }

        public async Task<OcrProcessingResultDto> ProcessImageAsync(IFormFile image, int userId, int workspaceId, int? projectId, bool saveAnnotated)
        {
            // --- 1. Đọc ảnh vào bộ nhớ ---
            byte[] originalImageBytes;
            await using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                originalImageBytes = ms.ToArray();
            }

            // --- 2. Tải ảnh gốc lên Azure Blob Storage và lưu MediaStore ---
            int originalMediaId;
            string uploadedUrl;
            try
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                await using (var stream = new MemoryStream(originalImageBytes))
                {
                    uploadedUrl = await _blobService.UploadFileBlobAsync(
                        containerName: "ocr-images",
                        content: stream,
                        contentType: image.ContentType,
                        fileName: uniqueFileName
                    );
                }
                _logger.LogInformation("Saved original image to Azure Blob. URL: {Url}", uploadedUrl);

                var originalMedia = new MediaStore
                {
                    OwnerId = userId,
                    WorkspaceId = workspaceId, // Lưu ID Công ty
                    FileName = image.FileName,
                    MimeType = image.ContentType,
                    SizeBytes = image.Length,
                    StorageUrl = uploadedUrl,
                    Sha256 = ComputeSha256(originalImageBytes),
                    CreatedAt = DateTime.UtcNow
                };
                _db.MediaStore.Add(originalMedia);
                await _db.SaveChangesAsync();
                originalMediaId = originalMedia.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload to Azure or save MediaStore record.");
                throw new Exception("Failed to save original image.", ex);
            }

            // --- 3. Tạo OcrJob ---
            var createJobDto = new OcrJobCreateDto
            {
                UserId = userId,
                MediaId = originalMediaId,
                ProjectId = projectId,
                Status = "pending",
                DetectedText = string.Empty
            };
            var jobDto = await _ocrJobService.CreateAsync(createJobDto);
            _logger.LogInformation("Created OcrJob id={Id} linked to MediaId={MediaId}", jobDto.Id, originalMediaId);


            // --- 4. GỌI GOOGLE CLOUD VISION API CHUẨN PRO DEV ---
            _logger.LogInformation("Bắt đầu gửi ảnh lên Google Cloud Vision cho Job {JobId}", jobDto.Id);

            string finalDetectedText = string.Empty;
            var createResults = new List<CreateOcrResultDto>();
             
            try
            {
                using var visionCts = new CancellationTokenSource(VisionTimeout);
                var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(originalImageBytes);
                var response = await _visionClient.DetectDocumentTextAsync(googleImage);

                if (response != null && response.Text != null)
                {
                    var fullTextBuilder = new StringBuilder();

                    // Google Vision phân cấp: Pages -> Blocks -> Paragraphs -> Words
                    foreach (var page in response.Pages)
                    {
                        foreach (var block in page.Blocks)
                        {
                            foreach (var paragraph in block.Paragraphs)
                            {
                                foreach (var word in paragraph.Words)
                                {
                                    // Ghép các ký tự thành 1 từ
                                    string wordText = string.Join("", word.Symbols.Select(s => s.Text));
                                    fullTextBuilder.Append(wordText).Append(" ");

                                    // Lấy tọa độ Bounding Box (4 góc x,y)
                                    var bboxList = word.BoundingBox.Vertices.Select(v => new[] { v.X, v.Y }).ToList();
                                    string bboxJson = JsonSerializer.Serialize(bboxList);

                                    createResults.Add(new CreateOcrResultDto
                                    {
                                        PageNumber = 1,
                                        WordText = wordText,
                                        BoundingBox = bboxJson
                                    });
                                }
                                fullTextBuilder.AppendLine(); // Xuống dòng khi hết 1 đoạn
                            }
                        }
                    }
                    finalDetectedText = fullTextBuilder.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Google Vision API cho Job {JobId}", jobDto.Id);
                await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = OcrStatus.Failed, DetectedText = null });
                throw new Exception("Lỗi khi gọi Google Vision API", ex);
            }

            // --- 5. LƯU KẾT QUẢ VÀO DATABASE ---
            if (createResults.Count > 0)
            {
                await _ocrJobService.AppendResultsAsync(jobDto.Id, createResults);
            }

            await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto
            {
                Status = OcrStatus.Completed,
                DetectedText = finalDetectedText
            });

            _logger.LogInformation("Google Vision nhận diện thành công Job {JobId}!", jobDto.Id);

            // --- 6. TRẢ VỀ KẾT QUẢ CHO FRONTEND ---
            return new OcrProcessingResultDto
            {
                JobId = jobDto.Id,
                Status = "completed",
                DetectedText = finalDetectedText,
                MediaId = originalMediaId,
                ImageUrl = uploadedUrl,
                AnnotatedMediaId = null,
                AnnotatedImageUrl = null,
                Results = createResults
            };
        }

        private async Task<int?> SaveAnnotatedImageAsync(string base64Image, int userId, int workspaceId, int jobId)
        {
            var match = Regex.Match(base64Image, @"data:image\/(?<type>.+?);base64,(?<data>.+)");
            if (!match.Success)
            {
                _logger.LogWarning("Invalid annotated image format received from infer service for job {JobId}", jobId);
                return null;
            }

            try
            {
                var type = match.Groups["type"].Value;
                var b64Data = match.Groups["data"].Value;
                var bytes = Convert.FromBase64String(b64Data);
                var mimeType = $"image/{type}";
                var uniqueFileName = $"annotated_{jobId}_{Guid.NewGuid()}.{type}";

                string uploadedUrl;
                await using (var stream = new MemoryStream(bytes))
                {
                    uploadedUrl = await _blobService.UploadFileBlobAsync(
                        containerName: "ocr-images-annotated", // Container riêng cho ảnh chú thích
                        content: stream,
                        contentType: mimeType,
                        fileName: uniqueFileName
                    );
                }

                var media = new MediaStore
                {
                    OwnerId = userId,
                    WorkspaceId = workspaceId,
                    FileName = uniqueFileName,
                    MimeType = mimeType,
                    SizeBytes = bytes.LongLength,
                    StorageUrl = uploadedUrl,
                    Sha256 = ComputeSha256(bytes),
                    CreatedAt = DateTime.UtcNow
                };
                _db.MediaStore.Add(media);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Saved annotated image to Blob for job {JobId}, MediaId {MediaId}", jobId, media.Id);
                return media.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save annotated image for job {JobId}", jobId);
                return null;
            }
        }

        private static string ComputeSha256(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(data);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
