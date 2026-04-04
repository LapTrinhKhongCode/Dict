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

        // THÊM ỐNG NƯỚC VÀO ĐÂY (Đã xóa IServiceScopeFactory)
        private readonly LogQueueService _logQueue;

        public WordService(
            ApplicationDbContext db,
            ILogger<WordService> logger,
            IMemoryCache memoryCache,
            LogQueueService logQueue) // <-- Inject vào đây
        {
            _db = db;
            _logger = logger;
            _memoryCache = memoryCache;
            _logQueue = logQueue;
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
                            k.Label == label)
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
                            k.Label == label)
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
            // Ném gói hàng Hit vào ống nước (Cực kỳ an toàn, tốn 0.0001ms)
            _ = _logQueue.QueueLogAsync(new SearchHitMessage(entryId));
        }

        private void LogSearchMissAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            // Ném gói hàng Miss vào ống nước
            _ = _logQueue.QueueLogAsync(new SearchMissMessage(term));
        }
    }
}