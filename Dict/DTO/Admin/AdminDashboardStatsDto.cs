namespace Dict.DTO.Admin
{
    public class AdminDashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int TotalDecks { get; set; }
        public int NewDecksThisMonth { get; set; }
        public int TotalPremiumUsers { get; set; }
        public int TotalOcrJobsThisMonth { get; set; }
        public double TotalStorageUsedMb { get; set; }
    }
}
