using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.User
{
    public class UpdateUserDto
    {
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public bool? IsActive { get; set; }
    }
}
