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
        private readonly string sensitiveCollation = "Japanese_CS_AS_KS_WS"; // Collation phân biệt Kana
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
                .Where(e => EF.Functions.Collate(e.Label, sensitiveCollation) == term && e.Type == "word")
                .IncludeAllData() // Sử dụng extension method
                .FirstOrDefaultAsync();
        }

        public Task<List<Entry>> FindHomophonesAsync(string term)
        {
            return _db.Entries
                .AsNoTracking()
                // Dùng Collate để đảm bảo tìm đúng Hiragana/Katakana
                .Where(e => e.Words.Any(w => EF.Functions.Collate(w.Phonetic, sensitiveCollation) == term) && e.Type == "word")
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
                    Score = (EF.Functions.Collate(e.Label, sensitiveCollation) == term ? 100 : 0) +
                            (e.Words.FirstOrDefault() != null && EF.Functions.Collate(e.Words.FirstOrDefault().Phonetic, sensitiveCollation) == term ? 90 : 0) +
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

        // Triển khai phương thức lấy gợi ý
        public async Task<List<AutocompleteSuggestionDto>> GetAutocompleteSuggestionsAsync(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length > 100)
            {
                // Return an empty list instead of BadRequest, since this is a service class
                return new List<AutocompleteSuggestionDto>();
            }
            if (string.IsNullOrWhiteSpace(term)) return new List<AutocompleteSuggestionDto>();
            string cleanTerm = term.Trim().ToLower();

            // 1. ƯU TIÊN: TÌM TRONG TRIE (RAM) - Tốc độ < 1ms
            if (_trieCache.IsLoaded)
            {
                var curr = _trieCache.Root;
                bool found = true;
                foreach (var c in cleanTerm)
                {
                    if (!curr.Children.TryGetValue(c, out curr))
                    {
                        found = false;
                        break;
                    }
                }

                if (found && curr.Suggestions.Any())
                {
                    return curr.Suggestions;
                }
            }

            // 2. DỰ PHÒNG (FALLBACK): TRUY VẤN SQL SERVER (Code gốc của Đức Anh)
            // Chạy khi Trie chưa load xong hoặc cần tìm kiếm sâu hơn
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
    }
}
