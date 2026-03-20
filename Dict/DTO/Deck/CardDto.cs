namespace Dict.DTO.Deck
{
    public class CardDto
    {
        public int Id { get; set; }
        public string CharBig { get; set; }
        public string Pinyin { get; set; }
        public string Meaning { get; set; }
        // Thêm trường này để frontend biết khi nào cần ôn tập
        public DateTime NextReviewAt { get; set; }
    }
}
