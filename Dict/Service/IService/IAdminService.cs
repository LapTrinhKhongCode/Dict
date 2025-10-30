using Dict.DTO.Admin;
using Dict.DTO.User;

namespace Dict.Service.IService
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatisticsAsync();
        Task<PaginatedUsersDto> SearchUsersAsync(string? searchTerm, int page, int pageSize);
        Task<HistoricalStatsDto> GetMonthlyGrowthStatisticsAsync();
        Task<bool> AdminResetUserPasswordAsync(int userId, string newPassword);
        Task<bool> SetDeckVisibilityAsync(int deckId, bool isPublic);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> SetUserLockStatusAsync(int userId, bool isLocked);

        // Chữ ký đã thay đổi
        Task<bool> UpdateUserRolesAsync(int userId, List<string> newRoleNames);

        Task<bool> AdminDeleteDeckAsync(int deckId);
        Task<bool> AdminDeleteUserAsync(int userId);
    }
}
