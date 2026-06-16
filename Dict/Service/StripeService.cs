using Dict.Data;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Dict.Service
{
    public class StripeService : IStripeService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<StripeService> _logger;

        public StripeService(ApplicationDbContext db, IConfiguration config, ILogger<StripeService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSessionAsync(int userId, string priceId, string successUrl, string cancelUrl)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found");

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerSvc = new CustomerService();
                var customer = await customerSvc.CreateAsync(new CustomerCreateOptions
                {
                    Email = user.Email,
                    Name = user.UserName,
                    Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
                });
                user.StripeCustomerId = customer.Id;
                await _db.SaveChangesAsync();
            }

            var sessionSvc = new SessionService();
            var session = await sessionSvc.CreateAsync(new SessionCreateOptions
            {
                Customer = user.StripeCustomerId,
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = priceId, Quantity = 1 }
                },
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() },
                AllowPromotionCodes = true,
            });

            return session.Url;
        }

        public async Task<string> CreateCustomerPortalSessionAsync(int userId, string returnUrl)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found");
            if (string.IsNullOrEmpty(user.StripeCustomerId))
                throw new InvalidOperationException("User has no Stripe customer");

            var portalSvc = new Stripe.BillingPortal.SessionService();
            var session = await portalSvc.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = user.StripeCustomerId,
                ReturnUrl = returnUrl,
            });
            return session.Url;
        }

        // ── Org Checkout ─────────────────────────────────────────────────
        public async Task<string> CreateOrgCheckoutSessionAsync(int orgId, int requestingUserId, string priceId, string successUrl, string cancelUrl)
        {
            var org = await _db.Organizations.Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == orgId)
                ?? throw new KeyNotFoundException("Organization not found");

            // Chỉ OWNER/ADMIN của org mới được upgrade billing
            var membership = org.Members.FirstOrDefault(m => m.UserId == requestingUserId);
            if (membership == null || (membership.OrgRole != Models.OrgRole.OWNER && membership.OrgRole != Models.OrgRole.ADMIN))
                throw new UnauthorizedAccessException("Chỉ Owner/Admin của tổ chức mới có thể thay đổi plan.");

            // Tạo Stripe Customer cho Org nếu chưa có
            if (string.IsNullOrEmpty(org.StripeCustomerId))
            {
                var user = await _db.Users.FindAsync(requestingUserId);
                var customerSvc = new CustomerService();
                var customer = await customerSvc.CreateAsync(new CustomerCreateOptions
                {
                    Email = user?.Email,
                    Name = org.Name,
                    Metadata = new Dictionary<string, string> { ["orgId"] = orgId.ToString(), ["type"] = "organization" }
                });
                org.StripeCustomerId = customer.Id;
                await _db.SaveChangesAsync();
            }

            var sessionSvc = new SessionService();
            var session = await sessionSvc.CreateAsync(new SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = priceId, Quantity = 1 }
                },
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string> { ["orgId"] = orgId.ToString(), ["type"] = "organization" },
                AllowPromotionCodes = true,
            });
            return session.Url;
        }

        public async Task<string> CreateOrgPortalSessionAsync(int orgId, int requestingUserId, string returnUrl)
        {
            var org = await _db.Organizations.Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == orgId)
                ?? throw new KeyNotFoundException("Organization not found");

            var membership = org.Members.FirstOrDefault(m => m.UserId == requestingUserId);
            if (membership == null || membership.OrgRole == Models.OrgRole.MEMBER)
                throw new UnauthorizedAccessException("Chỉ Owner/Admin mới quản lý được billing.");

            if (string.IsNullOrEmpty(org.StripeCustomerId))
                throw new InvalidOperationException("Tổ chức chưa có subscription.");

            var portalSvc = new Stripe.BillingPortal.SessionService();
            var session = await portalSvc.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                ReturnUrl = returnUrl,
            });
            return session.Url;
        }

        public async Task HandleWebhookAsync(string json, string stripeSignature)
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json, stripeSignature, webhookSecret,
                    throwOnApiVersionMismatch: false  // webhook endpoint dùng API version cũ hơn Stripe.net
                );
            }
            catch (StripeException ex)
            {
                _logger.LogWarning("Stripe webhook signature invalid: {Msg}", ex.Message);
                throw;
            }

            _logger.LogInformation("Stripe event: {Type}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    var session = stripeEvent.Data.Object as Session;
                    // Check type: org or personal
                    if (session?.Metadata?.TryGetValue("type", out var sessionType) == true && sessionType == "organization")
                        await HandleOrgCheckoutCompleted(session);
                    else
                        await HandleCheckoutCompleted(session);
                    break;
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    await HandleSubscriptionChange(stripeEvent.Data.Object as Stripe.Subscription, stripeEvent.Type);
                    break;
                case "invoice.payment_failed":
                    _logger.LogWarning("Invoice payment failed: {Json}", json[..Math.Min(200, json.Length)]);
                    break;
            }
        }

        private async Task HandleCheckoutCompleted(Session? session)
        {
            if (session == null) return;
            if (!session.Metadata.TryGetValue("userId", out var userIdStr) || !int.TryParse(userIdStr, out var userId)) return;

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;

            user.StripeSubscriptionId = session.SubscriptionId;
            user.PersonalTier = "PREMIUM";
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(session.SubscriptionId))
            {
                var subSvc = new Stripe.SubscriptionService();
                var sub = await subSvc.GetAsync(session.SubscriptionId);
                user.PremiumExpiresAt = GetPeriodEnd(sub);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} upgraded to PREMIUM", userId);
        }

        private async Task HandleOrgCheckoutCompleted(Session? session)
        {
            if (session == null) return;
            if (!session.Metadata.TryGetValue("orgId", out var orgIdStr) || !int.TryParse(orgIdStr, out var orgId)) return;

            var org = await _db.Organizations.FindAsync(orgId);
            if (org == null) return;

            org.StripeSubscriptionId = session.SubscriptionId;

            // Determine plan from price — lookup from subscription
            if (!string.IsNullOrEmpty(session.SubscriptionId))
            {
                var subSvc = new Stripe.SubscriptionService();
                var sub = await subSvc.GetAsync(session.SubscriptionId,
                    new SubscriptionGetOptions { Expand = new List<string> { "items.data.price" } });
                var priceId = sub.Items?.Data?.FirstOrDefault()?.Price?.Id;

                org.OrgPlan = priceId == _config["Stripe:Prices:OrgEnterprise"]
                    ? Models.OrgPlan.ENTERPRISE
                    : Models.OrgPlan.TEAM;
                org.MaxMembers = null; // Unlimited for paid plans
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Org {OrgId} upgraded to {Plan}", orgId, org.OrgPlan);
        }

        private async Task HandleSubscriptionChange(Stripe.Subscription? sub, string eventType)
        {
            if (sub == null) return;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.StripeSubscriptionId == sub.Id);
            if (user == null) return;

            if (eventType == "customer.subscription.deleted" || sub.Status == "canceled")
            {
                user.PersonalTier = "FREE";
                user.StripeSubscriptionId = null;
                user.PremiumExpiresAt = null;
                _logger.LogInformation("User {UserId} downgraded to FREE", user.Id);
            }
            else if (sub.Status == "active" || sub.Status == "trialing")
            {
                user.PersonalTier = "PREMIUM";
                user.PremiumExpiresAt = GetPeriodEnd(sub);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Helper: lấy end date tương thích với Stripe.net v47+
        private static DateTime? GetPeriodEnd(Stripe.Subscription sub)
        {
            // v47+: CurrentPeriodEnd nằm trực tiếp trên Subscription
            try { return (DateTime?)typeof(Stripe.Subscription).GetProperty("CurrentPeriodEnd")?.GetValue(sub); }
            catch { return null; }
        }
    }
}
