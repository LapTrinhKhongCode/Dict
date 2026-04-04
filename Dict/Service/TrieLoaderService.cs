using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Dict.Service
{
    public class TrieLoaderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TrieAutocompleteCache _trieCache;
        private readonly KanjiCache _kanjiCache;
        private readonly ILogger<TrieLoaderService> _logger;

        // --- DÙNG CLASS TẠM ĐỂ XÂY DỰNG (Dễ quản lý trùng lặp) ---
        private class TempBuilderNode
        {
            public char Character { get; set; }
            public Dictionary<char, TempBuilderNode> Children { get; set; } = new();
            public List<AutocompleteSuggestionDto> Suggestions { get; set; } = new();
        }

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
            _logger.LogInformation("Bắt đầu chiến dịch nạp dữ liệu lên RAM (Bản vá lỗi Flattening)...");
            var watch = Stopwatch.StartNew();

            string binNodesPath = "cache_trie_nodes.bin";
            string jsonSuggestionsPath = "cache_trie_suggestions.json";
            string jsonKanjiPath = "cache_kanji.json";

            if (File.Exists(binNodesPath) && File.Exists(jsonSuggestionsPath) && File.Exists(jsonKanjiPath))
            {
                _logger.LogInformation("Phát hiện Cache. Đang tải dữ liệu trực tiếp vào RAM...");
                await LoadFromCacheAsync(binNodesPath, jsonSuggestionsPath, jsonKanjiPath);
            }
            else
            {
                _logger.LogInformation("Không tìm thấy Cache. Đang xây dựng từ SQL Server...");
                await BuildFromSqlAsync(stoppingToken);
                await SaveToCacheAsync(binNodesPath, jsonSuggestionsPath, jsonKanjiPath);
            }

            watch.Stop();
            _logger.LogInformation($"[SUCCESS] Hệ thống sẵn sàng trong {watch.ElapsedMilliseconds}ms!");
        }

        private async Task LoadFromCacheAsync(string binNodesPath, string jsonSuggestionsPath, string jsonKanjiPath)
        {
            byte[] nodeBytes = await File.ReadAllBytesAsync(binNodesPath);
            _trieCache.NodePool = MemoryMarshal.Cast<byte, FlatTrieNode>(nodeBytes).ToArray();

            string sugJson = await File.ReadAllTextAsync(jsonSuggestionsPath);
            _trieCache.SuggestionPool = JsonSerializer.Deserialize<AutocompleteSuggestionDto[]>(sugJson)
                                        ?? Array.Empty<AutocompleteSuggestionDto>();
            _trieCache.IsLoaded = true;

            string kanjiJson = await File.ReadAllTextAsync(jsonKanjiPath);
            var kanjiDict = JsonSerializer.Deserialize<Dictionary<char, string>>(kanjiJson);
            if (kanjiDict != null)
            {
                foreach (var kvp in kanjiDict)
                {
                    _kanjiCache.Data[kvp.Key] = kvp.Value;
                }
            }
            _kanjiCache.IsLoaded = true;
        }

        private async Task BuildFromSqlAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Đang lấy từ vựng từ SQL...");
            var wordEntries = await db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "word")
                .OrderByDescending(e => e.Weight)
                .Select(e => new AutocompleteSuggestionDto
                {
                    Word = e.Label,
                    Reading = e.Phonetic,
                    Meaning = e.ShortMean,
                    Weight = e.Weight ?? 0
                })
                .ToListAsync(stoppingToken);

            // 1. XÂY DỰNG CÂY BẰNG OOP CLASS (Để dễ kiểm tra trùng lặp)
            var rootBuilder = new TempBuilderNode { Character = ' ' };

            foreach (var entry in wordEntries)
            {
                InsertIntoBuilder(rootBuilder, entry.Word?.ToLower(), entry);
                if (!string.IsNullOrEmpty(entry.Reading))
                {
                    InsertIntoBuilder(rootBuilder, entry.Reading.ToLower(), entry);
                }
            }

            // 2. LÀM PHẲNG CÂY (FLATTEN) SANG MẢNG STRUCT
            FlattenTreeToCache(rootBuilder);

            _trieCache.IsLoaded = true;
            _logger.LogInformation($"Đã xây dựng và làm phẳng xong Trie.");

            // NẠP KANJI 
            _logger.LogInformation("Đang lấy Kanji từ SQL...");
            var kanjiEntries = await db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "kanji")
                .Select(e => new { e.Label, e.RawJson })
                .ToListAsync(stoppingToken);

            foreach (var k in kanjiEntries)
            {
                if (!string.IsNullOrEmpty(k.Label))
                {
                    _kanjiCache.Data[k.Label[0]] = k.RawJson;
                }
            }
            _kanjiCache.IsLoaded = true;
        }

        private void InsertIntoBuilder(TempBuilderNode root, string path, AutocompleteSuggestionDto entry)
        {
            if (string.IsNullOrEmpty(path)) return;
            var curr = root;

            foreach (var c in path)
            {
                if (!curr.Children.TryGetValue(c, out var child))
                {
                    child = new TempBuilderNode { Character = c };
                    curr.Children[c] = child;
                }
                curr = child;

                // LOGIC CHỐNG TRÙNG LẶP CHUẨN XÁC
                if (curr.Suggestions.Count < 30 && !curr.Suggestions.Any(x => x.Word == entry.Word))
                {
                    curr.Suggestions.Add(entry);
                }
            }
        }

        // ==============================================================
        // THUẬT TOÁN BFS: Biến cây Class thành 2 mảng phẳng cực nhẹ
        // ==============================================================
        private void FlattenTreeToCache(TempBuilderNode rootBuilder)
        {
            var flatNodes = new List<FlatTrieNode>();
            var flatSuggestions = new List<AutocompleteSuggestionDto>();

            // Khởi tạo Root
            flatNodes.Add(new FlatTrieNode { Character = ' ', FirstChildIndex = -1, NextSiblingIndex = -1 });

            // Queue lưu trữ Node gốc và Index của nó trong mảng phẳng
            var queue = new Queue<(TempBuilderNode Original, int FlatIndex)>();
            queue.Enqueue((rootBuilder, 0));

            while (queue.Count > 0)
            {
                var (origNode, flatIdx) = queue.Dequeue();

                // Chép Suggestions sang mảng phẳng liên tục
                int sugOffset = flatSuggestions.Count;
                flatSuggestions.AddRange(origNode.Suggestions);

                // Xử lý các con
                int firstChildIdx = -1;
                int lastChildIdx = -1;

                foreach (var kvp in origNode.Children)
                {
                    int newChildIdx = flatNodes.Count;

                    // Tạo Node con (chưa biết Index của con/anh em nó)
                    flatNodes.Add(new FlatTrieNode
                    {
                        Character = kvp.Key,
                        FirstChildIndex = -1,
                        NextSiblingIndex = -1,
                        SuggestionOffset = 0,
                        SuggestionCount = 0
                    });

                    if (firstChildIdx == -1) firstChildIdx = newChildIdx;

                    if (lastChildIdx != -1)
                    {
                        // Cập nhật con trỏ NextSibling cho thằng anh đứng trước
                        var prevSibling = flatNodes[lastChildIdx];
                        prevSibling.NextSiblingIndex = newChildIdx;
                        flatNodes[lastChildIdx] = prevSibling;
                    }

                    lastChildIdx = newChildIdx;
                    queue.Enqueue((kvp.Value, newChildIdx)); // Đưa con vào hàng đợi BFS
                }

                // Cập nhật lại Node hiện tại
                var currFlat = flatNodes[flatIdx];
                currFlat.FirstChildIndex = firstChildIdx;
                currFlat.SuggestionOffset = sugOffset;
                currFlat.SuggestionCount = (short)origNode.Suggestions.Count;
                flatNodes[flatIdx] = currFlat;
            }

            // Gán vào Cache
            _trieCache.NodePool = flatNodes.ToArray();
            _trieCache.SuggestionPool = flatSuggestions.ToArray();
        }

        private async Task SaveToCacheAsync(string binNodesPath, string jsonSuggestionsPath, string jsonKanjiPath)
        {
            byte[] nodeBytes = MemoryMarshal.AsBytes(_trieCache.NodePool.AsSpan()).ToArray();
            await File.WriteAllBytesAsync(binNodesPath, nodeBytes);

            string sugJson = JsonSerializer.Serialize(_trieCache.SuggestionPool);
            await File.WriteAllTextAsync(jsonSuggestionsPath, sugJson);

            string kanjiJson = JsonSerializer.Serialize(_kanjiCache.Data);
            await File.WriteAllTextAsync(jsonKanjiPath, kanjiJson);
        }
    }
}