// Dict/DTO/Deck/CardCreateDto.cs

namespace Dict.DTO.Deck
{
    public class CardCreateDto
    {
        public string FrontText { get; set; }
        public string BackText { get; set; }

        // ✨ SỬA: Thêm thuộc tính Tags (nullable) để khớp với logic trong Service
        // Dấu '?' cho phép frontend không cần gửi giá trị này nếu không có tag.
        public string? Tags { get; set; }
    }
}