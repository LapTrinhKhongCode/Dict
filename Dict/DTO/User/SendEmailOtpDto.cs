using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.User
{
    public class SendEmailOtpDto
    {
        public string NewEmail { get; set; } = null!;
    }
}
