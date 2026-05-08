namespace Dict.DTO
{
    public class ProjectFileDto
    {
        public int Id { get; set; } // Trả về Id của OcrJob để FE làm link bấm vào xem chi tiết
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ImageUrl { get; set; }
    }
}
