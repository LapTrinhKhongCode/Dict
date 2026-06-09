using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageMagick;
using System.Text.Json;
using Dict.DTO.OCR;
using Google.Cloud.Vision.V1;
[ApiController]
[Route("api/[controller]")]
public class InferController : ControllerBase
{

    private readonly ILogger<InferController> _logger;
    private readonly IOcrProcessingService _ocrProcessingService; 
    private readonly IOcrJobService _ocrJobService;
    private readonly ResponseDTO _response;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _pythonApiBaseUrl;
    private readonly ICurrentUserService _currentUserService;
    // Định nghĩa form nhận file
    public class UploadForm
    {
        [FromForm(Name = "image")]
        public IFormFile image { get; set; }

        [FromForm(Name = "projectId")] // ✅ Lấy thêm ProjectId từ giao diện Vue
        public int? ProjectId { get; set; }
    }
    public class PredictionRequestDto
    {
        [JsonPropertyName("matrix")]
        public List<List<int>> Matrix { get; set; }
    }
    // DTO để hứng JSON trả về từ Python
    public class OcrStreamResult
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("line_number")]
        public int LineNumber { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("bbox")]
        public List<int> Bbox { get; set; }

        [JsonPropertyName("crop_dataurl")]
        public string CropDataUrl { get; set; }

        [JsonPropertyName("vis_dataurl")]
        public string VisDataUrl { get; set; }

        // Chúng ta sẽ thêm trường này
        [JsonPropertyName("page_number")]
        public int PageNumber { get; set; }
    }
    // === DTO MỚI CHO DỊCH THUẬT ===
    public class TranslationRequestDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        // Mặc dù Python là 'tgt_lang', C# convention là PascalCase
        // JsonPropertyName sẽ lo việc chuyển đổi
        [JsonPropertyName("tgt_lang")]
        public string TgtLang { get; set; }
    }

    public class TranslationResponseDto
    {
        [JsonPropertyName("original_text")]
        public string OriginalText { get; set; }

        [JsonPropertyName("translated_text")]
        public string TranslatedText { get; set; }

        [JsonPropertyName("detected_lang")]
        public string DetectedLang { get; set; }

        [JsonPropertyName("processing_time_ms")]
        public float ProcessingTimeMs { get; set; }
    }
    // === KẾT THÚC DTO MỚI ===
    // Constructor được rút gọn
    public InferController(
        ILogger<InferController> logger,
        IOcrProcessingService ocrProcessingService,
        IHttpClientFactory httpClientFactory, IConfiguration configuration, IOcrJobService ocrJobService, ICurrentUserService currentUserService)
        
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ocrProcessingService = ocrProcessingService;
        _ocrJobService = ocrJobService;
        _currentUserService = currentUserService;
        _response = new ResponseDTO();
        _pythonApiBaseUrl = configuration["ServiceUrls:PythonApiBaseUrl"]
                            ?? "http://127.0.0.1:8000";
    }

    // ✨ CẢI TIẾN: Dùng ClaimTypes.NameIdentifier
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            // Dòng này sẽ được kích hoạt nếu token không hợp lệ hoặc không chứa userId,
            // mặc dù [Authorize] thường sẽ chặn các request này trước.
            throw new InvalidOperationException("User ID không hợp lệ hoặc không tìm thấy trong token.");
        }
        return userId;
    }
    // InferController.cs — Thêm endpoint mới

    public class CreatePdfJobForm
    {
        [FromForm(Name = "projectId")]
        public int? ProjectId { get; set; }

        [FromForm(Name = "fileName")]
        public string FileName { get; set; }

        [FromForm(Name = "totalPages")]
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Bước 1: FE gọi 1 lần khi mở PDF → tạo Job trống, nhận jobId
    /// </summary>
    [HttpPost("create-pdf-job")]
    [Authorize]
    public async Task<IActionResult> CreatePdfJob([FromForm] CreatePdfJobForm form)
    {
        try
        {
            int userId = _currentUserService.UserId;
            int workspaceId = _currentUserService.WorkspaceId;

            var result = await _ocrProcessingService.CreatePdfJobAsync(
                userId, workspaceId, form.ProjectId, form.FileName, form.TotalPages
            );
            return Ok(result);
        }
        catch (Exception e)
        {
            return Problem(detail: e.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Bước 2: FE gọi từng trang → upload ảnh PNG, gắn vào jobId + pageNumber
    /// </summary>
    public class UploadPageForm
    {
        [FromForm(Name = "image")]
        public IFormFile Image { get; set; }

        [FromForm(Name = "jobId")]
        public int JobId { get; set; }

        [FromForm(Name = "pageNumber")]
        public int PageNumber { get; set; }
    }

    [HttpPost("upload-pdf-page")]
    [Authorize]
    public async Task<IActionResult> UploadPdfPage([FromForm] UploadPageForm form)
    {
        try
        {
            if (form.Image == null || form.Image.Length == 0)
                return BadRequest("Không có ảnh.");

            var result = await _ocrProcessingService.UploadAndOcrPageAsync(
                form.JobId, form.PageNumber, form.Image
            );
            return Ok(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Lỗi upload page {Page} cho Job {Job}", form.PageNumber, form.JobId);
            return Problem(detail: e.Message, statusCode: 500);
        }
    }
    [HttpPost("stream")]
    [Authorize] // 1. BẮT BUỘC [Authorize] để lấy GetUserId()
    public async Task StreamOcr(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            HttpContext.Response.StatusCode = 400; // BadRequest
            await HttpContext.Response.WriteAsync("No file uploaded.");
            return;
        }

        OcrJobDto ocrJob = null;
        var fullTextBuilder = new StringBuilder();
        var resultsBatch = new List<CreateOcrResultDto>();
        const int BATCH_SIZE = 20;
        int userId = 0;

        try
        {
            // 2. Lấy UserId và Tạo Job trong DB
            userId = GetUserId();
            ocrJob = await _ocrJobService.CreateAsync(new OcrJobCreateDto
            {
                UserId = userId,
                Status = "processing",
                DetectedText = ""
            });

            // 3. Gọi Python API (Giữ nguyên)
            using var httpClient = _httpClientFactory.CreateClient();
            using var formData = new MultipartFormDataContent();
            var fileStreamContent = new StreamContent(file.OpenReadStream());
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            formData.Add(fileStreamContent, "file", file.FileName);

            var pythonRequest = new HttpRequestMessage(HttpMethod.Post, $"{_pythonApiBaseUrl}/ocr-stream");
            pythonRequest.Content = formData;

            using var pythonResponse = await httpClient.SendAsync(
                pythonRequest,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );

            // 4. Cài đặt Header cho Vue (Giữ nguyên)
            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.StatusCode = (int)pythonResponse.StatusCode;

            if (!pythonResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Python API error: {pythonResponse.StatusCode}");
                // Ném lỗi để Catch xử lý (cập nhật Job = Failed)
                throw new Exception($"Python API error: {pythonResponse.StatusCode}");
            }

            using var pythonStream = await pythonResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(pythonStream);

            // 5. Đọc Stream, Gửi về Vue, VÀ Ghi log DB
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(line))
                {
                    await HttpContext.Response.WriteAsync("\n\n");
                    continue;
                }

                if (line.StartsWith("data:"))
                {
                    try
                    {
                        string jsonString = line.Substring(5).Trim();
                        var data = JsonSerializer.Deserialize<OcrStreamResult>(jsonString);

                        if (data != null && data.Status == "result")
                        {
                            data.PageNumber = 1; // Ảnh đơn luôn là trang 1

                            // A. Thêm kết quả vào "túi" (Batch)
                            resultsBatch.Add(new CreateOcrResultDto
                            {
                                PageNumber = 1,
                                WordText = data.Text,
                                BoundingBox = JsonSerializer.Serialize(data.Bbox)
                            });
                            fullTextBuilder.AppendLine(data.Text);

                            // B. Nếu túi đầy, đổ túi vào DB
                            if (resultsBatch.Count >= BATCH_SIZE)
                            {
                                await _ocrJobService.AppendResultsAsync(ocrJob.Id, resultsBatch);
                                resultsBatch.Clear(); // Dọn túi
                            }

                            // C. Gửi JSON đã sửa đổi về Vue (Giữ nguyên)
                            string modifiedJsonString = JsonSerializer.Serialize(data);
                            await HttpContext.Response.WriteAsync($"data: {modifiedJsonString}\n\n");
                            await HttpContext.Response.Body.FlushAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi parse JSON từ Python: {line}");
                    }
                }
            }

            // 6. LƯU NỐT PHẦN CÒN LẠI VÀ CẬP NHẬT TRẠNG THÁI
            if (resultsBatch.Any())
            {
                await _ocrJobService.AppendResultsAsync(ocrJob.Id, resultsBatch);
            }

            await _ocrJobService.UpdateStatusAsync(ocrJob.Id, new OcrJobUpdateStatusDto
            {
                Status = "completed",
                DetectedText = fullTextBuilder.ToString()
            });
            _logger.LogInformation("Đã xử lý xong ảnh đơn cho Job {JobId}.", ocrJob.Id);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi stream OCR (User: {UserId})", userId);

            if (ocrJob != null)
            {
                await _ocrJobService.UpdateStatusAsync(ocrJob.Id, new OcrJobUpdateStatusDto
                {
                    Status = "failed",
                    DetectedText = fullTextBuilder.ToString()
                });
                _logger.LogError("Đã đánh dấu Job {JobId} là Failed.", ocrJob.Id);
            }

            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 500;
                await HttpContext.Response.WriteAsync($"Lỗi server C#: {ex.Message}");
            }
        }
    }


    [HttpPost("pdf-stream")]
    [Authorize] // 1. BẮT BUỘC [Authorize]
    public async Task StreamPdfOcr(IFormFile file)
    {
        if (file == null || file.Length == 0 || file.ContentType != "application/pdf")
        {
            HttpContext.Response.StatusCode = 400; // BadRequest
            await HttpContext.Response.WriteAsync("Vui lòng tải lên một file PDF hợp lệ.");
            return;
        }

        HttpContext.Response.ContentType = "text/event-stream";
        HttpContext.Response.StatusCode = 200;

        int pageIndex = 1;
        OcrJobDto ocrJob = null; // Biến để lưu Job DTO
        var fullTextBuilder = new StringBuilder(); // Biến để lưu Full Text
        var resultsBatch = new List<CreateOcrResultDto>(); // Biến để "tích" kết quả
        const int BATCH_SIZE = 20; // Ghi xuống DB sau mỗi 20 dòng
        int userId = 0;

        try
        {
            // 2. Lấy UserId và Tạo Job
            userId = GetUserId();
            ocrJob = await _ocrJobService.CreateAsync(new OcrJobCreateDto
            {
                UserId = userId,
                Status = "processing",
                DetectedText = ""
            });

            // 3. Mở PDF (Giữ nguyên)
            using var images = new MagickImageCollection();
            await images.ReadAsync(file.OpenReadStream());
            _logger.LogInformation($"Tách PDF thành công. Số trang: {images.Count} (Job: {ocrJob.Id})");

            // 4. Lặp qua từng trang (Giữ nguyên)
            foreach (var image in images)
            {
                image.Format = MagickFormat.Png;
                byte[] imageBytes = image.ToByteArray();
                _logger.LogInformation($"Đang xử lý trang {pageIndex} cho Job {ocrJob.Id}...");

                // (Gọi Python API giữ nguyên...)
                using var httpClient = _httpClientFactory.CreateClient();
                using var formData = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                formData.Add(imageContent, "file", $"page_{pageIndex}.png");

                var pythonRequest = new HttpRequestMessage(HttpMethod.Post, $"{_pythonApiBaseUrl}/ocr-stream");
                pythonRequest.Content = formData;

                using var pythonResponse = await httpClient.SendAsync(
                    pythonRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    HttpContext.RequestAborted
                );

                if (!pythonResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Python API error (Page {pageIndex}): {pythonResponse.StatusCode}");
                    continue;
                }

                // 5. Đọc Stream, Gửi về Vue, VÀ Ghi log DB
                using var pythonStream = await pythonResponse.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(pythonStream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                    {
                        await HttpContext.Response.WriteAsync("\n\n");
                        continue;
                    }

                    if (line.StartsWith("data:"))
                    {
                        try
                        {
                            string jsonString = line.Substring(5).Trim();
                            var data = JsonSerializer.Deserialize<OcrStreamResult>(jsonString);

                            if (data != null && data.Status == "result")
                            {
                                data.PageNumber = pageIndex;

                                // A. Thêm kết quả vào "túi" (Batch)
                                resultsBatch.Add(new CreateOcrResultDto
                                {
                                    PageNumber = pageIndex,
                                    WordText = data.Text,
                                    BoundingBox = JsonSerializer.Serialize(data.Bbox)
                                });
                                fullTextBuilder.AppendLine(data.Text);

                                // B. Nếu túi đầy, đổ túi vào DB
                                if (resultsBatch.Count >= BATCH_SIZE)
                                {
                                    await _ocrJobService.AppendResultsAsync(ocrJob.Id, resultsBatch);
                                    resultsBatch.Clear(); // Dọn túi
                                }

                                // C. Gửi JSON đã sửa đổi về Vue (Giữ nguyên)
                                string modifiedJsonString = JsonSerializer.Serialize(data);
                                await HttpContext.Response.WriteAsync($"data: {modifiedJsonString}\n\n");
                                await HttpContext.Response.Body.FlushAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi parse JSON từ Python: {line}");
                        }
                    }
                }
                pageIndex++;
            } // Kết thúc vòng lặp ForEach

            // === 6. LƯU NỐT PHẦN CÒN LẠI VÀ CẬP NHẬT TRẠNG THÁI ===
            if (resultsBatch.Any())
            {
                await _ocrJobService.AppendResultsAsync(ocrJob.Id, resultsBatch);
            }

            await _ocrJobService.UpdateStatusAsync(ocrJob.Id, new OcrJobUpdateStatusDto
            {
                Status = "completed",
                DetectedText = fullTextBuilder.ToString()
            });
            _logger.LogInformation("Đã xử lý xong tất cả các trang PDF cho Job {JobId}.", ocrJob.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý stream PDF (User: {UserId})", userId);

            if (ocrJob != null)
            {
                await _ocrJobService.UpdateStatusAsync(ocrJob.Id, new OcrJobUpdateStatusDto
                {
                    Status = "failed",
                    DetectedText = fullTextBuilder.ToString()
                });
                _logger.LogError("Đã đánh dấu Job {JobId} là Failed.", ocrJob.Id);
            }

            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 500;
                await HttpContext.Response.WriteAsync($"Lỗi server C#: {ex.Message}");
            }
        }
    }

    [HttpPost("predict")]
    [AllowAnonymous]
    public async Task<IActionResult> PredictHandwriting([FromBody] PredictionRequestDto request)
    {
        // Kiểm tra dữ liệu đầu vào (đơn giản)
        if (request == null || request.Matrix == null || request.Matrix.Count != 64)
        {
            return BadRequest("Input matrix must be 64x64.");
        }

        _logger.LogInformation("Nhận được request /predict");

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            // URL đến endpoint MỚI của Python
            var pythonUrl = $"{_pythonApiBaseUrl}/predict";

            // Gửi request.
            // Đây là request-response JSON bình thường, KHÔNG stream.
            // Dùng PostAsJsonAsync để tự động serialize 'request' thành JSON
            using var response = await httpClient.PostAsJsonAsync(
                pythonUrl,
                request, // Gửi thẳng DTO (C# sẽ tự convert sang JSON)
                HttpContext.RequestAborted
            );

            if (!response.IsSuccessStatusCode)
            {
                // Nếu Python lỗi, trả lỗi đó về cho Vue
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Python /predict API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return StatusCode((int)response.StatusCode, errorBody);
            }

            // Đọc kết quả (JSON) từ Python
            var predictionResult = await response.Content.ReadFromJsonAsync<object>();

            _logger.LogInformation("/predict thành công");
            // Trả kết quả (JSON) về cho Vue
            return Ok(predictionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi gọi /predict");
            return StatusCode(500, $"Lỗi server C#: {ex.Message}");
        }
    }

    // === ENDPOINT MỚI CHO DỊCH THUẬT ===
    [HttpPost("translate")]
    [AllowAnonymous]
    public async Task<IActionResult> TranslateText([FromBody] TranslationRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Input 'text' cannot be empty." });
        }

        _logger.LogInformation("Nhận được request /translate cho: {Text}", request.Text.Length > 50 ? request.Text.Substring(0, 50) + "..." : request.Text);

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var pythonUrl = $"{_pythonApiBaseUrl}/translate";

            // Gửi request JSON (giống hệt /predict)
            using var response = await httpClient.PostAsJsonAsync(
                pythonUrl,
                request, // Gửi DTO request
                HttpContext.RequestAborted
            );

            // Kiểm tra lỗi từ Python
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Python /translate API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return StatusCode((int)response.StatusCode, errorBody);
            }

            // Đọc kết quả JSON
            var translationResult = await response.Content.ReadFromJsonAsync<TranslationResponseDto>();

            _logger.LogInformation("/translate thành công");

            // Trả kết quả về cho client
            return Ok(translationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi gọi /translate");
            return StatusCode(500, $"Lỗi server C#: {ex.Message}");
        }
    }
    // === KẾT THÚC ENDPOINT MỚI ===

    [HttpGet("health")]
    [AllowAnonymous] // Cho phép kiểm tra health mà không cần đăng nhập
    public IActionResult Health() => Ok(new { status = "ok", now = DateTimeOffset.UtcNow });

    // 1. API UPLOAD (CỰC NHANH - LAZY LOAD PART 1)
    [HttpPost("upload-and-infer")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadAndInfer([FromForm] UploadForm form, [FromQuery] bool saveAnnotated = false)
    {
        try
        {
            var image = form?.image;
            if (image == null || image.Length == 0) return BadRequest("No image uploaded.");

            int userId = _currentUserService.UserId;
            int workspaceId = _currentUserService.WorkspaceId;
            int? projectId = form?.ProjectId > 0 ? form.ProjectId : null;

            if (userId == 0 || workspaceId == 0)
                return Unauthorized(new { error = "Token không hợp lệ." });

            // GỌI HÀM MỚI CHỈ UPLOAD LÊN AZURE
            OcrProcessingResultDto result = await _ocrProcessingService.UploadImageOnlyAsync(image, userId, workspaceId, projectId);

            return Ok(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception in UploadAndInfer");
            return Problem(detail: e.Message, statusCode: 500);
        }
    }

    // 2. API GET CHI TIẾT VÀ XỬ LÝ OCR (LAZY LOAD PART 2)
    [HttpGet("job/{jobId}")]
    [Authorize]
    public async Task<IActionResult> GetJobDetails(int jobId)
    {
        try
        {
            // Gọi hàm xử lý OCR ngầm nếu chưa làm
            var result = await _ocrProcessingService.ProcessOcrLazyAsync(jobId);
            if (result == null) return NotFound("Không tìm thấy Job này.");

            return Ok(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Lỗi khi lấy chi tiết Job");
            return Problem(detail: e.Message, statusCode: 500);
        }
    }

    [HttpPost("scan-page-ocr")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> ScanPageOcr([FromForm] ScanPageForm form)
    {
        try
        {
            // 1. Lấy ảnh từ form ra
            var image = form?.image;
            if (image == null || image.Length == 0) return BadRequest("Không có ảnh.");

            // 2. Biến ảnh nhận được từ VueJS thành mảng Byte
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            byte[] imageBytes = ms.ToArray();

            // 3. Gọi Google Cloud Vision
            var client = await ImageAnnotatorClient.CreateAsync();
            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);

            var response = await client.DetectDocumentTextAsync(googleImage);

            var createResults = new List<CreateOcrResultDto>();
            string finalDetectedText = string.Empty;

            // Phân tích JSON của Google
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

            return Ok(new
            {
                message = "Quét thành công",
                detectedText = finalDetectedText,
                results = createResults
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi quét OCR: " + ex.Message });
        }
    }

    public class ScanPageForm
    {
        public IFormFile image { get; set; }
    }

    [HttpGet("me/recent")]
    public async Task<IActionResult> GetMyRecentOcrJobs()
    {
        try
        {
            var userId = GetUserId();

            // ✨ SỬA LẠI ĐỂ GỌI _ocrProcessingService
            var ocrJobs = await _ocrProcessingService.GetRecentOcrJobsForUserAsync(userId, 5);

            _response.Result = ocrJobs;
            _response.Message = "Successfully retrieved recent OCR jobs.";
            return Ok(_response);
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.Message = ex.Message;
            return BadRequest(_response);
        }
    }
}
