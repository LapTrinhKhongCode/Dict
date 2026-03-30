using Dict.Data;
using Dict.Models;
using Dict.Service.IService;
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

        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _cfg;
        private readonly ILogger<OcrProcessingService> _logger;
        private readonly IOcrJobService _ocrJobService;
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private readonly IConfiguration _configuration;

        public OcrProcessingService(
            IHttpClientFactory httpFactory,
            IConfiguration cfg,
            ILogger<OcrProcessingService> logger,
            IOcrJobService ocrJobService,
            ApplicationDbContext db,
            IBlobService blobService,
            IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _cfg = cfg;
            _logger = logger;
            _ocrJobService = ocrJobService;
            _db = db;
            _blobService = blobService;
            _configuration = configuration;
        }
        public async Task<IEnumerable<OcrJobDetailDto>> GetRecentOcrJobsForUserAsync(int userId, int limit = 5)
        {
            var jobs = await _db.OcrJobs
                .AsNoTracking()
                .Where(job => job.UserId == userId)
                // Sắp xếp theo ngày tạo mới nhất
                .OrderByDescending(job => job.CreatedAt)
                // Giới hạn số lượng kết quả
                .Take(limit)
                // Tải kèm dữ liệu liên quan
                .Include(job => job.Media) // Tải ảnh (MediaStore)
                .Include(job => job.Results) // Tải kết quả chi tiết
                                             // Ánh xạ sang DTO
                .Select(job => new OcrJobDetailDto
                {
                    Id = job.Id,
                    Status = job.Status,
                    DetectedText = job.DetectedText,
                    CreatedAt = job.CreatedAt,
                    // Lấy URL từ MediaStore liên quan
                    ImageUrl = job.Media != null ? job.Media.StorageUrl : null,
                    // Ánh xạ danh sách các kết quả con
                    Results = job.Results.Select(result => new OcrResultDto
                    {
                        Id = result.Id,
                        WordText = result.WordText,
                        BoundingBox = result.BoundingBox,
                        Confidence = result.Confidence
                    }).ToList()
                })
                .ToListAsync();

            return jobs;
        }
        // --- HÀM 1: CHỈ UPLOAD LÊN AZURE VÀ TẠO JOB PENDING ---
        public async Task<OcrProcessingResultDto> UploadImageOnlyAsync(IFormFile image, int userId, int workspaceId, int? projectId)
        {
            // Đọc ảnh vào bộ nhớ
            byte[] originalImageBytes;
            await using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                originalImageBytes = ms.ToArray();
            }

            // Tải ảnh gốc lên Azure Blob Storage
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

                var originalMedia = new MediaStore
                {
                    OwnerId = userId,
                    WorkspaceId = workspaceId,
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
                _logger.LogError(ex, "Lỗi lưu Azure Blob.");
                throw new Exception("Lỗi lưu ảnh lên Cloud.", ex);
            }

            // Tạo OcrJob với trạng thái PROCESSING
            var createJobDto = new OcrJobCreateDto
            {
                UserId = userId,
                MediaId = originalMediaId,
                ProjectId = projectId,
                Status = "processing", // Đánh dấu đang chờ AI quét
                DetectedText = string.Empty
            };
            var jobDto = await _ocrJobService.CreateAsync(createJobDto);

            // TRẢ VỀ NGAY LẬP TỨC CHO FRONTEND ĐỂ HIỆN ẢNH (CHƯA CÓ CHỮ)
            return new OcrProcessingResultDto
            {
                JobId = jobDto.Id,
                Status = "processing",
                MediaId = originalMediaId,
                ImageUrl = uploadedUrl,
                Results = new List<CreateOcrResultDto>()
            };
        }


        // --- HÀM 2: GỌI GOOGLE VISION KHI FRONTEND YÊU CẦU ---
        public async Task<OcrProcessingResultDto> ProcessOcrLazyAsync(int jobId)
        {
            // 1. Tìm Job và Media
            var ocrJob = await _db.OcrJobs
                .Include(j => j.Media)
                .Include(j => j.Results) // Include để map data lúc trả về
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (ocrJob == null) return null;

            // 2. KÍCH HOẠT QUÉT AI NẾU CHƯA QUÉT
            if (ocrJob.Status == "processing" || ocrJob.Status == "pending" || !ocrJob.Results.Any())
            {
                _logger.LogInformation("Bắt đầu gọi Google Cloud Vision Lazy cho Job {JobId}", jobId);

                try
                {
                    // Tải lại byte ảnh từ Azure URL (Vì Google Vision cần byte)
                    using var httpClient = new HttpClient();
                    byte[] imageBytes = await httpClient.GetByteArrayAsync(ocrJob.Media.StorageUrl);

                    // Gọi Google Cloud Vision API
                    var client = await ImageAnnotatorClient.CreateAsync();
                    var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
                    var response = await client.DetectDocumentTextAsync(googleImage);

                    string finalDetectedText = string.Empty;
                    var createResults = new List<CreateOcrResultDto>();

                    if (response != null && response.Text != null)
                    {
                        var fullTextBuilder = new StringBuilder();
                        foreach (var page in response.Pages)
                        {
                            foreach (var block in page.Blocks)
                            {
                                foreach (var paragraph in block.Paragraphs)
                                {
                                    foreach (var word in paragraph.Words)
                                    {
                                        string wordText = string.Join("", word.Symbols.Select(s => s.Text));
                                        fullTextBuilder.Append(wordText).Append(" ");

                                        var bboxList = word.BoundingBox.Vertices.Select(v => new[] { v.X, v.Y }).ToList();
                                        string bboxJson = JsonSerializer.Serialize(bboxList);

                                        createResults.Add(new CreateOcrResultDto
                                        {
                                            PageNumber = 1,
                                            WordText = wordText,
                                            BoundingBox = bboxJson
                                        });
                                    }
                                    fullTextBuilder.AppendLine();
                                }
                            }
                        }
                        finalDetectedText = fullTextBuilder.ToString().Trim();
                    }

                    // Lưu kết quả vào DB
                    if (createResults.Count > 0)
                    {
                        await _ocrJobService.AppendResultsAsync(jobId, createResults);
                    }

                    await _ocrJobService.UpdateStatusAsync(jobId, new OcrJobUpdateStatusDto
                    {
                        Status = "completed",
                        DetectedText = finalDetectedText
                    });

                    // Gán lại vào Object để trả về cho FE luôn
                    ocrJob.Status = "completed";
                    ocrJob.DetectedText = finalDetectedText;
                    ocrJob.Results = createResults.Select(r => new OcrResult
                    {
                        PageNumber = r.PageNumber,
                        WordText = r.WordText,
                        BoundingBox = r.BoundingBox
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi Google API");
                    await _ocrJobService.UpdateStatusAsync(jobId, new OcrJobUpdateStatusDto { Status = "failed", DetectedText = "Lỗi Google API" });
                    ocrJob.Status = "failed";
                }
            }

            // 3. Trả về DTO hoàn chỉnh
            return new OcrProcessingResultDto
            {
                JobId = ocrJob.Id,
                Status = ocrJob.Status,
                DetectedText = ocrJob.DetectedText,
                MediaId = (int)ocrJob.MediaId,
                ImageUrl = ocrJob.Media.StorageUrl,
                Results = ocrJob.Results.Select(r => new CreateOcrResultDto
                {
                    PageNumber = (int)r.PageNumber,
                    WordText = r.WordText,
                    BoundingBox = r.BoundingBox
                }).ToList()
            };
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
                // Đọc API Key từ appsettings
string apiKey = _configuration["GoogleCloud:ApiKey"];

// Khởi tạo client bằng Builder và nhét Key vào
var client = await new ImageAnnotatorClientBuilder 
{ 
    ApiKey = apiKey 
}.BuildAsync();
                var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(originalImageBytes);

                // Hàm DetectDocumentTextAsync chuyên trị văn bản mật độ dày (như tiếng Nhật)
                var response = await client.DetectDocumentTextAsync(googleImage);

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
                _logger.LogError(ex, "Lỗi khi gọi Google Vision API");
                await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = "failed", DetectedText = "Lỗi Google API: " + ex.Message });
                throw new Exception("Lỗi khi gọi Google Vision API", ex);
            }

            // --- 5. LƯU KẾT QUẢ VÀO DATABASE ---
            if (createResults.Count > 0)
            {
                await _ocrJobService.AppendResultsAsync(jobDto.Id, createResults);
            }

            await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto
            {
                Status = "completed",
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
                AnnotatedMediaId = null, // Frontend Vue.js sẽ tự vẽ CSS đè lên ảnh gốc
                AnnotatedImageUrl = null,
                Results = createResults
            };
        }
        //public async Task<OcrProcessingResultDto> ProcessImageAsync(IFormFile image, int userId, int workspaceId, int? projectId, bool saveAnnotated)
        //{
        //    // --- 1. Đọc ảnh vào bộ nhớ ---
        //    byte[] originalImageBytes;
        //    await using (var ms = new MemoryStream())
        //    {
        //        await image.CopyToAsync(ms);
        //        originalImageBytes = ms.ToArray();
        //    }

        //    // --- 2. Tải ảnh gốc lên Azure Blob Storage và lưu MediaStore ---
        //    int originalMediaId;
        //    string uploadedUrl;
        //    try
        //    {
        //        var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
        //        await using (var stream = new MemoryStream(originalImageBytes))
        //        {
        //            uploadedUrl = await _blobService.UploadFileBlobAsync(
        //                containerName: "ocr-images", // Tên container của bạn
        //                content: stream,
        //                contentType: image.ContentType,
        //                fileName: uniqueFileName
        //            );
        //        }
        //        _logger.LogInformation("Saved original image to Azure Blob. URL: {Url}", uploadedUrl);

        //        var originalMedia = new MediaStore
        //        {
        //            OwnerId = userId,
        //            WorkspaceId = workspaceId,
        //            FileName = image.FileName,
        //            MimeType = image.ContentType,
        //            SizeBytes = image.Length,
        //            StorageUrl = uploadedUrl, // ✅ SỬA LỖI: Lưu URL từ Azure
        //            Sha256 = ComputeSha256(originalImageBytes),
        //            CreatedAt = DateTime.UtcNow
        //        };
        //        _db.MediaStore.Add(originalMedia);
        //        await _db.SaveChangesAsync();
        //        originalMediaId = originalMedia.Id;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to upload to Azure or save MediaStore record.");
        //        throw new Exception("Failed to save original image.", ex); // Ném lỗi để Controller xử lý
        //    }

        //    // --- 3. Tạo OcrJob ---
        //    var createJobDto = new OcrJobCreateDto
        //    {
        //        UserId = userId,
        //        MediaId = originalMediaId,
        //        ProjectId = projectId,
        //        Status = "pending",
        //        DetectedText = string.Empty
        //    };
        //    var jobDto = await _ocrJobService.CreateAsync(createJobDto);
        //    _logger.LogInformation("Created OcrJob id={Id} linked to MediaId={MediaId}", jobDto.Id, originalMediaId);

        //    // --- 4. Gọi service Python (Flask) ---
        //    var flaskUrl = _cfg["InferService:Url"] ?? "http://127.0.0.1:5000/infer";
        //    _logger.LogInformation("Proxying image to infer service at {Url} for job {JobId}", flaskUrl, jobDto.Id);

        //    HttpResponseMessage resp;
        //    string respText;
        //    try
        //    {
        //        var client = _httpFactory.CreateClient();
        //        client.Timeout = TimeSpan.FromSeconds(60);
        //        using var content = new MultipartFormDataContent();
        //        var streamContent = new ByteArrayContent(originalImageBytes);
        //        streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "application/octet-stream");
        //        content.Add(streamContent, "image", image.FileName);

        //        resp = await client.PostAsync(flaskUrl, content);
        //        respText = await resp.Content.ReadAsStringAsync();
        //    }
        //    catch (TaskCanceledException tex) // Lỗi timeout
        //    {
        //        _logger.LogError(tex, "Timeout contacting infer service {Url}", flaskUrl);
        //        await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = "failed", DetectedText = null });
        //        throw new TimeoutException("Timeout contacting infer service", tex);
        //    }
        //    catch (Exception ex) // Lỗi không kết nối được
        //    {
        //        _logger.LogError(ex, "Failed to contact infer service {Url}", flaskUrl);
        //        await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = "failed", DetectedText = null });
        //        throw new HttpRequestException("Failed to contact infer service", ex);
        //    }

        //    // --- 5. Xử lý phản hồi từ Flask ---
        //    if (!resp.IsSuccessStatusCode)
        //    {
        //        _logger.LogWarning("Infer service returned non-success {Status}: {Body}", (int)resp.StatusCode, respText);
        //        await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = "failed", DetectedText = respText });
        //        throw new Exception($"Infer service returned error {(int)resp.StatusCode}: {respText}");
        //    }

        //    ApiResult result;
        //    try
        //    {
        //        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //        result = JsonSerializer.Deserialize<ApiResult>(respText, opts);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to parse infer response JSON. Body: {Body}", respText);
        //        await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = "failed", DetectedText = null });
        //        throw new JsonException("Failed to parse infer response", ex);
        //    }

        //    // --- 6. Lưu kết quả OCR ---
        //    var createResults = new List<CreateOcrResultDto>();
        //    if (result?.Main_Text != null)
        //    {
        //        foreach (var item in result.Main_Text)
        //        {
        //            var bboxJson = "{}";
        //            try { if (item.Bbox != null) bboxJson = JsonSerializer.Serialize(item.Bbox); } catch { }
        //            createResults.Add(new CreateOcrResultDto
        //            {
        //                PageNumber = 1,
        //                WordText = item.Text,
        //                BoundingBox = bboxJson
        //            });
        //        }
        //    }
        //    if (createResults.Count > 0)
        //    {
        //        await _ocrJobService.AppendResultsAsync(jobDto.Id, createResults);
        //    }

        //    var detectedText = result?.Detected_Text_Lines ?? string.Join("\n", createResults.ConvertAll(r => r.WordText));
        //    await _ocrJobService.UpdateStatusAsync(jobDto.Id, new OcrJobUpdateStatusDto { Status = "completed", DetectedText = detectedText });

        //    // --- 7. Lưu ảnh chú thích (nếu có) ---
        //    string? annotatedImageUrl = null;
        //    int? annotatedMediaId = null;

        //    if (saveAnnotated && !string.IsNullOrEmpty(result?.Annotated_Image))
        //    {
        //        // ✅ Truyền thêm workspaceId vào hàm lưu ảnh phụ
        //        annotatedMediaId = await SaveAnnotatedImageAsync(result.Annotated_Image, userId, workspaceId, jobDto.Id);
        //        if (annotatedMediaId.HasValue)
        //        {
        //            var media = await _db.MediaStore.FindAsync(annotatedMediaId.Value);
        //            annotatedImageUrl = media?.StorageUrl;
        //        }
        //    }

        //    // --- 8. Trả về DTO kết quả ---
        //    return new OcrProcessingResultDto
        //    {
        //        JobId = jobDto.Id,
        //        Status = "completed",
        //        DetectedText = detectedText,
        //        MediaId = originalMediaId,
        //        ImageUrl = uploadedUrl,
        //        AnnotatedMediaId = annotatedMediaId,
        //        AnnotatedImageUrl = annotatedImageUrl
        //    };
        //}

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
