    namespace Dict.DTO.User
{
    public class ChangePasswordDTO
    {
        public string Username { get; set; } = "";
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
