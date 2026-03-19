using Dict.Data;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // CẦN THIẾT CHO IServiceScopeFactory
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dict.Service
{
    public class WordService : IWordService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS";
        private readonly ILogger<WordService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public WordService(ApplicationDbContext db, ILogger<WordService> logger, IServiceScopeFactory scopeFactory)
        {
            _db = db;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<string?> GetWordJson(string label)
        {
            // 1. Lấy Entry (Id và RawJson)
            var entry = await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "word" &&
                            (EF.Functions.Collate(k.Label, _sensitiveCollation) == label))
                .Select(k => new { k.Id, k.RawJson })
                .FirstOrDefaultAsync();

            if (entry != null)
            {
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

        // --- CÁC HÀM GHI LOG (Hàm Void, Fire and Forget) ---

        private void LogSearchHitAsync(int entryId) // Đổi thành void
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // TẠO SCOPE MỚI TẠI ĐÂY LÀ CHUẨN XÁC
                    using var scope = _scopeFactory.CreateScope();
                    var backgroundDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var stat = await backgroundDb.StatsWordFreq
                        .FirstOrDefaultAsync(s => s.EntryId == entryId);

                    if (stat != null)
                    {
                        stat.Occurrences = (stat.Occurrences ?? 0) + 1;
                        stat.LastSeenAt = DateTime.UtcNow;
                    }
                    else
                    {
                        stat = new StatsWordFreq
                        {
                            EntryId = entryId,
                            Occurrences = 1,
                            LastSeenAt = DateTime.UtcNow
                        };
                        backgroundDb.StatsWordFreq.Add(stat);
                    }
                    await backgroundDb.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi ghi log StatsWordFreq cho EntryId {EntryId}", entryId);
                }
            });
        }

        private void LogSearchMissAsync(string term) // Đổi thành void
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            var normalizedTerm = term.Trim().ToLower();

            _ = Task.Run(async () =>
            {
                try
                {
                    // LỖI Ở BẢN TRƯỚC LÀ QUÊN ĐOẠN NÀY!
                    // Phải tạo DbContext mới cho luồng Miss này
                    using var scope = _scopeFactory.CreateScope();
                    var backgroundDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var miss = await backgroundDb.SearchMiss
                        .FirstOrDefaultAsync(sm => sm.NormalizedTerm == normalizedTerm);

                    if (miss != null)
                    {
                        miss.SearchCount++;
                        miss.LastSearchedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        miss = new SearchMiss
                        {
                            SearchTerm = term.Length > 255 ? term.Substring(0, 255) : term,
                            NormalizedTerm = normalizedTerm,
                            SearchCount = 1,
                            LastSearchedAt = DateTime.UtcNow
                        };
                        backgroundDb.SearchMiss.Add(miss); // Dùng backgroundDb
                    }
                    await backgroundDb.SaveChangesAsync(); // Dùng backgroundDb
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi ghi log SearchMiss cho Term {Term}", term);
                }
            });
        }
    }
}