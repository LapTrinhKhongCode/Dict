using Dict.DTO;

namespace Dict.Models
{
    public class TrieNode
    {
        // Dùng Dictionary để lưu các nhánh con (n, i, h, o, n...)
        public Dictionary<char, TrieNode> Children { get; set; } = new Dictionary<char, TrieNode>();

        // Lưu danh sách gợi ý ngay tại nút để đạt tốc độ O(L)
        public List<AutocompleteSuggestionDto> Suggestions { get; set; } = new List<AutocompleteSuggestionDto>();
    }

    public class TrieAutocompleteCache
    {
        public TrieNode Root { get; } = new TrieNode();
        public bool IsLoaded { get; set; } = false;
    }

}
