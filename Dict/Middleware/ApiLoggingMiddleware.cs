using Dict.Data;
using Dict.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dict.Middleware
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;

        public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            // Bỏ qua các đường dẫn không phải API (ví dụ: Swagger)
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            // Cho phép đọc body request
            context.Request.EnableBuffering();
            var requestBody = await GetRequestBodyAsync(context.Request);

            // Chạy các middleware/controller khác
            await _next(context);

            stopwatch.Stop();

            var apiCall = new ApiCall
            {
                Endpoint = context.Request.Path,
                RequestJson = requestBody, // Lưu body
                ResponseStatus = context.Response.StatusCode,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // Dùng AddAsync và SaveChangesAsync trong một DbContext riêng
                // (DbContext được inject vào Middleware là 'scoped')
                dbContext.ApiCalls.Add(apiCall);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể ghi log ApiCall xuống DB.");
            }
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            // Đảm bảo stream có thể đọc lại được
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Tua lại stream cho các Controller sau
            return body;
        }
    }
}