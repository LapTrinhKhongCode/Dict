namespace Dict.DTO.Admin
{
    public class DeckAdminDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool? IsPublic { get; set; }
        public int CardCount { get; set; }
        public int? AuthorId { get; set; }
        public string AuthorName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
