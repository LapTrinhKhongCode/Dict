namespace Dict.Models
{
    public static class WorkspaceRole
    {
        public const string OWNER  = "OWNER";   // Tao workspace, full quyen, transfer ownership
        public const string ADMIN  = "ADMIN";   // Moi/xoa member, quan ly project
        public const string MEMBER = "MEMBER";  // Tao/sua trong project duoc assign
        public const string VIEWER = "VIEWER";  // Chi doc
    }
}
