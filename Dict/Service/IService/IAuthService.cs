using Dict.DTO;
using Dict.DTO.Auth;
using Dict.Models;

namespace Dict.Service.IService
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegistrationRequestDto request);
        Task<LoginResponseDto> ConfirmEmailAsync(string email, string token);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task LogoutAsync(int userId);
        Task<string> ForgotPasswordAsync(string email);
        Task<string> ResetPasswordAsync(string email, string otp, string newPassword, string confirmPassword);
        Task<string> ResendConfirmationAsync(string email);
        Task<ApplicationUser?> GetUserByIdAsync(int userId);
    }
}
