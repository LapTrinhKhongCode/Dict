namespace Dict.DTO
{
    public class AutocompleteSuggestionDto
    {
        public string Word { get; set; } 
        public string Reading { get; set; }
        public string Meaning { get; set; } 
        public int Weight { get; set; } 
    }
}
