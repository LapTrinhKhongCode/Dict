namespace Dict.DTO.Deck
{
    public class DeckCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public List<CardCreateDto>? Cards { get; set; }
    }
}