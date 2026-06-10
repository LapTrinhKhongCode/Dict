using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _db;
        private readonly TrieAutocompleteCache _trieCache;

        // ĐÃ XÓA: private readonly string sensitiveCollation = "..."; (Vì DB đã tự lo)

        // Tiêm (inject) DbContext vào constructor
        public SearchService(ApplicationDbContext dbContext, TrieAutocompleteCache trieCache)
        {
            _db = dbContext;
            _trieCache = trieCache;
        }

        public Task<Entry?> FindExactLabelMatchAsync(string term)
        {
            return _db.Entries
                .AsNoTracking()
                .Where(e => e.Label == term && e.Type == "word")
                // Source_Word ưu tiên trước Homophone_Build
                .OrderBy(e => e.EntryCategory == "Homophone_Build" ? 1 : 0)
                .ThenBy(e => e.Weight)
                .IncludeAllData()
                .FirstOrDefaultAsync();
        }

        public Task<List<Entry>> FindHomophonesAsync(string term)
        {
            return _db.Entries
                .AsNoTracking()
                // BỎ EF.Functions.Collate, DÙNG == BÌNH THƯỜNG
                .Where(e => e.Words.Any(w => w.Phonetic == term) && e.Type == "word")
                .OrderBy(e => e.Words.FirstOrDefault().Weight)
                .IncludeAllData()
                .ToListAsync();
        }

        public async Task<List<Entry>> GetSuggestionEntriesAsync(string term, int limit, List<int>? excludeEntryIds = null)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new List<Entry>();

            var candidatesQuery = _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "word" && (
                   EF.Functions.Contains(e.Label, term) ||
                   EF.Functions.Contains(e.Phonetic, term) ||
                   e.Label.StartsWith(term) ||
                   e.Phonetic.StartsWith(term)
                 ));

            if (excludeEntryIds != null && excludeEntryIds.Any())
            {
                candidatesQuery = candidatesQuery.Where(e => !excludeEntryIds.Contains(e.Id));
            }

            var rankedSuggestions = await candidatesQuery
                .Select(e => new
                {
                    Entry = e,
                    WordInfo = e.Words.FirstOrDefault(),
                    // BỎ EF.Functions.Collate TRONG CÔNG THỨC TÍNH SCORE
                    Score = (e.Label == term ? 100 : 0) +
                            (e.Words.FirstOrDefault() != null && e.Words.FirstOrDefault().Phonetic == term ? 90 : 0) +
                            (e.Label.StartsWith(term) ? 50 : 0) +
                            (e.Phonetic.StartsWith(term) ? 40 : 0) +
                            (EF.Functions.Contains(e.Label, term) || EF.Functions.Contains(e.Phonetic, term) ? 10 : 0)
                })
                .Where(x => x.Score > 0 && x.WordInfo != null)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.WordInfo.Weight)
                .ThenBy(x => x.Entry.Label.Length)
                .Take(limit)
                .Select(x => x.Entry)
                // Chỉ Include dữ liệu cần cho suggestWord (nhẹ hơn)
                .Include(e => e.Words)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses)
                .ToListAsync();

            return rankedSuggestions;
        }

        // Triển khai phương thức lấy gợi ý (PHIÊN BẢN MẢNG PHẲNG - KHÔNG ĐỔI)
        public async Task<List<AutocompleteSuggestionDto>> GetAutocompleteSuggestionsAsync(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length > 100)
            {
                return new List<AutocompleteSuggestionDto>();
            }
            if (string.IsNullOrWhiteSpace(term)) return new List<AutocompleteSuggestionDto>();
            string cleanTerm = term.Trim().ToLower();

            // 1. ƯU TIÊN: TÌM TRONG TRIE (RAM MẢNG PHẲNG) - Tốc độ < 1ms
            if (_trieCache.IsLoaded)
            {
                int currIdx = _trieCache.RootIndex; // Bắt đầu từ Root (Index 0)
                bool found = true;

                foreach (var c in cleanTerm)
                {
                    // Lấy thằng con đầu tiên của Node hiện tại
                    int childIdx = _trieCache.NodePool[currIdx].FirstChildIndex;
                    found = false;

                    // Duyệt qua các thằng em (Siblings) để tìm ký tự khớp
                    while (childIdx != -1)
                    {
                        if (_trieCache.NodePool[childIdx].Character == c)
                        {
                            currIdx = childIdx; // Tìm thấy, nhảy xuống Node này
                            found = true;
                            break;
                        }
                        childIdx = _trieCache.NodePool[childIdx].NextSiblingIndex; // Chuyển sang thằng em kế tiếp
                    }

                    // Nếu duyệt hết Siblings mà không thấy ký tự khớp -> Từ này không có trong Trie
                    if (!found) break;
                }

                // Nếu tìm thấy đến ký tự cuối cùng của chuỗi tìm kiếm
                if (found)
                {
                    var resultNode = _trieCache.NodePool[currIdx];

                    // Nếu Node này có chứa gợi ý
                    if (resultNode.SuggestionCount > 0)
                    {
                        // "Cắt" lấy đúng số lượng gợi ý từ SuggestionPool khổng lồ
                        return _trieCache.SuggestionPool
                            .Skip(resultNode.SuggestionOffset)
                            .Take(resultNode.SuggestionCount)
                            .ToList();
                    }
                }
            }

            // 2. DỰ PHÒNG (FALLBACK): TRUY VẤN SQL SERVER (Chạy khi Trie chưa load xong)
            return await _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "word" &&
                           (EF.Functions.Like(e.Label, cleanTerm + "%") ||
                            EF.Functions.Like(e.Phonetic, cleanTerm + "%")))
                .Select(e => new {
                    e.Label,
                    e.Phonetic,
                    e.ShortMean,
                    e.Weight,
                    Score = (e.Label == cleanTerm ? 100 : 0) +
                            (e.Phonetic == cleanTerm ? 90 : 0) +
                            (EF.Functions.Like(e.Label, cleanTerm + "%") ? 50 : 0)
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Weight)
                .Take(15)
                .Select(x => new AutocompleteSuggestionDto
                {
                    Word = x.Label,
                    Reading = x.Phonetic,
                    Meaning = x.ShortMean
                })
                .ToListAsync();
        }

        // Collation dùng cho tìm kiếm tiếng Việt accent-insensitive trên SQL Server
        private const string VI_COLLATION = "Vietnamese_CI_AI";

        public async Task<List<AutocompleteSuggestionDto>> SearchByVietnameseMeaningAsync(string term, int limit = 12)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length > 100)
                return new List<AutocompleteSuggestionDto>();

            var clean = term.Trim();

            // Bước 1: Tìm entryId + score từ bảng Glosses (ngôn ngữ "vi", accent-insensitive)
            // GroupBy entryId để mỗi từ chỉ xuất hiện 1 lần dù có nhiều nghĩa match
            var ranked = await _db.Glosses
                .AsNoTracking()
                .Where(g => g.Language!.Code == "vi" &&
                            g.Sense!.Entry!.Type == "word" &&
                            EF.Functions.Like(
                                EF.Functions.Collate(g.Text, VI_COLLATION),
                                $"%{clean}%"))
                .Select(g => new
                {
                    EntryId   = (int)g.Sense!.EntryId!,
                    Label     = g.Sense.Entry!.Label,
                    Phonetic  = g.Sense.Entry.Phonetic ?? "",
                    ShortMean = g.Sense.Entry.ShortMean ?? "",
                    Weight    = g.Sense.Entry.Weight ?? 99999,
                    // Score: exact=100, startsWith=50, contains=20 — lấy max trong nhóm
                    Score =
                        (EF.Functions.Collate(g.Text, VI_COLLATION) == clean ? 100 : 0) +
                        (EF.Functions.Like(EF.Functions.Collate(g.Text, VI_COLLATION), clean + "%") ? 50 : 0) +
                        20  // base contains
                })
                .GroupBy(x => x.EntryId)
                .Select(grp => new
                {
                    MaxScore  = grp.Max(x => x.Score),
                    MinWeight = grp.Min(x => x.Weight),
                    Label     = grp.Min(x => x.Label),    // Min = bất kỳ (group cùng entry)
                    Phonetic  = grp.Min(x => x.Phonetic),
                    ShortMean = grp.Min(x => x.ShortMean)
                })
                .OrderByDescending(x => x.MaxScore)
                .ThenBy(x => x.MinWeight)
                .Take(limit)
                .Select(x => new AutocompleteSuggestionDto
                {
                    Word    = x.Label,
                    Reading = x.Phonetic,
                    Meaning = x.ShortMean,
                    Weight  = x.MinWeight
                })
                .ToListAsync();

            return ranked;
        }
    }
}