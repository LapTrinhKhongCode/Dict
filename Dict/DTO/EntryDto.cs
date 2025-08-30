namespace Dict.DTO
{
    public class EntryDto
    {
        public long EntSeq { get; set; }
        public string Type { get; set; }
        public List<string> KanjiForms { get; set; } = new();
        public List<string> Readings { get; set; } = new();
        public List<SenseDto> Senses { get; set; } = new();
    }
}
