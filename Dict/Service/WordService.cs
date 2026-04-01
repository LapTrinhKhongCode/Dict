using Dict.Data;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection; // CẦN THIẾT CHO IServiceScopeFactory
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dict.Service
{
    public class WordService : IWordService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS";
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<WordService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public WordService(ApplicationDbContext db, ILogger<WordService> logger, IServiceScopeFactory scopeFactory, IMemoryCache memoryCache)
        {
            _db = db;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _memoryCache = memoryCache;
        }

        public async Task<string?> GetWordJson(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return null;
            if (label.Length > 50)
            {
                return JsonConvert.SerializeObject(new { status = 400, error = "Input too long" });
            }
            string cacheKey = $"word_json_{label}";
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedJson))
            {
                // Log nhẹ một cái để biết là trúng Cache (Hit)
                // _logger.LogInformation("L2 Cache HIT: {Label}", label);
                return cachedJson;
            }
            // 1. Lấy Entry (Id và RawJson)
            var entry = await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "word" &&
                            (EF.Functions.Collate(k.Label, _sensitiveCollation) == label))
                .Select(k => new { k.Id, k.RawJson })
                .FirstOrDefaultAsync();

            if (entry != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30)) // Nếu 30p ko ai sờ đến thì xóa
                .SetAbsoluteExpiration(TimeSpan.FromHours(2))   // Sau 2h chắc chắn xóa để cập nhật mới
                .SetSize(1); // Giới hạn kích thước nếu cần

                _memoryCache.Set(cacheKey, entry.RawJson, cacheOptions);
                // 2. TÌM THẤY: Ghi log Hit (Gọi thẳng, KHÔNG DÙNG AWAIT)
                LogSearchHitAsync(entry.Id);
                return entry.RawJson;
            }
            else
            {
                // 3. KHÔNG TÌM THẤY: Ghi log Miss (Gọi thẳng, KHÔNG DÙNG AWAIT)
                LogSearchMissAsync(label);
                return null;
            }
        }

        public async Task UpsertCacheForLabelAsync(string label, string newJson, string category)
        {
            var existingEntry = await _db.Entries
                .Where(k => k.Type == "word" &&
                            EF.Functions.Collate(k.Label, _sensitiveCollation) == label)
                .FirstOrDefaultAsync();

            if (existingEntry != null)
            {
                existingEntry.RawJson = newJson;
                existingEntry.EntryCategory = category;
                existingEntry.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newCacheEntry = new Entry
                {
                    Type = "word",
                    Label = label,
                    Phonetic = label,
                    RawJson = newJson,
                    EntryCategory = category,
                    JsonProcessingStatus = "Processed_Success",
                    CommentRawJson = "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Weight = 0,
                    ShortMean = "（Tổng hợp các từ đồng âm）"
                };
                _db.Entries.Add(newCacheEntry);
            }
            await _db.SaveChangesAsync();
        }


        public async Task<int> GetSearchMissCountAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return 0;

            var normalizedTerm = term.Trim().ToLower();

            // Dùng AsNoTracking để đọc nhanh, không tốn tài nguyên tracking
            return await _db.SearchMiss
                .AsNoTracking()
                .Where(sm => sm.NormalizedTerm == normalizedTerm)
                .Select(sm => sm.SearchCount)
                .FirstOrDefaultAsync();
        }


        public async Task IncrementSearchMissAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length > 255) return;

            var normalizedTerm = term.Trim().ToLower();

            // Câu lệnh MERGE: "Nếu thấy thì Update, không thấy thì Insert"
            // Giúp triệt tiêu Race Condition khi nhiều user cùng search 1 từ mới
            string sql = @"
        MERGE INTO SearchMisses AS target
        USING (SELECT @term AS SearchTerm, @norm AS NormalizedTerm) AS source
        ON (target.NormalizedTerm = source.NormalizedTerm)
        WHEN MATCHED THEN
            UPDATE SET 
                SearchCount = target.SearchCount + 1, 
                LastSearchedAt = GETUTCDATE()
        WHEN NOT MATCHED THEN
            INSERT (SearchTerm, NormalizedTerm, SearchCount, LastSearchedAt)
            VALUES (source.SearchTerm, source.NormalizedTerm, 1, GETUTCDATE());";

            try
            {
                // Thực thi trực tiếp xuống nhân SQL Server
                await _db.Database.ExecuteSqlRawAsync(sql,
                    new SqlParameter("@term", term),
                    new SqlParameter("@norm", normalizedTerm));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy Upsert SearchMiss cho: {Term}", term);
            }
        }
        // --- CÁC HÀM GHI LOG (Hàm Void, Fire and Forget) ---

        private void LogSearchHitAsync(int entryId)
        {
            // BƯỚC 1: Thay vì tự chạy Task.Run ở đây, ta ném nó vào "ống dẫn" Channel
            // Nếu bạn muốn nhanh nhất, hãy gửi một tín hiệu (Message) vào LogQueueService
            // Ở đây mình sẽ sửa lại logic MERGE trước cho chuẩn:

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Lệnh MERGE cho bảng Stats: Tăng Occurrences nếu có, nạp mới nếu chưa
                    string sql = @"
                MERGE INTO stats_word_freq AS target
                USING (SELECT @entryId AS EntryId) AS source
                ON (target.EntryId = source.EntryId)
                WHEN MATCHED THEN
                    UPDATE SET 
                        Occurrences = target.Occurrences + 1, 
                        LastSeenAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (EntryId, Occurrences, LastSeenAt)
                    VALUES (@entryId, 1, GETUTCDATE());";

                    await db.Database.ExecuteSqlRawAsync(sql, new Microsoft.Data.SqlClient.SqlParameter("@entryId", entryId));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật Stats cho EntryId: {Id}", entryId);
                }
            });
        }

        private void LogSearchMissAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            var normalizedTerm = term.Trim().ToLower();

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // DÙNG SQL MERGE ĐỂ "VÔ ĐỐI" CONCURRENCY
                    // Nhớ dùng đúng tên bảng [SearchMisses] (có chữ es)
                    string sql = @"
                MERGE INTO SearchMisses AS target
                USING (SELECT @term AS SearchTerm, @norm AS NormalizedTerm) AS source
                ON (target.NormalizedTerm = source.NormalizedTerm)
                WHEN MATCHED THEN
                    UPDATE SET 
                        SearchCount = target.SearchCount + 1, 
                        LastSearchedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (SearchTerm, NormalizedTerm, SearchCount, LastSearchedAt)
                    VALUES (source.SearchTerm, source.NormalizedTerm, 1, GETUTCDATE());";

                    await db.Database.ExecuteSqlRawAsync(sql,
                        new Microsoft.Data.SqlClient.SqlParameter("@term", term),
                        new Microsoft.Data.SqlClient.SqlParameter("@norm", normalizedTerm));
                }
                catch (Exception ex)
                {
                    // Bây giờ lỗi duy nhất có thể xảy ra là DB chết, 
                    // còn lỗi trùng lặp sẽ bị triệt tiêu hoàn toàn.
                    _logger.LogError(ex, "Lỗi khi Upsert SearchMisses cho Term: {Term}", term);
                }
            });
        }
    }
}