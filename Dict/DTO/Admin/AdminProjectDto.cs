namespace Dict.DTO.Admin
{
    public class AdminProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int WorkspaceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
