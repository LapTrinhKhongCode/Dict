using Dict.Data;
using Dict.Hubs;
using Dict.Models;
using Dict.Service.IService;
using ImageMagick;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Dict.DTO.OCR;
using Google.Cloud.Vision.V1;
using Microsoft.Extensions.Configuration;

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
        private static readonly TimeSpan AzureDiTimeout = TimeSpan.FromMinutes(2);
        private const string OcrCachePrefix = "ocr_job_";
        private static readonly TimeSpan OcrCacheTtl = TimeSpan.FromMinutes(10);
        // Completed jobs never change — cache much longer
        private static readonly MemoryCacheEntryOptions OcrCompletedCacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromDays(7))
            .SetAbsoluteExpiration(TimeSpan.FromDays(7));
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _pageOcrLocks = new();
        private const string AzureDiProvider = "azure-di";
        private const string AzureDiMimeParam = "ocr-provider";
        private const string StructuredSegmentOpenTag = "[[RAG_STRUCTURED";
        private const string StructuredSegmentCloseTag = "[[/RAG_STRUCTURED]]";

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
        private readonly bool _enableDocumentAiUpload;
        private readonly string _azureDiEndpoint;
        private readonly string _azureDiApiKey;
        private readonly string _azureDiApiVersion;
        private readonly int _azureDiPollIntervalMs;
        private readonly int _azureDiMaxPollAttempts;

        // Thread-local holder for table records from the last Azure DI parse (per-request, not shared)
        [ThreadStatic]
        private static List<AzureDiTableRecord>? _lastParsedTableRecords;

        public OcrProcessingService(
            IHttpClientFactory httpFactory,
            ILogger<OcrProcessingService> logger,
            IOcrJobService ocrJobService,
            ApplicationDbContext db,
            IBlobService blobService,
            ImageAnnotatorClient visionClient,
            IMemoryCache cache,
            IHubContext<NotificationHub> hub,
            IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _ocrJobService = ocrJobService;
            _db = db;
            _blobService = blobService;
            _visionClient = visionClient;
            _cache = cache;
            _hub = hub;
            _enableDocumentAiUpload = configuration.GetValue<bool>("FeatureFlags:EnableDocumentAiUpload");
            _azureDiEndpoint = (configuration["AzureDocumentIntelligence:Endpoint"] ?? string.Empty).Trim().TrimEnd('/');
            _azureDiApiKey = (configuration["AzureDocumentIntelligence:ApiKey"] ?? string.Empty).Trim();
            _azureDiApiVersion = (configuration["AzureDocumentIntelligence:ApiVersion"] ?? "2023-07-31").Trim();
            _azureDiPollIntervalMs = Math.Clamp(configuration.GetValue("AzureDocumentIntelligence:PollIntervalMs", 1000), 250, 5000);
            _azureDiMaxPollAttempts = Math.Clamp(configuration.GetValue("AzureDocumentIntelligence:MaxPollAttempts", 60), 5, 180);
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

        // --- HÀM 0: UPLOAD tài liệu native (PDF/DOCX/PPTX/XLSX/TXT/CSV/MD) — không cần Vision ---
        public async Task<OcrProcessingResultDto> UploadNativeDocAsync(IFormFile file, int userId, int workspaceId, int? projectId)
        {
            byte[] fileBytes;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            string sha256 = ComputeSha256(fileBytes);

            // Detect if PDF is image-only (no native text) → tell FE to call Azure DI
            bool needsAzure = false;
            string ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
            var textPages = ExtractTextPagesFromNativeDocument(file.FileName, file.ContentType, fileBytes);
            if (ext == ".pdf" || string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                needsAzure = !IsNativePdf(textPages);
                if (needsAzure)
                {
                    _logger.LogInformation("📷 PDF {FileName} is image-only — signalling FE to use Azure DI", file.FileName);
                }
            }

            if (!needsAzure && !HasExtractedText(textPages))
                throw new InvalidOperationException("File không có nội dung text để index. Hãy kiểm tra định dạng hoặc dùng OCR thông thường.");

            // Upload to blob
            string uploadedUrl;
            int mediaId;
            try
            {
                var existing = await _db.MediaStore.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Sha256 == sha256 && m.OwnerId == userId);
                if (existing != null)
                {
                    mediaId = existing.Id; uploadedUrl = existing.StorageUrl;
                }
                else
                {
                    var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
                    await using var stream = new MemoryStream(fileBytes);
                    uploadedUrl = await _blobService.UploadFileBlobAsync("ocr-images", stream, file.ContentType, uniqueName);
                    var media = new MediaStore
                    {
                        OwnerId = userId, WorkspaceId = workspaceId, FileName = file.FileName,
                        MimeType = file.ContentType, ProjectId = projectId,
                        SizeBytes = file.Length, StorageUrl = uploadedUrl, Sha256 = sha256, CreatedAt = DateTime.UtcNow
                    };
                    _db.MediaStore.Add(media);
                    await _db.SaveChangesAsync();
                    mediaId = media.Id;
                }
            }
            catch (Exception ex) { throw new Exception("Lỗi upload file.", ex); }

            // If PDF is image-only: create a pending job, return early with needsAzure=true
            // FE will call upload-document-ai with the same jobId to fill in text via Azure DI
            if (needsAzure)
            {
                var pendingJobDto = await _ocrJobService.CreateAsync(new OcrJobCreateDto
                {
                    UserId = userId, MediaId = mediaId, ProjectId = projectId,
                    Status = OcrStatus.Pending, DetectedText = string.Empty
                });

                return new OcrProcessingResultDto
                {
                    JobId = pendingJobDto.Id, Status = OcrStatus.Pending,
                    MediaId = mediaId, ImageUrl = uploadedUrl,
                    Results = new List<CreateOcrResultDto>(),
                    NeedsAzureDocumentAi = true
                };
            }

            // Create job as Completed immediately
            var jobDto = await _ocrJobService.CreateAsync(new OcrJobCreateDto
            {
                UserId = userId, MediaId = mediaId, ProjectId = projectId,
                Status = OcrStatus.Processing, DetectedText = string.Empty
            });

            // Store each page as OcrResult rows (same format as Vision output)
            var fullTextSb = new StringBuilder();
            var results = new List<CreateOcrResultDto>();
            foreach (var (page, text) in textPages)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                // Split into word-level tokens for compatibility with existing RAG indexer
                var words = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    string trimmed = word.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    results.Add(new CreateOcrResultDto
                    {
                        PageNumber = page,
                        WordText = trimmed,
                        BoundingBox = "[]"
                    });
                }
                fullTextSb.AppendLine(text);
            }

            if (results.Any())
                await _ocrJobService.AppendResultsAsync(jobDto.Id, results);

            await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto
            {
                Status = OcrStatus.Completed,
                DetectedText = fullTextSb.ToString()
            });

            // Save document_tables for xlsx/csv (additive — no impact on legacy text flow)
            try
            {
                string docExt = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
                List<AzureDiTableRecord> nativeTableRecords = docExt switch
                {
                    ".xlsx" => ExtractTablesFromXlsx(fileBytes),
                    ".csv" => ExtractTablesFromCsv(fileBytes),
                    ".docx" => ExtractTablesFromDocx(fileBytes),
                    _ => new List<AzureDiTableRecord>()
                };

                foreach (var rec in nativeTableRecords)
                {
                    bool exists = await _db.DocumentTables
                        .AnyAsync(t => t.OcrJobId == jobDto.Id && t.ContentHash == rec.ContentHash);
                    if (exists) continue;
                    _db.DocumentTables.Add(new Dict.Models.DocumentTable
                    {
                        OcrJobId = jobDto.Id, PageNumber = rec.PageNumber,
                        TableIndex = rec.TableIndex, SectionTitle = rec.SectionTitle,
                        RowCount = rec.RowCount, ColumnCount = rec.ColumnCount,
                        HeadersJson = rec.HeadersJson, CellsJson = rec.CellsJson,
                        ContentHash = rec.ContentHash, CreatedAt = DateTime.UtcNow
                    });
                }
                if (nativeTableRecords.Count > 0)
                {
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("📊 Saved {Count} document_tables (native) for Job {JobId}", nativeTableRecords.Count, jobDto.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to save native document_tables for Job {JobId}, skipping", jobDto.Id);
            }

            _logger.LogInformation("✅ UploadNativeDocAsync: Job {JobId} — {Pages} trang, {Words} dòng (native text)",
                jobDto.Id, textPages.Count, results.Count);

            return new OcrProcessingResultDto
            {
                JobId = jobDto.Id, Status = OcrStatus.Completed,
                MediaId = mediaId, ImageUrl = uploadedUrl, Results = results,
                NeedsAzureDocumentAi = needsAzure ? true : null
            };
        }

        // --- HÀM 0.1: UPLOAD tài liệu qua Azure Document Intelligence (luồng mới, tách biệt) ---
        public async Task<OcrProcessingResultDto> UploadDocumentAiAsync(IFormFile file, int userId, int workspaceId, int? projectId)
        {
            if (!_enableDocumentAiUpload)
                throw new InvalidOperationException("Luồng Document AI chưa bật. Bật FeatureFlags:EnableDocumentAiUpload để sử dụng.");

            if (string.IsNullOrWhiteSpace(_azureDiEndpoint) || string.IsNullOrWhiteSpace(_azureDiApiKey))
                throw new InvalidOperationException("Thiếu cấu hình Azure Document Intelligence (Endpoint/ApiKey).");

            byte[] fileBytes;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            string safeContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            string sha256 = ComputeSha256(fileBytes);
            string? selectivePages = null;
            if (IsPdfDocument(file.FileName, safeContentType))
            {
                var nativePdfPages = ExtractTextFromNativePdf(fileBytes);
                if (nativePdfPages.Count > 0)
                {
                    var candidatePages = DetectLikelyTableOrVisualPages(nativePdfPages);
                    if (candidatePages.Count > 0 && candidatePages.Count < nativePdfPages.Count)
                    {
                        selectivePages = BuildAzurePagesQuery(candidatePages);
                    }

                    _logger.LogInformation(
                        "🧭 Azure DI selective-pages detect: total={TotalPages}, candidates={CandidatePages}, pages={Pages}",
                        nativePdfPages.Count,
                        candidatePages.Count,
                        string.IsNullOrWhiteSpace(selectivePages) ? "all" : selectivePages
                    );
                }
            }

            var pages = await AnalyzeWithAzureDocumentIntelligenceAsync(fileBytes, safeContentType, selectivePages);
            if (!pages.Any(page => page.Results.Count > 0 || page.StructuredSegments.Count > 0))
                throw new InvalidOperationException("Document AI không trích xuất được text. Hãy kiểm tra lại file đầu vào.");

            // Upload to blob
            string uploadedUrl;
            int mediaId;
            try
            {
                var existing = await _db.MediaStore
                    .FirstOrDefaultAsync(m => m.Sha256 == sha256 && m.OwnerId == userId);
                if (existing != null)
                {
                    mediaId = existing.Id;
                    uploadedUrl = existing.StorageUrl;
                    string normalizedMime = AppendMimeProviderTag(existing.MimeType, AzureDiProvider);
                    if (!string.Equals(existing.MimeType, normalizedMime, StringComparison.OrdinalIgnoreCase))
                    {
                        existing.MimeType = normalizedMime;
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
                    await using var stream = new MemoryStream(fileBytes);
                    uploadedUrl = await _blobService.UploadFileBlobAsync("ocr-images", stream, safeContentType, uniqueName);
                    var media = new MediaStore
                    {
                        OwnerId = userId,
                        WorkspaceId = workspaceId,
                        FileName = file.FileName,
                        MimeType = AppendMimeProviderTag(safeContentType, AzureDiProvider),
                        ProjectId = projectId,
                        SizeBytes = file.Length,
                        StorageUrl = uploadedUrl,
                        Sha256 = sha256,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.MediaStore.Add(media);
                    await _db.SaveChangesAsync();
                    mediaId = media.Id;
                }
            }
            catch (Exception ex) { throw new Exception("Lỗi upload file.", ex); }

            // Reuse existing pending job for this media if one exists (avoids creating duplicate job)
            var existingPendingJob = await _db.OcrJobs
                .FirstOrDefaultAsync(j => j.MediaId == mediaId && j.UserId == userId
                    && (j.Status == OcrStatus.Pending || j.Status == OcrStatus.Processing));

            int jobId;
            if (existingPendingJob != null)
            {
                existingPendingJob.Status = OcrStatus.Processing;
                await _db.SaveChangesAsync();
                jobId = existingPendingJob.Id;
            }
            else
            {
                var newJobDto = await _ocrJobService.CreateAsync(new OcrJobCreateDto
                {
                    UserId = userId,
                    MediaId = mediaId,
                    ProjectId = projectId,
                    Status = OcrStatus.Processing,
                    DetectedText = string.Empty
                });
                jobId = newJobDto.Id;
            }

            var results = new List<CreateOcrResultDto>();
            var fullTextSb = new StringBuilder();
            foreach (var page in pages.OrderBy(item => item.PageNumber))
            {
                if (page.Results.Count > 0)
                    results.AddRange(page.Results);

                if (!string.IsNullOrWhiteSpace(page.PageText))
                {
                    fullTextSb.AppendLine($"[Trang {page.PageNumber}]");
                    fullTextSb.AppendLine(page.PageText.Trim());
                }

                foreach (var segment in page.StructuredSegments)
                {
                    AppendStructuredSegment(fullTextSb, page.PageNumber, segment);
                }
            }

            if (results.Any())
                await _ocrJobService.AppendResultsAsync(jobId, results);

            await _ocrJobService.UpdateStatusAsync(jobId, new OcrJobUpdateStatusDto
            {
                Status = OcrStatus.Completed,
                DetectedText = fullTextSb.ToString()
            });

            // Save structured document_tables to DB (additive — no impact on legacy flow)
            var tableRecords = _lastParsedTableRecords ?? new List<AzureDiTableRecord>();
            _lastParsedTableRecords = null;
            if (tableRecords.Count > 0)
            {
                try
                {
                    foreach (var rec in tableRecords)
                    {
                        bool alreadyExists = await _db.DocumentTables
                            .AnyAsync(t => t.OcrJobId == jobId && t.ContentHash == rec.ContentHash);
                        if (alreadyExists) continue;

                        _db.DocumentTables.Add(new Dict.Models.DocumentTable
                        {
                            OcrJobId = jobId,
                            PageNumber = rec.PageNumber,
                            TableIndex = rec.TableIndex,
                            SectionTitle = rec.SectionTitle,
                            Caption = rec.Caption,
                            RowCount = rec.RowCount,
                            ColumnCount = rec.ColumnCount,
                            HeadersJson = rec.HeadersJson,
                            CellsJson = rec.CellsJson,
                            ContentHash = rec.ContentHash,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("📊 Saved {Count} document_tables for Job {JobId}", tableRecords.Count, jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Failed to save document_tables for Job {JobId}, skipping (non-fatal)", jobId);
                }
            }

            int structuredSegmentCount = pages.Sum(item => item.StructuredSegments.Count);
            _logger.LogInformation("✅ UploadDocumentAiAsync: Job {JobId} — {Pages} trang, {Words} token, {StructuredCount} structured segment (Azure DI)",
                jobId, pages.Count, results.Count, structuredSegmentCount);

            return new OcrProcessingResultDto
            {
                JobId = jobId,
                Status = OcrStatus.Completed,
                MediaId = mediaId,
                ImageUrl = uploadedUrl,
                Results = results
            };
        }

        public async Task<OcrProcessingResultDto> UploadImageOnlyAsync(IFormFile image, int userId, int workspaceId, int? projectId)
        {
            byte[] originalImageBytes;
            await using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                originalImageBytes = ms.ToArray();
            }

            string sha256 = ComputeSha256(originalImageBytes);

            // ── Upload Azure (không chạy Vision nữa) ────────────────────────────
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

                    // PDF: KHÔNG nén. CompressImage (ImageMagick) sẽ phá cấu trúc PDF hoặc
                    // render về ảnh 1 trang (khi VM có Ghostscript) → pdf.js không đọc được,
                    // mất luồng lazy-load nhiều trang. Lưu nguyên bytes + đúng content-type
                    // để pdf.js render trực tiếp và OCR từng trang như luồng cũ.
                    var fileExt = Path.GetExtension(image.FileName ?? string.Empty);
                    bool isPdfUpload =
                        (image.ContentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) ?? false)
                        || fileExt.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

                    byte[] bytesToStore;
                    string blobContentType;
                    if (isPdfUpload)
                    {
                        bytesToStore = originalImageBytes;
                        blobContentType = "application/pdf";
                    }
                    else
                    {
                        bytesToStore = CompressImage(originalImageBytes);
                        blobContentType = "image/jpeg";
                    }

                    await using (var stream = new MemoryStream(bytesToStore))
                    {
                        uploadedUrl = await _blobService.UploadFileBlobAsync(
                            containerName: "ocr-images",
                            content: stream,
                            contentType: blobContentType,
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

            // ── Tạo job với status Pending — FE sẽ trigger OCR khi render trang ─
            var jobDto = await _ocrJobService.CreateAsync(new OcrJobCreateDto
            {
                UserId = userId,
                MediaId = originalMediaId,
                ProjectId = projectId,
                Status = OcrStatus.Pending,
                DetectedText = string.Empty
            });

            _logger.LogInformation("✅ UploadImageOnlyAsync: Tạo Job {JobId} (Pending) — chờ FE trigger OCR", jobDto.Id);

            return new OcrProcessingResultDto
            {
                JobId = jobDto.Id,
                Status = OcrStatus.Pending,
                MediaId = originalMediaId,
                ImageUrl = uploadedUrl,
                Results = new List<CreateOcrResultDto>()
            };
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
                .AsNoTracking()
                .Include(j => j.Media)
                .Include(j => j.Results)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (ocrJob == null) return null;

            // 2. Completed → cache và trả về
            if (ocrJob.Status == OcrStatus.Completed && ocrJob.Results != null && ocrJob.Results.Any())
            {
                _logger.LogInformation("✅ DB Cache HIT — Job {JobId}", jobId);
                var dto = MapToResultDto(ocrJob);
                _cache.Set(cacheKey, dto, OcrCompletedCacheOptions);
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

                // Cache kết quả completed — dùng long TTL vì completed jobs không thay đổi
                var completedDto = MapToResultDto(ocrJob);
                _cache.Set(cacheKey, completedDto, OcrCompletedCacheOptions);

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
            string lockKey = $"{jobId}:{pageNumber}";
            var pageLock = _pageOcrLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
            await pageLock.WaitAsync();
            try
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

                // Double-check trước khi ghi DB để tránh duplicate khi nhiều node xử lý đồng thời.
                bool becameDone = await _db.OcrResults
                    .AnyAsync(r => r.OcrJobId == jobId && r.PageNumber == pageNumber);
                if (becameDone)
                {
                    var cachedResults = await _db.OcrResults
                        .Where(r => r.OcrJobId == jobId && r.PageNumber == pageNumber)
                        .ToListAsync();
                    return new { jobId, pageNumber, status = "cached", results = cachedResults };
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
            finally
            {
                pageLock.Release();
            }
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


        private sealed class AzureDiPageResult
        {
            public int PageNumber { get; init; }
            public string PageText { get; init; } = string.Empty;
            public List<CreateOcrResultDto> Results { get; init; } = new();
            public List<AzureDiStructuredSegment> StructuredSegments { get; init; } = new();
        }

        private sealed class AzureDiStructuredSegment
        {
            public string ContentType { get; init; } = "text";
            public string Text { get; init; } = string.Empty;
        }

        /// <summary>Rich table record parsed from Azure DI — saved to document_tables.</summary>
        internal sealed class AzureDiTableRecord
        {
            public int PageNumber { get; set; }
            public int TableIndex { get; set; }
            public string? SectionTitle { get; set; }
            public string? Caption { get; set; }
            public int RowCount { get; set; }
            public int ColumnCount { get; set; }
            public string HeadersJson { get; set; } = "[]";
            public string CellsJson { get; set; } = "[]";
            public string ContentHash { get; set; } = string.Empty;
        }

        private async Task<List<AzureDiPageResult>> AnalyzeWithAzureDocumentIntelligenceAsync(byte[] fileBytes, string? contentType, string? pagesQuery = null)
        {
            using var cts = new CancellationTokenSource(AzureDiTimeout);
            using var httpClient = _httpFactory.CreateClient();

            string analyzeUrl = $"{_azureDiEndpoint}/formrecognizer/documentModels/prebuilt-layout:analyze?api-version={_azureDiApiVersion}";
            if (!string.IsNullOrWhiteSpace(pagesQuery))
            {
                analyzeUrl += $"&pages={Uri.EscapeDataString(pagesQuery)}";
            }
            using var analyzeRequest = new HttpRequestMessage(HttpMethod.Post, analyzeUrl);
            analyzeRequest.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _azureDiApiKey);
            analyzeRequest.Content = new ByteArrayContent(fileBytes);
            analyzeRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            using var analyzeResponse = await httpClient.SendAsync(analyzeRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!analyzeResponse.IsSuccessStatusCode)
            {
                string errorBody = await analyzeResponse.Content.ReadAsStringAsync(cts.Token);
                throw new InvalidOperationException($"Azure Document Intelligence analyze thất bại ({(int)analyzeResponse.StatusCode}): {errorBody}");
            }

            string operationLocation = analyzeResponse.Headers.TryGetValues("Operation-Location", out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(operationLocation))
                throw new InvalidOperationException("Azure Document Intelligence không trả về Operation-Location.");

            for (int attempt = 0; attempt < _azureDiMaxPollAttempts; attempt++)
            {
                await Task.Delay(_azureDiPollIntervalMs, cts.Token);
                using var pollRequest = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                pollRequest.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _azureDiApiKey);
                using var pollResponse = await httpClient.SendAsync(pollRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                string pollBody = await pollResponse.Content.ReadAsStringAsync(cts.Token);
                if (!pollResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Azure Document Intelligence poll lỗi ({(int)pollResponse.StatusCode}): {pollBody}");

                using var pollJson = JsonDocument.Parse(pollBody);
                string status = pollJson.RootElement.TryGetProperty("status", out var statusNode)
                    ? (statusNode.GetString() ?? string.Empty).Trim().ToLowerInvariant()
                    : string.Empty;

                if (status == "succeeded")
                {
                    var parsedPages = ParseAzureDiPages(pollJson.RootElement);
                    // Store table records list as out-of-band field via a separate call on the same root
                    var tableRecords = ParseAzureDiTableRecords(pollJson.RootElement);
                    // Attach table records to first page as a carrier (parsed separately in UploadDocumentAiAsync)
                    _lastParsedTableRecords = tableRecords;
                    return parsedPages;
                }
                if (status is "failed" or "cancelled")
                    throw new InvalidOperationException($"Azure Document Intelligence xử lý thất bại: {pollBody}");
            }

            throw new TimeoutException("Azure Document Intelligence xử lý quá thời gian chờ.");
        }

        private static List<AzureDiPageResult> ParseAzureDiPages(JsonElement root)
        {
            var pages = new List<AzureDiPageResult>();
            if (!root.TryGetProperty("analyzeResult", out var analyzeResult))
                return pages;
            if (!analyzeResult.TryGetProperty("pages", out var pagesNode) || pagesNode.ValueKind != JsonValueKind.Array)
                return pages;

            var pageMap = new Dictionary<int, AzureDiPageResult>();

            foreach (var pageNode in pagesNode.EnumerateArray())
            {
                int pageNumber = pageNode.TryGetProperty("pageNumber", out var pageNumberNode) && pageNumberNode.TryGetInt32(out int p)
                    ? p
                    : 1;
                var results = new List<CreateOcrResultDto>();
                var pageTextBuilder = new StringBuilder();

                if (pageNode.TryGetProperty("words", out var wordsNode) && wordsNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var wordNode in wordsNode.EnumerateArray())
                    {
                        string text = wordNode.TryGetProperty("content", out var textNode) ? (textNode.GetString() ?? string.Empty).Trim() : string.Empty;
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        string bbox = wordNode.TryGetProperty("polygon", out var polygonNode)
                            ? SerializePolygonToBoundingBox(polygonNode)
                            : "[]";

                        results.Add(new CreateOcrResultDto
                        {
                            PageNumber = pageNumber,
                            WordText = text,
                            BoundingBox = bbox
                        });
                        pageTextBuilder.Append(text).Append(' ');
                    }
                }

                if (results.Count == 0 && pageNode.TryGetProperty("lines", out var linesNode) && linesNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var lineNode in linesNode.EnumerateArray())
                    {
                        string text = lineNode.TryGetProperty("content", out var textNode) ? (textNode.GetString() ?? string.Empty).Trim() : string.Empty;
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        string bbox = lineNode.TryGetProperty("polygon", out var polygonNode)
                            ? SerializePolygonToBoundingBox(polygonNode)
                            : "[]";

                        results.Add(new CreateOcrResultDto
                        {
                            PageNumber = pageNumber,
                            WordText = text,
                            BoundingBox = bbox
                        });
                        pageTextBuilder.AppendLine(text);
                    }
                }

                var pageResult = new AzureDiPageResult
                {
                    PageNumber = pageNumber,
                    PageText = pageTextBuilder.ToString().Trim(),
                    Results = results
                };
                pages.Add(pageResult);
                pageMap[pageNumber] = pageResult;
            }

            // Build paragraph lookup: page → ordered list of (y-offset, content) for section heading detection
            var paragraphsByPage = BuildParagraphsByPage(analyzeResult);

            if (analyzeResult.TryGetProperty("tables", out var tablesNode) && tablesNode.ValueKind == JsonValueKind.Array)
            {
                int tableOrdinal = 1;
                foreach (var tableNode in tablesNode.EnumerateArray())
                {
                    string tableText = BuildTableStructuredText(tableNode, tableOrdinal++);
                    if (string.IsNullOrWhiteSpace(tableText))
                        continue;

                    int pageNumber = ResolveStructuredPageNumber(tableNode);
                    string? sectionTitle = FindNearestHeading(tableNode, pageNumber, paragraphsByPage);
                    if (!string.IsNullOrWhiteSpace(sectionTitle))
                    {
                        tableText = $"Section: {sectionTitle}\n{tableText}";
                    }

                    AddStructuredSegment(pageMap, pages, pageNumber, "table", tableText);
                }
            }

            if (analyzeResult.TryGetProperty("figures", out var figuresNode) && figuresNode.ValueKind == JsonValueKind.Array)
            {
                int figureOrdinal = 1;
                foreach (var figureNode in figuresNode.EnumerateArray())
                {
                    string figureText = BuildFigureStructuredText(figureNode, figureOrdinal++);
                    if (string.IsNullOrWhiteSpace(figureText))
                        continue;

                    int pageNumber = ResolveStructuredPageNumber(figureNode);
                    AddStructuredSegment(pageMap, pages, pageNumber, "figure", figureText);
                }
            }

            return pages.OrderBy(item => item.PageNumber).ToList();
        }

        /// <summary>Parse table nodes into rich AzureDiTableRecord list for DB storage.</summary>
        internal static List<AzureDiTableRecord> ParseAzureDiTableRecords(JsonElement root)
        {
            var records = new List<AzureDiTableRecord>();
            if (!root.TryGetProperty("analyzeResult", out var analyzeResult))
                return records;
            if (!analyzeResult.TryGetProperty("tables", out var tablesNode) || tablesNode.ValueKind != JsonValueKind.Array)
                return records;

            var paragraphsByPage = BuildParagraphsByPage(analyzeResult);
            int tableIndex = 0;

            foreach (var tableNode in tablesNode.EnumerateArray())
            {
                int rowCount = tableNode.TryGetProperty("rowCount", out var rc) && rc.TryGetInt32(out int r) ? r : 0;
                int colCount = tableNode.TryGetProperty("columnCount", out var cc) && cc.TryGetInt32(out int c) ? c : 0;
                int pageNumber = ResolveStructuredPageNumber(tableNode);

                string? caption = null;
                if (tableNode.TryGetProperty("caption", out var captionNode) &&
                    captionNode.TryGetProperty("content", out var captionContent))
                    caption = captionContent.GetString()?.Trim();

                string? sectionTitle = FindNearestHeading(tableNode, pageNumber, paragraphsByPage);

                // Build cells list
                var cellList = new List<object>();
                var headerTexts = new List<string>();

                if (tableNode.TryGetProperty("cells", out var cellsNode) && cellsNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cellNode in cellsNode.EnumerateArray())
                    {
                        string content = cellNode.TryGetProperty("content", out var ct) ? ct.GetString()?.Trim() ?? string.Empty : string.Empty;
                        int rowIdx = cellNode.TryGetProperty("rowIndex", out var ri) && ri.TryGetInt32(out int rv) ? rv : 0;
                        int colIdx = cellNode.TryGetProperty("columnIndex", out var ci) && ci.TryGetInt32(out int cv) ? cv : 0;
                        int rowSpan = cellNode.TryGetProperty("rowSpan", out var rs) && rs.TryGetInt32(out int rsv) ? rsv : 1;
                        int colSpan = cellNode.TryGetProperty("columnSpan", out var cs) && cs.TryGetInt32(out int csv) ? csv : 1;
                        string kind = cellNode.TryGetProperty("kind", out var kv) ? kv.GetString() ?? "content" : "content";

                        cellList.Add(new { row = rowIdx, col = colIdx, content, rowSpan, colSpan, kind });

                        if ((kind == "columnHeader" || rowIdx == 0) && !string.IsNullOrWhiteSpace(content))
                            headerTexts.Add(content);
                    }
                }

                string cellsJson = JsonSerializer.Serialize(cellList);
                string headersJson = JsonSerializer.Serialize(headerTexts.Distinct().ToList());
                string contentHash = ComputeSha256(System.Text.Encoding.UTF8.GetBytes(cellsJson)).Substring(0, 32);

                records.Add(new AzureDiTableRecord
                {
                    PageNumber = pageNumber > 0 ? pageNumber : 1,
                    TableIndex = tableIndex++,
                    SectionTitle = sectionTitle,
                    Caption = caption,
                    RowCount = rowCount,
                    ColumnCount = colCount,
                    HeadersJson = headersJson,
                    CellsJson = cellsJson,
                    ContentHash = contentHash
                });
            }

            return records;
        }

        private static Dictionary<int, List<(double Y, string Text)>> BuildParagraphsByPage(JsonElement analyzeResult)
        {
            var result = new Dictionary<int, List<(double Y, string Text)>>();
            if (!analyzeResult.TryGetProperty("paragraphs", out var paragraphsNode) || paragraphsNode.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var para in paragraphsNode.EnumerateArray())
            {
                string content = para.TryGetProperty("content", out var ct) ? ct.GetString()?.Trim() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(content) || content.Length < 3) continue;

                int page = ReadPageFromBoundingRegions(para);
                if (page <= 0) page = 1;

                double y = 0;
                if (para.TryGetProperty("boundingRegions", out var brs) && brs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var br in brs.EnumerateArray())
                    {
                        if (br.TryGetProperty("polygon", out var poly) && poly.ValueKind == JsonValueKind.Array)
                        {
                            var coords = poly.EnumerateArray().ToList();
                            if (coords.Count >= 2 && coords[1].TryGetDouble(out double yVal))
                                y = yVal;
                            break;
                        }
                    }
                }

                if (!result.ContainsKey(page))
                    result[page] = new List<(double, string)>();
                result[page].Add((y, content));
            }

            // Sort by Y on each page
            foreach (var key in result.Keys.ToList())
                result[key] = result[key].OrderBy(item => item.Y).ToList();

            return result;
        }

        private static string? FindNearestHeading(
            JsonElement tableNode,
            int tablePageNumber,
            Dictionary<int, List<(double Y, string Text)>> paragraphsByPage)
        {
            if (!paragraphsByPage.TryGetValue(tablePageNumber, out var paras) || paras.Count == 0)
                return null;

            // Get approximate Y top of table
            double tableY = double.MaxValue;
            if (tableNode.TryGetProperty("boundingRegions", out var brs) && brs.ValueKind == JsonValueKind.Array)
            {
                foreach (var br in brs.EnumerateArray())
                {
                    if (br.TryGetProperty("polygon", out var poly) && poly.ValueKind == JsonValueKind.Array)
                    {
                        var coords = poly.EnumerateArray().ToList();
                        if (coords.Count >= 2 && coords[1].TryGetDouble(out double yVal))
                        {
                            tableY = yVal;
                            break;
                        }
                    }
                }
            }

            if (tableY == double.MaxValue) return null;

            // Find paragraph just above the table (closest Y that is smaller than tableY)
            string? candidate = null;
            double bestDist = double.MaxValue;
            foreach (var (y, text) in paras)
            {
                if (y >= tableY) continue;
                double dist = tableY - y;
                if (dist < bestDist && dist < 2.0) // within ~2 inches
                {
                    bestDist = dist;
                    candidate = text;
                }
            }

            return candidate != null && candidate.Length <= 200 ? candidate : null;
        }

        private static void AddStructuredSegment(
            IDictionary<int, AzureDiPageResult> pageMap,
            IList<AzureDiPageResult> pages,
            int pageNumber,
            string contentType,
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            int safePage = pageNumber > 0 ? pageNumber : 1;
            if (!pageMap.TryGetValue(safePage, out var page))
            {
                page = new AzureDiPageResult
                {
                    PageNumber = safePage,
                    PageText = string.Empty,
                    Results = new List<CreateOcrResultDto>(),
                    StructuredSegments = new List<AzureDiStructuredSegment>()
                };
                pageMap[safePage] = page;
                pages.Add(page);
            }

            page.StructuredSegments.Add(new AzureDiStructuredSegment
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "text" : contentType.Trim().ToLowerInvariant(),
                Text = text
            });
        }

        private static int ResolveStructuredPageNumber(JsonElement node)
        {
            int pageFromNode = ReadPageFromBoundingRegions(node);
            if (pageFromNode > 0)
                return pageFromNode;

            if (node.TryGetProperty("cells", out var cellsNode) && cellsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var cellNode in cellsNode.EnumerateArray())
                {
                    int pageFromCell = ReadPageFromBoundingRegions(cellNode);
                    if (pageFromCell > 0)
                        return pageFromCell;
                }
            }

            if (node.TryGetProperty("caption", out var captionNode))
            {
                int pageFromCaption = ReadPageFromBoundingRegions(captionNode);
                if (pageFromCaption > 0)
                    return pageFromCaption;
            }

            return 1;
        }

        private static int ReadPageFromBoundingRegions(JsonElement node)
        {
            if (!node.TryGetProperty("boundingRegions", out var boundingRegions) || boundingRegions.ValueKind != JsonValueKind.Array)
                return 0;

            foreach (var region in boundingRegions.EnumerateArray())
            {
                if (region.TryGetProperty("pageNumber", out var pageNode) && pageNode.TryGetInt32(out int page) && page > 0)
                    return page;
            }

            return 0;
        }

        private static string BuildTableStructuredText(JsonElement tableNode, int tableOrdinal)
        {
            if (!tableNode.TryGetProperty("cells", out var cellsNode) || cellsNode.ValueKind != JsonValueKind.Array)
                return string.Empty;

            int rowCount = tableNode.TryGetProperty("rowCount", out var rowCountNode) && rowCountNode.TryGetInt32(out int r) ? r : 0;
            int colCount = tableNode.TryGetProperty("columnCount", out var colCountNode) && colCountNode.TryGetInt32(out int c) ? c : 0;
            var rows = new SortedDictionary<int, SortedDictionary<int, string>>();

            foreach (var cellNode in cellsNode.EnumerateArray())
            {
                if (!cellNode.TryGetProperty("content", out var contentNode))
                    continue;
                string cellText = contentNode.GetString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(cellText))
                    continue;

                int rowIndex = cellNode.TryGetProperty("rowIndex", out var rowIndexNode) && rowIndexNode.TryGetInt32(out int row) ? row : 0;
                int colIndex = cellNode.TryGetProperty("columnIndex", out var colIndexNode) && colIndexNode.TryGetInt32(out int col) ? col : 0;
                if (!rows.TryGetValue(rowIndex, out var rowCells))
                {
                    rowCells = new SortedDictionary<int, string>();
                    rows[rowIndex] = rowCells;
                }

                if (!rowCells.TryGetValue(colIndex, out var existing))
                    rowCells[colIndex] = cellText;
                else
                    rowCells[colIndex] = $"{existing} {cellText}".Trim();
            }

            if (rows.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            string shape = rowCount > 0 && colCount > 0 ? $" ({rowCount}x{colCount})" : string.Empty;
            sb.AppendLine($"Bảng {tableOrdinal}{shape}:");
            foreach (var row in rows)
            {
                string line = string.Join(" | ", row.Value.OrderBy(item => item.Key).Select(item => item.Value.Trim()));
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine(line);
            }

            return TrimStructuredSegmentText(sb.ToString(), 2800);
        }

        private static string BuildFigureStructuredText(JsonElement figureNode, int figureOrdinal)
        {
            var parts = new List<string>();
            if (figureNode.TryGetProperty("caption", out var captionNode) &&
                captionNode.TryGetProperty("content", out var captionTextNode))
            {
                string caption = captionTextNode.GetString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(caption))
                    parts.Add($"Chú thích: {caption}");
            }

            if (figureNode.TryGetProperty("content", out var contentNode))
            {
                string content = contentNode.GetString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(content))
                    parts.Add($"Nội dung: {content}");
            }

            if (parts.Count == 0)
                return string.Empty;

            return TrimStructuredSegmentText($"Hình {figureOrdinal}: {string.Join(" ", parts)}", 1800);
        }

        private static string TrimStructuredSegmentText(string text, int maxChars)
        {
            string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Trim();
            if (normalized.Length <= maxChars)
                return normalized;
            return normalized[..maxChars].Trim();
        }

        private static void AppendStructuredSegment(StringBuilder buffer, int pageNumber, AzureDiStructuredSegment segment)
        {
            if (segment == null || string.IsNullOrWhiteSpace(segment.Text))
                return;

            string contentType = string.IsNullOrWhiteSpace(segment.ContentType) ? "text" : segment.ContentType.Trim().ToLowerInvariant();
            buffer.AppendLine($"{StructuredSegmentOpenTag} page={pageNumber} type={contentType}]]");
            buffer.AppendLine(segment.Text.Trim());
            buffer.AppendLine(StructuredSegmentCloseTag);
        }

        private static string SerializePolygonToBoundingBox(JsonElement polygonNode)
        {
            if (polygonNode.ValueKind != JsonValueKind.Array)
                return "[]";

            var points = new List<int[]>();
            double? pendingX = null;
            foreach (var coordinate in polygonNode.EnumerateArray())
            {
                if (coordinate.ValueKind != JsonValueKind.Number)
                    continue;
                double value = coordinate.GetDouble();
                if (!pendingX.HasValue)
                {
                    pendingX = value;
                    continue;
                }

                points.Add(new[] { (int)Math.Round(pendingX.Value), (int)Math.Round(value) });
                pendingX = null;
            }

            return points.Count > 0 ? JsonSerializer.Serialize(points) : "[]";
        }

        private static string AppendMimeProviderTag(string? contentType, string provider)
        {
            string baseMime = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
            if (baseMime.Contains($"{AzureDiMimeParam}=", StringComparison.OrdinalIgnoreCase))
                return baseMime;
            return $"{baseMime};{AzureDiMimeParam}={provider}";
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

        // --- Native PDF text extraction (iText 9) ---
        // Returns list of (pageNumber, pageText). Empty list if PDF is image-only.
        public static List<(int Page, string Text)> ExtractTextFromNativePdf(byte[] pdfBytes)
        {
            var pages = new List<(int, string)>();
            try
            {
                using var reader = new iText.Kernel.Pdf.PdfReader(new MemoryStream(pdfBytes));
                using var doc = new iText.Kernel.Pdf.PdfDocument(reader);
                for (int i = 1; i <= doc.GetNumberOfPages(); i++)
                {
                    var page = doc.GetPage(i);
                    var simpleStrategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();
                    var locationStrategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();

                    string simpleText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, simpleStrategy) ?? string.Empty;
                    string locationText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, locationStrategy) ?? string.Empty;

                    int simpleScore = Regex.Replace(simpleText, @"\s+", string.Empty).Length;
                    int locationScore = Regex.Replace(locationText, @"\s+", string.Empty).Length;
                    string selectedText = locationScore > simpleScore ? locationText : simpleText;

                    pages.Add((i, selectedText.Trim()));
                }
            }
            catch { /* not a valid PDF or encrypted */ }
            return pages;
        }

        public static bool IsNativePdf(IEnumerable<(int Page, string Text)> pages, int minCharsPerPage = 30, int minTotalChars = 80)
        {
            var list = pages
                .Select(p => (p.Page, Text: (p.Text ?? string.Empty).Trim()))
                .ToList();
            if (list.Count == 0) return false;

            int textualPages = list.Count(p => p.Text.Length >= minCharsPerPage);
            int totalChars = list.Sum(p => p.Text.Length);
            bool hasDensePage = list.Any(p => p.Text.Length >= minCharsPerPage);

            // Keep the strict half-pages rule for clearly native PDFs, but also accept
            // copyable PDFs that only have meaningful text on a subset of pages.
            return textualPages >= Math.Max(1, list.Count / 2)
                || (hasDensePage && totalChars >= minTotalChars);
        }

        // --- DOCX text extraction (OpenXml) ---
        public static List<(int Page, string Text)> ExtractTextFromDocx(byte[] docxBytes)
        {
            var pages = new List<(int, string)>();
            try
            {
                using var ms = new MemoryStream(docxBytes);
                using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(ms, false);
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null) return pages;

                var sb = new StringBuilder();
                int pageNum = 1;

                foreach (var element in body.Elements())
                {
                    // Page break detection
                    bool hasPageBreak = element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>()
                        .Any(b => b.Type?.Value == DocumentFormat.OpenXml.Wordprocessing.BreakValues.Page);

                    string paraText = string.Concat(
                        element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));

                    if (!string.IsNullOrWhiteSpace(paraText))
                        sb.AppendLine(paraText);

                    if (hasPageBreak && sb.Length > 0)
                    {
                        pages.Add((pageNum++, sb.ToString().Trim()));
                        sb.Clear();
                    }
                }

                if (sb.Length > 0)
                    pages.Add((pageNum, sb.ToString().Trim()));
            }
            catch { /* not a valid docx */ }
            return pages;
        }

        public static List<(int Page, string Text)> ExtractTextFromPptx(byte[] pptxBytes)
        {
            var pages = new List<(int, string)>();
            try
            {
                using var ms = new MemoryStream(pptxBytes);
                using var presentation = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(ms, false);
                var presentationPart = presentation.PresentationPart;
                var slideIds = presentationPart?.Presentation?.SlideIdList?.ChildElements;
                if (presentationPart == null || slideIds == null) return pages;

                int slideNumber = 1;
                foreach (var slideId in slideIds)
                {
                    if (slideId is not DocumentFormat.OpenXml.Presentation.SlideId typedSlideId) continue;
                    var slidePart = (DocumentFormat.OpenXml.Packaging.SlidePart)presentationPart.GetPartById(typedSlideId.RelationshipId);
                    string text = string.Join(
                        "\n",
                        slidePart.Slide
                            .Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                            .Select(t => t.Text)
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                    ).Trim();

                    pages.Add((slideNumber++, text));
                }
            }
            catch { /* not a valid pptx */ }
            return pages;
        }

        public static List<(int Page, string Text)> ExtractTextFromXlsx(byte[] xlsxBytes)
        {
            var pages = new List<(int, string)>();
            try
            {
                using var ms = new MemoryStream(xlsxBytes);
                using var spreadsheet = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(ms, false);
                var workbookPart = spreadsheet.WorkbookPart;
                if (workbookPart?.Workbook?.Sheets == null) return pages;

                int sheetNumber = 1;
                foreach (var sheet in workbookPart.Workbook.Sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
                {
                    var worksheetPart = workbookPart.GetPartById(sheet.Id!) as DocumentFormat.OpenXml.Packaging.WorksheetPart;
                    if (worksheetPart?.Worksheet == null) continue;

                    var rowTexts = new List<string>();
                    foreach (var row in worksheetPart.Worksheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>())
                    {
                        var values = row
                            .Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>()
                            .Select(cell => ReadSpreadsheetCellText(cell, workbookPart))
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .ToList();
                        if (values.Count > 0)
                            rowTexts.Add(string.Join(" | ", values));
                    }

                    string sheetText = string.Join("\n", rowTexts).Trim();
                    pages.Add((sheetNumber++, sheetText));
                }
            }
            catch { /* not a valid xlsx */ }
            return pages;
        }

        public static List<(int Page, string Text)> ExtractTextFromPlainText(byte[] textBytes)
        {
            var pages = new List<(int, string)>();
            try
            {
                string text = Encoding.UTF8.GetString(textBytes);
                var normalizedLines = text
                    .Replace("\r\n", "\n")
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                if (normalizedLines.Count == 0) return pages;

                const int linesPerPage = 120;
                int pageNumber = 1;
                for (int i = 0; i < normalizedLines.Count; i += linesPerPage)
                {
                    var pageLines = normalizedLines.Skip(i).Take(linesPerPage);
                    pages.Add((pageNumber++, string.Join("\n", pageLines)));
                }
            }
            catch { /* ignore invalid text encoding */ }
            return pages;
        }

        private static string ReadSpreadsheetCellText(DocumentFormat.OpenXml.Spreadsheet.Cell cell, DocumentFormat.OpenXml.Packaging.WorkbookPart workbookPart)
        {
            string rawValue = cell.CellValue?.Text ?? string.Empty;
            if (cell.DataType?.Value == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString &&
                int.TryParse(rawValue, out int sharedIndex))
            {
                var sharedTable = workbookPart.SharedStringTablePart?.SharedStringTable;
                var sharedItem = sharedTable?.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>().ElementAtOrDefault(sharedIndex);
                return sharedItem?.InnerText?.Trim() ?? string.Empty;
            }

            return rawValue.Trim();
        }

        private static bool IsPdfDocument(string fileName, string? contentType)
        {
            string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            if (extension == ".pdf")
                return true;
            return !string.IsNullOrWhiteSpace(contentType) &&
                   contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static List<int> DetectLikelyTableOrVisualPages(IEnumerable<(int Page, string Text)> pages)
        {
            var selected = new List<int>();
            foreach (var (pageNumber, text) in pages)
            {
                if (pageNumber <= 0) continue;

                if (IsLikelyVisualPageText(text) || IsLikelyTablePageText(text))
                {
                    selected.Add(pageNumber);
                }
            }

            return selected
                .Distinct()
                .OrderBy(page => page)
                .ToList();
        }

        private static bool IsLikelyVisualPageText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            string normalized = text.Replace("\r\n", "\n").Trim();
            var lines = normalized
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            int charCount = normalized.Count(ch => !char.IsWhiteSpace(ch));
            int longLineCount = lines.Count(line => line.Length >= 40);
            return charCount < 120 || (lines.Count <= 4 && longLineCount <= 1);
        }

        private static bool IsLikelyTablePageText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string normalized = text.Replace("\r\n", "\n");
            var lines = normalized
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            if (lines.Count < 4)
                return false;

            int delimiterLines = lines.Count(line => line.Contains('\t') || line.Contains('|'));
            int tableLikeRows = 0;
            int numericHeavyRows = 0;
            int multiSpaceRows = 0;

            foreach (var line in lines)
            {
                var tokens = Regex.Split(line, @"\s+")
                    .Where(token => !string.IsNullOrWhiteSpace(token))
                    .ToList();
                if (tokens.Count == 0) continue;

                int numericTokenCount = tokens.Count(token => Regex.IsMatch(token, @"^[\(\)\[\]\+\-]?\d[\d,\.%\/:-]*$"));
                if (tokens.Count >= 4 && numericTokenCount >= 2)
                    tableLikeRows++;

                if (numericTokenCount >= Math.Max(2, tokens.Count / 2))
                    numericHeavyRows++;

                if (Regex.IsMatch(line, @"\s{2,}"))
                    multiSpaceRows++;
            }

            if (delimiterLines >= 2)
                return true;

            return tableLikeRows >= 3 && (numericHeavyRows >= 3 || multiSpaceRows >= 2);
        }

        private static string BuildAzurePagesQuery(IEnumerable<int> pageNumbers)
        {
            var ordered = pageNumbers
                .Where(page => page > 0)
                .Distinct()
                .OrderBy(page => page)
                .ToList();
            if (ordered.Count == 0) return string.Empty;

            var ranges = new List<string>();
            int start = ordered[0];
            int prev = ordered[0];

            for (int i = 1; i < ordered.Count; i++)
            {
                int current = ordered[i];
                if (current == prev + 1)
                {
                    prev = current;
                    continue;
                }

                ranges.Add(start == prev ? start.ToString() : $"{start}-{prev}");
                start = current;
                prev = current;
            }

            ranges.Add(start == prev ? start.ToString() : $"{start}-{prev}");
            return string.Join(",", ranges);
        }

        private static List<(int Page, string Text)> ExtractTextPagesFromNativeDocument(string fileName, string? contentType, byte[] fileBytes)
        {
            string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

            if (extension == ".docx" || string.Equals(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
                return ExtractTextFromDocx(fileBytes);

            if (extension == ".pptx" || string.Equals(contentType, "application/vnd.openxmlformats-officedocument.presentationml.presentation", StringComparison.OrdinalIgnoreCase))
                return ExtractTextFromPptx(fileBytes);

            if (extension == ".xlsx" || string.Equals(contentType, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase))
                return ExtractTextFromXlsx(fileBytes);

            if (extension == ".txt" || extension == ".csv" || extension == ".md" ||
                string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(contentType, "text/csv", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(contentType, "text/markdown", StringComparison.OrdinalIgnoreCase))
                return ExtractTextFromPlainText(fileBytes);

            // Default fallback: try native PDF extractor.
            return ExtractTextFromNativePdf(fileBytes);
        }

        private static bool HasExtractedText(IEnumerable<(int Page, string Text)> pages)
        {
            return pages.Any(page => !string.IsNullOrWhiteSpace(page.Text));
        }

        /// <summary>
        /// Extract Word DOCX tables as structured DocumentTable records.
        /// Each table in the document becomes one record; heading paragraph just above becomes SectionTitle.
        /// </summary>
        private static List<AzureDiTableRecord> ExtractTablesFromDocx(byte[] docxBytes)
        {
            var tables = new List<AzureDiTableRecord>();
            try
            {
                using var ms = new MemoryStream(docxBytes);
                using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(ms, false);
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null) return tables;

                var bodyElements = body.Elements().ToList();
                int tableIndex = 0;

                for (int elemIdx = 0; elemIdx < bodyElements.Count; elemIdx++)
                {
                    if (bodyElements[elemIdx] is not DocumentFormat.OpenXml.Wordprocessing.Table tableEl)
                        continue;

                    // Find heading just above this table
                    string? sectionTitle = null;
                    for (int prev = elemIdx - 1; prev >= 0 && prev >= elemIdx - 5; prev--)
                    {
                        if (bodyElements[prev] is DocumentFormat.OpenXml.Wordprocessing.Paragraph paraEl)
                        {
                            string paraStyle = paraEl.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
                            string paraText = string.Concat(
                                paraEl.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)).Trim();
                            if (!string.IsNullOrWhiteSpace(paraText))
                            {
                                // Accept any non-empty paragraph near the table (heading or bold caption)
                                sectionTitle = paraText.Length <= 300 ? paraText : paraText[..300];
                                break;
                            }
                        }
                    }

                    var docxRows = tableEl.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().ToList();
                    if (docxRows.Count == 0) { tableIndex++; continue; }

                    var cellList = new List<object>();
                    var headerTexts = new List<string>();
                    int maxCol = 0;

                    for (int rowIdx = 0; rowIdx < docxRows.Count; rowIdx++)
                    {
                        var rowCells = docxRows[rowIdx].Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>().ToList();
                        if (rowCells.Count > maxCol) maxCol = rowCells.Count;

                        for (int colIdx = 0; colIdx < rowCells.Count; colIdx++)
                        {
                            string content = string.Concat(
                                rowCells[colIdx].Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                                    .Select(t => t.Text)).Trim();
                            if (string.IsNullOrWhiteSpace(content)) continue;

                            string kind = rowIdx == 0 ? "columnHeader" : "content";
                            cellList.Add(new { row = rowIdx, col = colIdx, content, rowSpan = 1, colSpan = 1, kind });
                            if (rowIdx == 0) headerTexts.Add(content);
                        }
                    }

                    if (cellList.Count == 0) { tableIndex++; continue; }

                    string cellsJson = JsonSerializer.Serialize(cellList);
                    string headersJson = JsonSerializer.Serialize(headerTexts);
                    string contentHash = ComputeSha256(Encoding.UTF8.GetBytes(cellsJson)).Substring(0, 32);

                    tables.Add(new AzureDiTableRecord
                    {
                        PageNumber = 1,
                        TableIndex = tableIndex,
                        SectionTitle = sectionTitle,
                        Caption = null,
                        RowCount = docxRows.Count,
                        ColumnCount = maxCol,
                        HeadersJson = headersJson,
                        CellsJson = cellsJson,
                        ContentHash = contentHash
                    });

                    tableIndex++;
                }
            }
            catch { /* not a valid docx */ }
            return tables;
        }

        /// <summary>
        /// Extract each worksheet as a structured DocumentTable record for xlsx files.
        /// Sheet name becomes SectionTitle; row 0 becomes column headers.
        /// </summary>
        private static List<AzureDiTableRecord> ExtractTablesFromXlsx(byte[] xlsxBytes)
        {
            var tables = new List<AzureDiTableRecord>();
            try
            {
                using var ms = new MemoryStream(xlsxBytes);
                using var spreadsheet = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(ms, false);
                var workbookPart = spreadsheet.WorkbookPart;
                if (workbookPart?.Workbook?.Sheets == null) return tables;

                int sheetIndex = 0;
                foreach (var sheet in workbookPart.Workbook.Sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
                {
                    string sheetName = sheet.Name?.Value?.Trim() ?? $"Sheet{sheetIndex + 1}";
                    var worksheetPart = workbookPart.GetPartById(sheet.Id!) as DocumentFormat.OpenXml.Packaging.WorksheetPart;
                    if (worksheetPart?.Worksheet == null) { sheetIndex++; continue; }

                    var rowList = worksheetPart.Worksheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
                    if (rowList.Count == 0) { sheetIndex++; continue; }

                    // Determine max column index
                    int maxCol = 0;
                    var allCells = rowList.SelectMany(r =>
                        r.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>()
                         .Select(c => new { Cell = c, Row = (int)(r.RowIndex?.Value ?? 1) - 1 }))
                        .ToList();

                    foreach (var item in allCells)
                    {
                        int col = CellReferenceToColumnIndex(item.Cell.CellReference?.Value ?? string.Empty);
                        if (col > maxCol) maxCol = col;
                    }

                    int rowCount = rowList.Count;
                    int colCount = maxCol + 1;

                    var cellList = new List<object>();
                    var headerTexts = new List<string>();
                    bool headerRowProcessed = false;

                    foreach (var rowEl in rowList)
                    {
                        int rowIdx = (int)(rowEl.RowIndex?.Value ?? 1) - 1;
                        foreach (var cell in rowEl.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>())
                        {
                            int colIdx = CellReferenceToColumnIndex(cell.CellReference?.Value ?? string.Empty);
                            string content = ReadSpreadsheetCellText(cell, workbookPart).Trim();
                            if (string.IsNullOrWhiteSpace(content)) continue;

                            string kind = rowIdx == 0 ? "columnHeader" : "content";
                            cellList.Add(new { row = rowIdx, col = colIdx, content, rowSpan = 1, colSpan = 1, kind });

                            if (rowIdx == 0 && !headerRowProcessed)
                                headerTexts.Add(content);
                        }
                        if (rowIdx == 0) headerRowProcessed = true;
                    }

                    if (cellList.Count == 0) { sheetIndex++; continue; }

                    string cellsJson = JsonSerializer.Serialize(cellList);
                    string headersJson = JsonSerializer.Serialize(headerTexts);
                    string contentHash = ComputeSha256(Encoding.UTF8.GetBytes(cellsJson)).Substring(0, 32);

                    tables.Add(new AzureDiTableRecord
                    {
                        PageNumber = sheetIndex + 1,
                        TableIndex = sheetIndex,
                        SectionTitle = sheetName,
                        Caption = null,
                        RowCount = rowCount,
                        ColumnCount = colCount,
                        HeadersJson = headersJson,
                        CellsJson = cellsJson,
                        ContentHash = contentHash
                    });

                    sheetIndex++;
                }
            }
            catch { /* not a valid xlsx */ }
            return tables;
        }

        /// <summary>Parse CSV as a single table (1 sheet equivalent).</summary>
        private static List<AzureDiTableRecord> ExtractTablesFromCsv(byte[] csvBytes)
        {
            var tables = new List<AzureDiTableRecord>();
            try
            {
                string text = Encoding.UTF8.GetString(csvBytes);
                var lines = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                if (lines.Count == 0) return tables;

                var cellList = new List<object>();
                var headerTexts = new List<string>();
                int maxCol = 0;

                for (int rowIdx = 0; rowIdx < lines.Count; rowIdx++)
                {
                    string line = lines[rowIdx];
                    var cols = ParseCsvLine(line);
                    if (cols.Count > maxCol) maxCol = cols.Count;

                    for (int colIdx = 0; colIdx < cols.Count; colIdx++)
                    {
                        string content = cols[colIdx].Trim().Trim('"').Trim();
                        if (string.IsNullOrWhiteSpace(content)) continue;
                        string kind = rowIdx == 0 ? "columnHeader" : "content";
                        cellList.Add(new { row = rowIdx, col = colIdx, content, rowSpan = 1, colSpan = 1, kind });
                        if (rowIdx == 0) headerTexts.Add(content);
                    }
                }

                if (cellList.Count == 0) return tables;

                string cellsJson = JsonSerializer.Serialize(cellList);
                string headersJson = JsonSerializer.Serialize(headerTexts);
                string contentHash = ComputeSha256(Encoding.UTF8.GetBytes(cellsJson)).Substring(0, 32);

                tables.Add(new AzureDiTableRecord
                {
                    PageNumber = 1, TableIndex = 0,
                    SectionTitle = "CSV Data",
                    Caption = null,
                    RowCount = lines.Count, ColumnCount = maxCol,
                    HeadersJson = headersJson, CellsJson = cellsJson,
                    ContentHash = contentHash
                });
            }
            catch { /* invalid csv */ }
            return tables;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes) { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            fields.Add(current.ToString());
            return fields;
        }

        private static int CellReferenceToColumnIndex(string cellRef)
        {
            if (string.IsNullOrWhiteSpace(cellRef)) return 0;
            int col = 0;
            foreach (char c in cellRef)
            {
                if (!char.IsLetter(c)) break;
                col = col * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            }
            return Math.Max(0, col - 1);
        }
    }
}
