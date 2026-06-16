namespace Dict.Models
{
    public class OrganizationMember
    {
        public int OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>OWNER | ADMIN | MEMBER | BILLING_MANAGER</summary>
        public string OrgRole { get; set; } = "MEMBER";

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
