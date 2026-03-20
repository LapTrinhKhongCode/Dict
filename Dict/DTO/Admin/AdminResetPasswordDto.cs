namespace Dict.DTO.Admin
{
    public class AdminResetPasswordDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(8)]
        public string NewPassword { get; set; }
    }
}
