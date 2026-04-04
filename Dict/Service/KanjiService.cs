using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using EllipticCurve.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection; // <-- QUAN TRỌNG: Thêm thư viện này
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class KanjiService : IKanjiService
{
    private readonly ApplicationDbContext _db;
    private readonly IJsonBuilderService _jsonBuilderService;
    private readonly ILogger<KanjiService> _logger;
    private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS";
    private readonly KanjiCache _kanjiCache;
    private readonly IMemoryCache _memoryCache;

    // THÊM ỐNG NƯỚC VÀO ĐÂY
    private readonly LogQueueService _logQueue;

    public KanjiService(
        ApplicationDbContext db,
        IJsonBuilderService jsonBuilderService,
        ILogger<KanjiService> logger,
        KanjiCache kanjiCache,
        IMemoryCache memoryCache,
        LogQueueService logQueue) // <-- Inject vào đây
    {
        _db = db;
        _jsonBuilderService = jsonBuilderService;
        _logger = logger;
        _kanjiCache = kanjiCache;
        _memoryCache = memoryCache;
        _logQueue = logQueue;
    }

    public async Task<string?> GetKanjiJson(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        if (label.Length > 50)
        {
            return JsonConvert.SerializeObject(new { status = 400, error = "Input too long" });
        }
        string fullCacheKey = $"kanji_full_{label}";
        if (_memoryCache.TryGetValue(fullCacheKey, out string? cachedResult))
        {
            return cachedResult;
        }
        // 1. Lọc Kanji từ chuỗi nhập vào (Unique)
        var kanjiChars = label.Where(c => c >= 0x4E00 && c <= 0x9FFF).Distinct().ToList();

        // --- TRƯỜNG HỢP 1: KHÔNG CÓ KÝ TỰ KANJI (TÌM THEO PHIÊN ÂM) ---
        if (!kanjiChars.Any())
        {
            var phoneticMatch = await _db.Entries.AsNoTracking()
                .Where(k => k.Type == "kanji" && k.Phonetic == label)
                .Select(k => new { k.Id, k.RawJson })
                .FirstOrDefaultAsync();

            if (phoneticMatch != null)
            {
                LogSearchHitAsync(phoneticMatch.Id);
                return phoneticMatch.RawJson;
            }
            LogSearchMissAsync(label);
            return JsonConvert.SerializeObject(new { status = 404, error = "No Kanji found" });
        }

        // --- TRƯỜNG HỢP 2: CÓ KANJI -> TRIỂN KHAI CHIẾN THUẬT HYBRID ---
        var rawJsonResults = new List<string>(); // Lưu chuỗi thô, không Parse Object
        var missingInCache = new List<string>();

        // 2.1. Quét RAM trước (O(1) mỗi ký tự)
        foreach (var c in kanjiChars)
        {
            if (_kanjiCache.IsLoaded && _kanjiCache.Data.TryGetValue(c, out var json))
                rawJsonResults.Add(json);
            else
                missingInCache.Add(c.ToString());
        }

        // 2.2. Nếu RAM thiếu chữ nào, mới đi tìm trong DB (Chỉ tìm những chữ thiếu)
        if (missingInCache.Any())
        {
            var dbEntries = await _db.Entries.AsNoTracking()
                .Where(k => k.Type == "kanji" && missingInCache.Contains(k.Label))
                .Select(k => new { k.Id, k.Label, k.RawJson })
                .ToListAsync();

            foreach (var entry in dbEntries)
            {
                rawJsonResults.Add(entry.RawJson);
                missingInCache.Remove(entry.Label);
            }
            if (dbEntries.Any()) LogSearchHitAsync(dbEntries.First().Id);
        }

        // 2.3. Trả về kết quả nếu tìm thấy đủ
        if (rawJsonResults.Any())
        {
            // Tối ưu: Dùng string.Join để nối chuỗi JSON thô, không tốn CPU Deserialize/Serialize
            var finalResult = "{\"status\":200,\"results\":[" + string.Join(",", rawJsonResults) + "]}";

            // Lưu vào L2 Cache trong 60 phút
            _memoryCache.Set(fullCacheKey, finalResult, TimeSpan.FromMinutes(60));

            if (missingInCache.Any())
            {
                _logger.LogWarning($"Thiếu {missingInCache.Count} chữ: {string.Join("", missingInCache)}");
                // Có thể gọi Rebuild ngầm ở đây nếu muốn
            }

            return finalResult;
        }

        // --- TRƯỜNG HỢP 3: KHÔNG THẤY GÌ CẢ -> REBUILD TẤT CẢ ---
        string kanjiStringForRebuild = string.Join("", kanjiChars);
        return await _jsonBuilderService.RebuildJsonForKanjiAsync(kanjiStringForRebuild);
    }

    // Hàm phụ để parse JSON an toàn
    private dynamic? TryParseJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return null;
        try
        {
            return JsonConvert.DeserializeObject<dynamic>(json);
        }
        catch
        {
            return null;
        }
    }
    public async Task<KanjiDto?> GetKanjiInfoAsync(
        string character,
        string languageCode = "en",
        int maxWords = 200,
        CancellationToken cancellationToken = default)
    {
        // (Hàm này giữ nguyên)
        return null;
    }

    // --- CÁC HÀM GHI LOG (ĐÃ ĐƯỢC "ĐỘ" LÊN FIRE-AND-FORGET) ---

    private void LogSearchHitAsync(int entryId)
    {
        _ = _logQueue.QueueLogAsync(new SearchHitMessage(entryId));
    }

    private void LogSearchMissAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;
        _ = _logQueue.QueueLogAsync(new SearchMissMessage(term));
    }
}