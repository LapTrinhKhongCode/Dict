using Dict.DTO.Deck;

namespace Dict.DTO.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }

        // ✨ THÊM CÁC TRƯỜNG MỚI ✨
        public string Role { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Danh sách các Deck (tùy chọn, giữ lại nếu cần)
        public IEnumerable<DeckSummaryDto>? Decks { get; set; }
    }   
}
