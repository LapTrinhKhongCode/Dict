using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.User
{
    public class VerifyEmailOtpDto
    {
        public string NewEmail { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }
}
