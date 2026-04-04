using Dict.Data;
using Dict.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
            await foreach (var logMessage in _queueService.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    // Tạo Scope mới cho mỗi lần xử lý để lấy DbContext (vì DBContext không sống dai được như BackgroundService)
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // C# 9+ PATTERN MATCHING: Bóc hàng và xử lý
                    switch (logMessage)
                    {
                        case ApiCall apiCallLog:
                            // 1. Nếu là ApiCall -> Lưu bảng ApiCalls
                            db.ApiCalls.Add(apiCallLog);
                            await db.SaveChangesAsync(stoppingToken);
                            break;

                        case SearchHitMessage hitLog:
                            // 2. Nếu là Hit -> MERGE bảng stats_word_freq
                            await ProcessSearchHitAsync(db, hitLog.EntryId, stoppingToken);
                            break;

                        case SearchMissMessage missLog:
                            // 3. Nếu là Miss -> MERGE bảng SearchMisses
                            await ProcessSearchMissAsync(db, missLog.Term, stoppingToken);
                            break;

                        default:
                            _logger.LogWarning("Có một loại Log lạ lọt vào ống: {Type}", logMessage.GetType().Name);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi ra Console/File để mình biết, nhưng KHÔNG throw exception để ống nước tiếp tục chạy
                    _logger.LogError(ex, "Lỗi khi lưu Log ngầm ({Type}). Bỏ qua bản ghi này.", logMessage?.GetType().Name);
                }
            }
        }

        // Logic của WordService chuyển sang đây
        private async Task ProcessSearchHitAsync(ApplicationDbContext db, int entryId, CancellationToken ct)
        {
            string sql = @"
                MERGE INTO stats_word_freq AS target
                USING (SELECT @entryId AS EntryId) AS source
                ON (target.EntryId = source.EntryId)
                WHEN MATCHED THEN
                    UPDATE SET Occurrences = target.Occurrences + 1, LastSeenAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (EntryId, Occurrences, LastSeenAt)
                    VALUES (@entryId, 1, GETUTCDATE());";

            await db.Database.ExecuteSqlRawAsync(sql, new[] { new SqlParameter("@entryId", entryId) }, ct);
        }

        private async Task ProcessSearchMissAsync(ApplicationDbContext db, string term, CancellationToken ct)
        {
            var normalizedTerm = term.Trim().ToLower();
            string sql = @"
                MERGE INTO SearchMisses AS target
                USING (SELECT @term AS SearchTerm, @norm AS NormalizedTerm) AS source
                ON (target.NormalizedTerm = source.NormalizedTerm)
                WHEN MATCHED THEN
                    UPDATE SET SearchCount = target.SearchCount + 1, LastSearchedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (SearchTerm, NormalizedTerm, SearchCount, LastSearchedAt)
                    VALUES (source.SearchTerm, source.NormalizedTerm, 1, GETUTCDATE());";

            await db.Database.ExecuteSqlRawAsync(sql, new[] {
                new SqlParameter("@term", term),
                new SqlParameter("@norm", normalizedTerm)
            }, ct);
        }
    }
}