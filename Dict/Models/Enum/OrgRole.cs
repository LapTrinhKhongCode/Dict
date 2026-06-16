namespace Dict.Models
{
    public static class OrgRole
    {
        public const string OWNER           = "OWNER";           // Tạo org, billing, transfer ownership
        public const string ADMIN           = "ADMIN";           // Quản lý member, workspace
        public const string MEMBER          = "MEMBER";          // Dùng workspace được assign
        public const string BILLING_MANAGER = "BILLING_MANAGER"; // Chỉ quản lý billing
    }

    public static class OrgPlan
    {
        public const string FREE       = "FREE";       // 3 members, 20 OCR/tháng shared
        public const string TEAM       = "TEAM";       // Unlimited members, 500 OCR/tháng
        public const string ENTERPRISE = "ENTERPRISE"; // Unlimited everything
    }
}
