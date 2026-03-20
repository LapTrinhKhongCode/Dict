using System.ComponentModel.DataAnnotations;

namespace Dict.DTO.User // (Hoặc namespace DTO của bạn)
{
    public class UpdateUserDto
    {
        //public string? Username { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }

        // Quan trọng: kiểu IFormFile để nhận file upload
        public IFormFile? AvatarUrl { get; set; }
    }
}