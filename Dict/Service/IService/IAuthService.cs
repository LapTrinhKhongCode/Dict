using Dict.DTO;
using Dict.DTO.Auth;
using Dict.Models;

namespace Dict.Service.IService
{
    public interface IAuthService
    {
        // ✨ THAY ĐỔI: Trả về string (tin nhắn) thay vì User
        Task<string> RegisterAsync(RegistrationRequestDto request);

        // ✨ THÊM MỚI: Trả về LoginResponseDto (token) sau khi xác thực thành công
        Task<LoginResponseDto> VerifyEmailAsync(VerifyEmailDto verifyDto);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task LogoutAsync(int userId);
    }
}
