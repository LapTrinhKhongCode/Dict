using Dict.Data;
using Dict.DTO;
using Dict.Service.IService;
using EllipticCurve.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Dict.Models;

public class KanjiService : IKanjiService
{
    private readonly ApplicationDbContext _db;
    private readonly IJsonBuilderService _jsonBuilderService;
    private readonly ILogger<KanjiService> _logger;
    private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS";

    public KanjiService(
        ApplicationDbContext db,
        IJsonBuilderService jsonBuilderService,
        ILogger<KanjiService> logger)
    {
        _db = db;
        _jsonBuilderService = jsonBuilderService;
        _logger = logger;
    }

    public async Task<string?> GetKanjiJson(string label)
    {
        string kanjiChars = new string(label.Where(c => c >= 0x4E00 && c <= 0x9FFF).ToArray());

        if (string.IsNullOrEmpty(kanjiChars))
        {
            // --- KỊCH BẢN 1: TÌM THEO PHIÊN ÂM (PHONETIC) ---
            var phoneticMatch = await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "kanji" && k.Phonetic == label)
                .Select(k => new { k.Id, k.RawJson })
                .FirstOrDefaultAsync();

            if (phoneticMatch != null)
            {
                // GHI LOG HIT (AN TOÀN SAU KHI MIGRATION)
                await LogSearchHitAsync(phoneticMatch.Id);
                return phoneticMatch.RawJson;
            }

            await LogSearchMissAsync(label);
            return JsonConvert.SerializeObject(new { status = 404, error = "No Kanji found in search term" });
        }

        if (kanjiChars.Length == 1)
        {
            // --- KỊCH BẢN 2: TÌM THEO HÁN TỰ (LABEL) ---
            var entry = await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "kanji" && k.Label == kanjiChars)
                .Select(k => new { k.Id, k.RawJson })
                .FirstOrDefaultAsync();

            if (entry != null && entry.RawJson != "{}")
            {
                // GHI LOG HIT (AN TOÀN SAU KHI MIGRATION)
                await LogSearchHitAsync(entry.Id);
                return entry.RawJson;
            }
        }

        return await _jsonBuilderService.RebuildJsonForKanjiAsync(kanjiChars);
    }

    public async Task<KanjiDto?> GetKanjiInfoAsync(
        string character,
        string languageCode = "en",
        int maxWords = 200,
        CancellationToken cancellationToken = default)
    {
        // (Hàm này giữ nguyên, không cần sửa)
        // ... (Logic của bạn) ...
        return null; // (Thay bằng logic thật của bạn)
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