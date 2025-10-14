namespace Dict.DTO.Deck
{
    public class DeckSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CardCount { get; set; }
        public bool IsPublic { get; set; }
        public string AuthorName { get; set; } = string.Empty; // Tên tác giả
    }

}
