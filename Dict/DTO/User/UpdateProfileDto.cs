using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.User
{
    public class UpdateProfileDto
    {
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public bool? IsActive { get; set; }
    }
}
