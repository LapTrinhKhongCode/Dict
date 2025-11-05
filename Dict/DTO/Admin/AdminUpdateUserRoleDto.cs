using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.Admin
{
    public class AdminUpdateUserRoleDto
    {
        [Required]
        public string Role { get; set; } // "USER", "PREMIUM_USER", "MODERATOR", "ADMIN"
    }
}