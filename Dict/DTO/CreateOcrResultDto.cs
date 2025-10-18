namespace Dict.DTO
{
    public class CreateOcrResultDto
    {
        public int? PageNumber { get; set; }
        public string WordText { get; set; }
        public string BoundingBox { get; set; }
    }
}
