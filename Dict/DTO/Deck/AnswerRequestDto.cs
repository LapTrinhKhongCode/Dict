namespace Dict.DTO.Deck
{
    public class AnswerRequestDto
    {
        public int CardId { get; set; }
        // 1: Again, 2: Hard, 3: Good, 4: Easy
        public int Quality { get; set; }
    }
}
