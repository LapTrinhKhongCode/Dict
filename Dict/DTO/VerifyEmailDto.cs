using System.ComponentModel.DataAnnotations;

namespace Dict.DTO
{
    public class VerifyEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Length(4, 4)]
        public string Code { get; set; }
    }   
}
