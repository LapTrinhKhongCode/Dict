namespace Dict.DTO.Deck
{
    public class DeckDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<CardDto> Cards { get; set; }
    }
}
