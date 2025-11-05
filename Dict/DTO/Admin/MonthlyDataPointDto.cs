namespace Dict.DTO.Admin
{
    public class MonthlyDataPointDto
    {
        public string Month { get; set; } // Định dạng "YYYY-MM"
        public int NewUserCount { get; set; }
        public int NewDeckCount { get; set; }
    }
}
