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
        private readonly string sensitiveCollation = "Japanese_CS_AS_KS_WS"; // Collation phân biệt Kana
        // Tiêm (inject) DbContext vào constructor
        public SearchService(ApplicationDbContext dbContext)
        {
            _db = dbContext;
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
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return new List<AutocompleteSuggestionDto>();
            }

            // --- Logic Autocomplete Tối Ưu ---
            // --- THÊM BƯỚC LÀM SẠCH (SANITIZE) ---
            // Loại bỏ các ký tự đặc biệt của FTS để tránh lỗi SQL
            string ftsSafeTerm = term
                .Replace("\"", "") // Bỏ dấu nháy kép
                .Replace("-", "")  // Bỏ dấu gạch ngang
                .Replace("+", "")  // Bỏ dấu cộng
                .Replace("(", "")  // Bỏ ngoặc
                .Replace(")", "")
                .Replace("*", ""); // Bỏ dấu sao

            // Nếu sau khi làm sạch, term bị rỗng (ví dụ: người dùng chỉ gõ "-")
            if (string.IsNullOrWhiteSpace(ftsSafeTerm))
            {
                return new List<AutocompleteSuggestionDto>();
            }
            // Chuẩn bị các biến thể tìm kiếm cho FTS và LIKE
            // SỬA LẠI CHO ĐÚNG
            string ftsTerm = $"\"{ftsSafeTerm}*\"";
            string startsWithTerm = ftsSafeTerm + "%";

            var suggestions = await _db.Entries
                .AsNoTracking()
                // Bước 1: Lọc RỘNG và NHANH bằng FTS (ưu tiên tìm từ bắt đầu bằng)
                .Where(e => e.Type == "word" &&
                           (EF.Functions.Contains(e.Label, ftsTerm) || EF.Functions.Contains(e.Phonetic, ftsTerm))
                 )
                // Bước 2: JOIN SỚM với bảng Words để lấy thông tin cần thiết
                .Join(_db.Words.AsNoTracking(), // Join với words
                      entry => entry.Id,        // Khóa của entries
                      word => word.EntryId,    // Khóa của words
                      (entry, word) => new { Entry = entry, WordInfo = word }) // Kết quả join
                                                                               // Bước 3: Tính điểm và lọc dựa trên kết quả JOIN
                .Select(joined => new
                {
                    joined.Entry,
                    joined.WordInfo,
                    // Hệ thống điểm tinh chỉnh (ưu tiên khớp chính xác và bắt đầu bằng)
                    Score =
                        (EF.Functions.Collate(joined.Entry.Label, sensitiveCollation) == term ? 100 : 0) +
                        (joined.WordInfo != null && EF.Functions.Collate(joined.WordInfo.Phonetic, sensitiveCollation) == term ? 90 : 0) +
                        // Dùng LIKE cho StartsWith vì FTS prefix có thể không chính xác 100% cho mọi trường hợp
                        (EF.Functions.Like(joined.Entry.Label, startsWithTerm) ? 50 : 0) +
                        (EF.Functions.Like(joined.WordInfo.Phonetic, startsWithTerm) ? 40 : 0) +
                        // Điểm cơ bản cho việc FTS tìm thấy (đã lọc ở WHERE)
                        10
                })
                // Bước 4: Sắp xếp (ORDER BY) - Giờ đã có đủ thông tin
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.WordInfo.Weight) // Sắp xếp theo Weight trực tiếp
                .ThenBy(x => x.Entry.Label.Length)
                // Bước 5: Giới hạn kết quả
                .Take(10)
                // Bước 6: Chọn ra DTO cuối cùng
                .Select(x => new AutocompleteSuggestionDto
                {
                    Word = x.WordInfo.WordText,
                    Reading = x.WordInfo.Phonetic,
                    Meaning = x.WordInfo.ShortMean
                })
                .ToListAsync(); // Thực thi truy vấn

            return suggestions;
        }
    }
}
