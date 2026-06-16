using System.ComponentModel.DataAnnotations;

namespace Dict.Models
{
    public class Organization
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(100)]
        public string Slug { get; set; } = "";  // URL-friendly name

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>FREE | TEAM | ENTERPRISE</summary>
        [MaxLength(50)]
        public string OrgPlan { get; set; } = "FREE";

        /// <summary>Stripe Customer ID cho Org billing</summary>
        public string? StripeCustomerId { get; set; }

        /// <summary>Stripe Subscription ID cho Org</summary>
        public string? StripeSubscriptionId { get; set; }

        /// <summary>Max số member. null = unlimited (ENTERPRISE)</summary>
        public int? MaxMembers { get; set; } = 5;

        public int OwnerId { get; set; }
        public virtual ApplicationUser Owner { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<OrganizationMember> Members { get; set; } = new HashSet<OrganizationMember>();
        public virtual ICollection<Workspace> Workspaces { get; set; } = new HashSet<Workspace>();
    }
}
