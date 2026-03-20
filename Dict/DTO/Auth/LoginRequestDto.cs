using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.Auth
{

    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

}
