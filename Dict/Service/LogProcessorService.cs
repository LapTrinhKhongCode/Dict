using Dict.Data;

namespace Dict.Service
{
    public class LogProcessorService : BackgroundService
    {
        private readonly LogQueueService _queueService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LogProcessorService> _logger;

        public LogProcessorService(LogQueueService queueService, IServiceScopeFactory scopeFactory, ILogger<LogProcessorService> logger)
        {
            _queueService = queueService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Lắng nghe đường ống liên tục
            await foreach (var log in _queueService.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        db.ApiCalls.Add(log);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi ra Console/File để mình biết, nhưng KHÔNG throw exception
                    _logger.LogError(ex, "Lỗi khi lưu Log ngầm. Bỏ qua bản ghi này để tránh sập App.");
                }
            }
        }
    }
}
