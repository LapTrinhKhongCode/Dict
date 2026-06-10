using Dict.DTO;
using Dict.Models;

namespace Dict.Service.IService
{
    public interface ISearchService
    {
        /// <summary>
        /// Tìm MỘT entry khớp chính xác Label (phân biệt Kana).
        /// Trả về Entry đầy đủ dữ liệu (đã Include).
        /// </summary>
        Task<Entry?> FindExactLabelMatchAsync(string term);

        /// <summary>
        /// Tìm TẤT CẢ các entry có Phonetic khớp chính xác (đồng âm).
        /// Trả về danh sách Entry đầy đủ dữ liệu, sắp xếp theo Weight.
        /// </summary>
        Task<List<Entry>> FindHomophonesAsync(string term);

        /// <summary>
        /// Tìm các entry phù hợp để làm gợi ý (theo thuật toán điểm).
        /// Trả về danh sách Entry đã Include dữ liệu cần thiết cho gợi ý.
        /// </summary>
        Task<List<Entry>> GetSuggestionEntriesAsync(string term, int limit, List<int>? excludeEntryIds = null);
        Task<List<AutocompleteSuggestionDto>> GetAutocompleteSuggestionsAsync(string term);

        /// <summary>
        /// Tìm entry theo NGHĨA TIẾNG VIỆT (Gloss.Text), accent-insensitive.
        /// Xếp theo mức độ liên quan: khớp chính xác > bắt đầu bằng > chứa từ khoá.
        /// </summary>
        Task<List<AutocompleteSuggestionDto>> SearchByVietnameseMeaningAsync(string term, int limit = 12);
    }
}
