using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
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

[ApiController]
[Route("api/[controller]")]
public class InferController : ControllerBase
{

    private readonly ILogger<InferController> _logger;
    private readonly IOcrProcessingService _ocrProcessingService; 
    private readonly ResponseDTO _response;
    private readonly IHttpClientFactory _httpClientFactory;

    // Định nghĩa form nhận file
    public class UploadForm
    {
        [FromForm(Name = "image")]
        public IFormFile image { get; set; }
    }
    public class PredictionRequestDto
    {
        [JsonPropertyName("matrix")]
        public List<List<int>> Matrix { get; set; }
    }
    // Constructor được rút gọn
    public InferController(
        ILogger<InferController> logger,
        IOcrProcessingService ocrProcessingService,
        IHttpClientFactory httpClientFactory)
        
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ocrProcessingService = ocrProcessingService;
        _response = new ResponseDTO();
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

    [HttpPost("stream")]
    // Nhận file từ form, giống hệt [FromForm] IFormFile
    public async Task StreamOcr(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            HttpContext.Response.StatusCode = 400; // BadRequest
            await HttpContext.Response.WriteAsync("No file uploaded.");
            return;
        }

        try
        {
            // 1. Dùng IHttpClientFactory (cách làm đúng trong dự án DI)
            using var httpClient = _httpClientFactory.CreateClient();

            // 2. Tạo form-data để gửi file (giữ nguyên logic)
            using var formData = new MultipartFormDataContent();
            var fileStreamContent = new StreamContent(file.OpenReadStream());
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            formData.Add(fileStreamContent, "file", file.FileName);

            // 3. Tạo request POST đến Python (giữ nguyên logic)
            var pythonRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "http://127.0.0.1:8000/ocr-stream"
            );
            pythonRequest.Content = formData;

            // 4. Gửi request và chỉ đọc HEADER (giữ nguyên logic)
            using var pythonResponse = await httpClient.SendAsync(
                pythonRequest,
                HttpCompletionOption.ResponseHeadersRead, // Chìa khóa stream
                HttpContext.RequestAborted // Ngắt nếu client ngắt
            );

            // 5. Cài đặt Header cho Vue (giữ nguyên logic)
            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.StatusCode = (int)pythonResponse.StatusCode;

            if (!pythonResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Python API error: {pythonResponse.StatusCode}");
                // Không cần làm gì thêm, Vue sẽ nhận được code lỗi
                return;
            }

            // 6. Lấy stream từ Python (giữ nguyên logic)
            using var pythonStream = await pythonResponse.Content.ReadAsStreamAsync();

            // 7. Bơm stream từ Python về Vue (giữ nguyên logic)
            await pythonStream.CopyToAsync(HttpContext.Response.Body, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi stream OCR");
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 500;
                await HttpContext.Response.WriteAsync($"Lỗi server C#: {ex.Message}");
            }
        }
    }



    [HttpPost("predict")]
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
            var pythonUrl = "http://127.0.0.1:8000/predict";

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


    [HttpGet("health")]
    [AllowAnonymous] // Cho phép kiểm tra health mà không cần đăng nhập
    public IActionResult Health() => Ok(new { status = "ok", now = DateTimeOffset.UtcNow });

    [HttpPost("upload-and-infer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAndInfer([FromForm] UploadForm form, [FromQuery] bool saveAnnotated = false)
    {
        try
        {
            var image = form?.image;
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { error = "No image uploaded. Use form-field 'image'." });
            }

            // 1. Lấy UserId
            var userId = GetUserId();

            // 2. Ủy quyền toàn bộ công việc cho service
            OcrProcessingResultDto result = await _ocrProcessingService.ProcessImageAsync(image, userId, saveAnnotated);

            // 3. Trả về kết quả thành công
            return Ok(result);
        }
        catch (Exception e)
        {
            // Bắt tất cả các lỗi được ném ra từ service (Timeout, HttpRequest, DB, ...)
            _logger.LogError(e, "Unhandled exception in UploadAndInfer");

            // Trả về lỗi 500 chung
            // Bạn cũng có thể bắt các Exception cụ thể (TimeoutException, JsonException) 
            // để trả về mã lỗi 504, 502, v.v.
            return Problem(detail: e.Message, statusCode: 500);
        }
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
