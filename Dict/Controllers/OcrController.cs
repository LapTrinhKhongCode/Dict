using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Cors.Infrastructure;

[ApiController]
[Route("api/[controller]")]
public class InferController : ControllerBase
{

    private readonly ILogger<InferController> _logger;
    private readonly IOcrProcessingService _ocrProcessingService; // ✨ Chỉ cần service này
    private readonly ResponseDTO _response;

    // Định nghĩa form nhận file
    public class UploadForm
    {
        [FromForm(Name = "image")]
        public IFormFile image { get; set; }
    }

    // Constructor được rút gọn
    public InferController(
        ILogger<InferController> logger,
        IOcrProcessingService ocrProcessingService)
        
    {
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
