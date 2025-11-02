using Dict.DTO.User;

namespace Dict.Service.IService
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserResponseDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<UserDto>> SearchUsersByUsernameAsync(string username);
        Task<UserResponseDto> UpdateUserByUsernameAsync(string username, UpdateUserDto updateUserDto);
        Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword);
    }
}
