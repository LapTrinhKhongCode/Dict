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
using Microsoft.Extensions.DependencyInjection; // <-- QUAN TRỌNG: Thêm thư viện này

public class KanjiService : IKanjiService
{
    private readonly ApplicationDbContext _db;
    private readonly IJsonBuilderService _jsonBuilderService;
    private readonly ILogger<KanjiService> _logger;
    private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS";
    private readonly IServiceScopeFactory _scopeFactory;

    public KanjiService(
        ApplicationDbContext db,
        IJsonBuilderService jsonBuilderService,
        ILogger<KanjiService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _jsonBuilderService = jsonBuilderService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> GetKanjiJson(string label)
    {
        // 1. Lọc lấy các ký tự là Kanji và đảm bảo không bị trùng lặp
        var kanjiList = label.Where(c => c >= 0x4E00 && c <= 0x9FFF)
                             .Select(c => c.ToString())
                             .Distinct()
                             .ToList();

        if (!kanjiList.Any())
        {
            // Kịch bản tìm theo phiên âm (giữ nguyên)
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

        // 2. KỊCH BẢN TỐI ƯU NHẤT: Bắn 1 lệnh IN quét toàn bộ các chữ Hán
        var entries = await _db.Entries.AsNoTracking()
            .Where(k => k.Type == "kanji" && kanjiList.Contains(k.Label))
            .Select(k => new { k.Id, k.Label, k.RawJson })
            .ToListAsync();

        if (entries.Any())
        {
            LogSearchHitAsync(entries.First().Id); // Ghi log 1 phát đại diện

            // 3. Gom Json
            var combinedResults = new List<object>();

            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.RawJson) && entry.RawJson != "{}")
                {
                    try
                    {
                        // Parse cái JSON gốc (vd: của chữ 大) ra thành Object
                        var parsed = JsonConvert.DeserializeObject<dynamic>(entry.RawJson);

                        // ÉP NÓ VÀO MẢNG LUÔN, KHÔNG CẦN CHỌC VÀO TÌM KEY NÀO NỮA
                        combinedResults.Add(parsed);
                    }
                    catch { /* Bỏ qua lỗi */ }
                }
            }

            if (combinedResults.Any())
            {
                // Trả về đúng format: {"status": 200, "results": [ {chữ Đại}, {chữ Học} ]}
                // Ở đây mình bọc cái mảng combinedResults vào biến "results"
                return JsonConvert.SerializeObject(new { status = 200, results = combinedResults });
            }
        }

        // 4. Nếu không có trong DB thì cho Rebuild
        string kanjiStringForRebuild = string.Join("", kanjiList);
        return await _jsonBuilderService.RebuildJsonForKanjiAsync(kanjiStringForRebuild);
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

    private void LogSearchHitAsync(int entryId) // <-- ĐỔI THÀNH VOID
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // TẠO SCOPE MỚI ĐỂ TRÁNH LỖI DISPOSED DBCONTEXT
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
                    backgroundDb.StatsWordFreq.Add(stat); // Dùng backgroundDb
                }
                await backgroundDb.SaveChangesAsync(); // Dùng backgroundDb
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi log StatsWordFreq cho EntryId {EntryId}", entryId);
            }
        });
    }

    private void LogSearchMissAsync(string term) // <-- ĐỔI THÀNH VOID
    {
        if (string.IsNullOrWhiteSpace(term)) return;
        var normalizedTerm = term.Trim().ToLower();

        _ = Task.Run(async () =>
        {
            try
            {
                // BỌC LUÔN THẰNG NÀY VÀO SCOPE RIÊNG BIỆT
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