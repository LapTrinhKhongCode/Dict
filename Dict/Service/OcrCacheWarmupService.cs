using Dict.Data;
using Dict.DTO.OCR;
using Dict.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dict.Service
{
    /// <summary>
    /// Background service that preloads the N most recently accessed completed OCR jobs
    /// into memory cache on server startup, so demo/first-load is fast.
    /// </summary>
    public class OcrCacheWarmupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OcrCacheWarmupService> _logger;

        private const string CachePrefix = "ocr_job_";
        private const int WarmupJobCount = 30; // Preload latest 30 completed jobs
        private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromDays(7))
            .SetAbsoluteExpiration(TimeSpan.FromDays(7));

        public OcrCacheWarmupService(
            IServiceProvider serviceProvider,
            IMemoryCache cache,
            ILogger<OcrCacheWarmupService> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit for the app to fully start before hitting DB
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            _logger.LogInformation("🔥 OCR Cache Warmup: starting preload of latest {Count} completed jobs...", WarmupJobCount);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var jobs = await db.OcrJobs
                    .AsNoTracking()
                    .Include(j => j.Media)
                    .Include(j => j.Results)
                    .Where(j => j.Status == "completed" && j.Results.Any())
                    .OrderByDescending(j => j.UpdatedAt ?? j.CreatedAt)
                    .Take(WarmupJobCount)
                    .ToListAsync(stoppingToken);

                int loaded = 0;
                foreach (var job in jobs)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    var key = $"{CachePrefix}{job.Id}";
                    if (!_cache.TryGetValue(key, out _))
                    {
                        var dto = MapToDto(job);
                        _cache.Set(key, dto, CacheOptions);
                        loaded++;
                    }
                }

                _logger.LogInformation("✅ OCR Cache Warmup: preloaded {Loaded}/{Total} jobs into memory.", loaded, jobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ OCR Cache Warmup failed — will serve cold on first request.");
            }
        }

        private static OcrProcessingResultDto MapToDto(OcrJob job) => new()
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
}
