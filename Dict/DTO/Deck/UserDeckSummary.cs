using Microsoft.AspNetCore.Mvc;

namespace Dict.DTO.Deck
{
    public class UserDeckSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CardCount { get; set; }
        public string Avatar { get; set; }
        // Thêm tên người tạo để hiển thị trên UI
        public string AuthorUsername { get; set; }
    }
}
