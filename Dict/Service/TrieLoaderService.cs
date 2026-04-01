using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics; // Dùng để đo thời gian nạp cho "oai"

namespace Dict.Service
{
    public class TrieLoaderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TrieAutocompleteCache _trieCache;
        private readonly KanjiCache _kanjiCache;
        private readonly ILogger<TrieLoaderService> _logger;

        public TrieLoaderService(
            IServiceProvider serviceProvider,
            TrieAutocompleteCache trieCache,
            KanjiCache kanjiCache,
            ILogger<TrieLoaderService> logger)
        {
            _serviceProvider = serviceProvider;
            _trieCache = trieCache;
            _kanjiCache = kanjiCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bắt đầu chiến dịch nạp dữ liệu lên RAM...");
            var watch = Stopwatch.StartNew();

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // --- PHẦN 1: NẠP TỪ VỰNG VÀO TRIE ---
                _logger.LogInformation("Đang nạp từ vựng (Trie)...");
                var wordEntries = await db.Entries
                    .AsNoTracking()
                    .Where(e => e.Type == "word")
                    .OrderBy(e => e.Weight)
                    .Select(e => new AutocompleteSuggestionDto
                    {
                        Word = e.Label,
                        Reading = e.Phonetic,
                        Meaning = e.ShortMean,
                        Weight = e.Weight ?? 0
                    })
                    .ToListAsync(stoppingToken);

                foreach (var entry in wordEntries)
                {
                    InsertPath(entry.Word?.ToLower(), entry);
                    if (!string.IsNullOrEmpty(entry.Reading))
                    {
                        InsertPath(entry.Reading.ToLower(), entry);
                    }
                }
                _trieCache.IsLoaded = true;
                _logger.LogInformation($"Đã nạp xong {wordEntries.Count} từ vựng.");

                // --- PHẦN 2: NẠP KANJI VÀO DICTIONARY ---
                _logger.LogInformation("Đang nạp Kanji (Dictionary)...");
                var kanjiEntries = await db.Entries
                    .AsNoTracking()
                    .Where(e => e.Type == "kanji")
                    .Select(e => new { e.Label, e.RawJson })
                    .ToListAsync(stoppingToken);

                foreach (var k in kanjiEntries)
                {
                    if (!string.IsNullOrEmpty(k.Label))
                    {
                        // Lưu chữ cái đầu tiên (Label của Kanji luôn là 1 chữ)
                        _kanjiCache.Data[k.Label[0]] = k.RawJson;
                    }
                }
                _kanjiCache.IsLoaded = true;
                _logger.LogInformation($"Đã nạp xong {kanjiEntries.Count} chữ Kanji.");
            }

            watch.Stop();
            _logger.LogInformation($"[SUCCESS] Toàn bộ dữ liệu đã sẵn sàng trên RAM trong {watch.ElapsedMilliseconds}ms. Tốc độ tra cứu bây giờ là 0ms!");
        }

        private void InsertPath(string path, AutocompleteSuggestionDto entry)
        {
            if (string.IsNullOrEmpty(path)) return;
            var curr = _trieCache.Root;
            foreach (var c in path)
            {
                if (!curr.Children.ContainsKey(c))
                    curr.Children[c] = new TrieNode();

                curr = curr.Children[c];

                // Logic giữ 10 gợi ý để tối ưu RAM
                if (curr.Suggestions.Count < 30 && !curr.Suggestions.Any(x => x.Word == entry.Word))
                {
                    curr.Suggestions.Add(entry);
                }
            }
        }
    }
}