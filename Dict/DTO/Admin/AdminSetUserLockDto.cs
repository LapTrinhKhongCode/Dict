namespace Dict.DTO.Admin
{
    public class AdminSetUserLockDto
    {
        /// <summary>
        /// true = khóa (IsActive = false), false = mở khóa (IsActive = true)
        /// </summary>
        public bool IsLocked { get; set; }
    }
}
