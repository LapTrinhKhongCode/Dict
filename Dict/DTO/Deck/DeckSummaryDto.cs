namespace Dict.DTO.Deck
{
    public class DeckSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public int CardCount { get; set; }
        public string AuthorName { get; set; }

    }

}
