using Docker.DotNet.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public bool IsActive { get; set; } = true;
        public string AvatarUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ── Stripe Billing ────────────────────────────────────────────────
        /// <summary>Stripe Customer ID (cus_xxx) — tạo khi user đăng ký hoặc lần đầu checkout</summary>
        public string? StripeCustomerId { get; set; }

        /// <summary>Stripe Subscription ID (sub_xxx) — null nếu chưa subscribe</summary>
        public string? StripeSubscriptionId { get; set; }

        /// <summary>Plan hiện tại: FREE | PREMIUM</summary>
        public string PersonalTier { get; set; } = "FREE";

        /// <summary>Ngày hết hạn Premium — null nếu FREE hoặc lifetime</summary>
        public DateTime? PremiumExpiresAt { get; set; }
        // ─────────────────────────────────────────────────────────────────

        public virtual ICollection<Deck> Decks { get; set; }
        public virtual ICollection<CardState> CardStates { get; set; }
        public virtual ICollection<ReviewLog> ReviewLogs { get; set; }
        public virtual ICollection<MediaStore> MediaStore { get; set; }
        public virtual ICollection<OcrJob> OcrJobs { get; set; }

        public ApplicationUser()
        {
            Decks = new HashSet<Deck>();
            CardStates = new HashSet<CardState>();
            ReviewLogs = new HashSet<ReviewLog>();
            MediaStore = new HashSet<MediaStore>();
            OcrJobs = new HashSet<OcrJob>();
        }

        // Helper
        public bool IsPremiumActive() =>
            PersonalTier == "PREMIUM" &&
            (PremiumExpiresAt == null || PremiumExpiresAt > DateTime.UtcNow);
    }
}
