using Dict.Models;
using Dict.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Dict.Middleware
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;
        private readonly LogQueueService _logQueue;

        public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger, LogQueueService logQueue)
        {
            _next = next;
            _logger = logger;
            _logQueue = logQueue;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            string requestBody = "";
            var contentType = context.Request.ContentType ?? "";
            bool isFileUpload = contentType.Contains("multipart/form-data");

            if (!isFileUpload)
            {
                context.Request.EnableBuffering();
                requestBody = await GetRequestBodyAsync(context.Request);
            }
            else
            {
                requestBody = "[file upload]";
            }

            // --- GIAI ĐOẠN QUAN TRỌNG ---
            await _next(context); // Chạy API
            stopwatch.Stop();
            // ----------------------------

            var endpoint = context.Request.Path.ToString();
            var apiCall = new ApiCall
            {
                Endpoint = endpoint.Length > 500 ? endpoint.Substring(0, 500) : endpoint,
                RequestJson = requestBody,
                ResponseStatus = context.Response.StatusCode,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };

            // THAY VÌ LƯU DB, TA NÉM VÀO HÀNG ĐỢI RỒI KẾT THÚC REQUEST LUÔN
            try
            {
                // Dùng `_ =` để bảo trình biên dịch "Tôi cố tình ném vào không đợi kết quả, đừng báo lỗi"
                _ = _logQueue.QueueLogAsync(apiCall);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể đưa ApiCall vào hàng đợi.");
            }
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
    }
}