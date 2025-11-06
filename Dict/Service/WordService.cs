using Dict.Data;
using Dict.Models;
using Dict.Service.IService;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
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

        public WordService(ApplicationDbContext db, ILogger<WordService> logger)
        {
            _db = db;
            _logger = logger;
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
                // 2. TÌM THẤY: Ghi log Hit (dùng entry.Id)
                // Logic này giờ đã AN TOÀN (sau khi bạn chạy migration)
                await LogSearchHitAsync(entry.Id);
                return entry.RawJson;
            }
            else
            {
                // 3. KHÔNG TÌM THẤY: Ghi log Miss
                await LogSearchMissAsync(label);
                return null;
            }
        }

        public async Task UpsertCacheForLabelAsync(string label, string newJson, string category)
        {
            // (Hàm này giữ nguyên)
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

        // --- CÁC HÀM GHI LOG (ĐÃ CẬP NHẬT) ---

        private async Task LogSearchHitAsync(int entryId) // <-- Giờ đây là EntryId
        {
            try
            {
                // Truy vấn bằng EntryId
                var stat = await _db.StatsWordFreq
                    .FirstOrDefaultAsync(s => s.EntryId == entryId); // <-- SỬA

                if (stat != null)
                {
                    stat.Occurrences = (stat.Occurrences ?? 0) + 1;
                    stat.LastSeenAt = DateTime.UtcNow;
                }
                else
                {
                    stat = new StatsWordFreq
                    {
                        EntryId = entryId, // <-- SỬA
                        Occurrences = 1,
                        LastSeenAt = DateTime.UtcNow
                    };
                    _db.StatsWordFreq.Add(stat);
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi log StatsWordFreq cho EntryId {EntryId}", entryId);
            }
        }

        private async Task LogSearchMissAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            var normalizedTerm = term.Trim().ToLower();

            try
            {
                var miss = await _db.SearchMiss
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
                    _db.SearchMiss.Add(miss);
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi log SearchMiss cho Term {Term}", term);
            }
        }
    }
}