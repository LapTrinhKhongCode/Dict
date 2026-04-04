using Dict.DTO;
using System.Runtime.InteropServices;

namespace Dict.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FlatTrieNode
    {
        public char Character;        // 2 bytes
        public int FirstChildIndex;   // 4 bytes: Index con đầu tiên
        public int NextSiblingIndex;  // 4 bytes: Index thằng em kế bên
        public int SuggestionOffset;  // 4 bytes: Vị trí bắt đầu trong mảng Suggestion lớn
        public short SuggestionCount; // 2 bytes: Số lượng gợi ý tại nút này
    } // Tổng cộng: 16 bytes/node (Cực nhẹ!)

    public class TrieAutocompleteCache
    {
        // Toàn bộ cây nằm trong mảng này
        public FlatTrieNode[] NodePool { get; set; } = Array.Empty<FlatTrieNode>();

        // Chứa tất cả các gợi ý để các Node trỏ vào
        public AutocompleteSuggestionDto[] SuggestionPool { get; set; } = Array.Empty<AutocompleteSuggestionDto>();

        public bool IsLoaded { get; set; } = false;
        public int RootIndex => 0;
    }

}
