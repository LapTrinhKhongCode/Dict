namespace Dict.Enums
{
    public enum InvitationStatus
    {
        PENDING = 0,    // Đang chờ user duyệt
        ACCEPTED = 1,   // Đã đồng ý
        DECLINED = 2,   // Đã từ chối
        REVOKED = 3     // Admin rút lại lời mời
    }
}