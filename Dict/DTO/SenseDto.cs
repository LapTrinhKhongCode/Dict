namespace Dict.DTO
{
    public class SenseDto
    {
        public string Pos { get; set; }
        public string Field { get; set; }
        public string Misc { get; set; }
        public string SInf { get; set; }
        public int SenseOrder { get; set; }
        public List<string> Glosses { get; set; } = new();
        public List<ExampleDto> Examples { get; set; } = new();
    }
}
