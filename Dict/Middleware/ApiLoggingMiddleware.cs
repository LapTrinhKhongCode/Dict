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
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            // Skip đọc body nếu là file upload (multipart/form-data)
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

            await _next(context);
            stopwatch.Stop();

            var apiCall = new ApiCall
            {
                Endpoint = context.Request.Path,
                RequestJson = requestBody,
                ResponseStatus = context.Response.StatusCode,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
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
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
    }
}