using Dict.DTO.Admin;
using Dict.DTO.User;

namespace Dict.Service.IService
{
    public interface IAdminService
    {
        /// <summary>
        /// Lấy số liệu thống kê tổng quan cho Dashboard.
        /// </summary>
        Task<AdminDashboardStatsDto> GetDashboardStatisticsAsync();
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> SetUserLockStatusAsync(int userId, bool isLocked);
        Task<bool> UpdateUserRoleAsync(int userId, string newRole);
        Task<bool> AdminDeleteDeckAsync(int deckId);
        Task<bool> AdminDeleteUserAsync(int userId);
        Task<HistoricalStatsDto> GetMonthlyGrowthStatisticsAsync();

        /// <summary>
        /// Tìm kiếm và phân trang người dùng.
        /// </summary>
        /// <param name="searchTerm">Từ khóa tìm kiếm (username hoặc email)</param>
        /// <param name="page">Trang hiện tại (bắt đầu từ 1)</param>
        /// <param name="pageSize">Số lượng kết quả mỗi trang</param>
        Task<PaginatedUsersDto> SearchUsersAsync(string? searchTerm, int page, int pageSize);

        /// <summary>
        /// Admin đặt lại mật khẩu cho người dùng.
        /// </summary>
        Task<bool> AdminResetUserPasswordAsync(int userId, string newPassword);

        /// <summary>
        /// Admin ẩn (hoặc hiện) một bộ thẻ bất kỳ (thay đổi IsPublic).
        /// </summary>
        Task<bool> SetDeckVisibilityAsync(int deckId, bool isPublic);
    }
}
