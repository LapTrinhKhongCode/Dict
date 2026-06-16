using Dict.DTO;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripe;
        private readonly IConfiguration _config;
        private readonly ILogger<StripeController> _logger;

        public StripeController(IStripeService stripe, IConfiguration config, ILogger<StripeController> logger)
        {
            _stripe = stripe;
            _config = config;
            _logger = logger;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim == null || !int.TryParse(claim.Value, out var id))
                throw new InvalidOperationException("Invalid user");
            return id;
        }

        // ── Tạo Stripe Checkout Session ───────────────────────────────────
        /// <summary>
        /// Tạo Checkout Session cho Personal Premium subscription.
        /// Frontend redirect user đến session.url
        /// </summary>
        [Authorize]
        [HttpPost("create-checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest req)
        {
            try
            {
                var userId = GetUserId();
                var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
                var priceId = req.PriceId ?? _config["Stripe:Prices:PersonalPremiumMonthly"]!;
                if (string.IsNullOrEmpty(priceId))
                    priceId = _config["Stripe:Prices:PersonalPremiumMonthly"]!;

                var url = await _stripe.CreateCheckoutSessionAsync(
                    userId,
                    priceId,
                    successUrl: $"{frontendUrl}/premium/success",
                    cancelUrl: $"{frontendUrl}/premium"
                );

                return Ok(new ResponseDTO { Result = new { url }, Message = "Checkout session created" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create checkout session");
                return StatusCode(500, new ResponseDTO { IsSuccess = false, Message = ex.Message });
            }
        }

        // ── Customer Portal (quản lý billing) ─────────────────────────────
        /// <summary>
        /// Tạo Stripe Customer Portal session để user quản lý subscription.
        /// User có thể cancel, update payment method, xem invoice.
        /// </summary>
        [Authorize]
        [HttpPost("portal")]
        public async Task<IActionResult> CreatePortal()
        {
            try
            {
                var userId = GetUserId();
                var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";

                var url = await _stripe.CreateCustomerPortalSessionAsync(userId, returnUrl: $"{frontendUrl}/premium");
                return Ok(new ResponseDTO { Result = new { url } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create portal session");
                return StatusCode(500, new ResponseDTO { IsSuccess = false, Message = ex.Message });
            }
        }

        // ── Org Checkout ──────────────────────────────────────────────────
        [Authorize]
        [HttpPost("create-org-checkout")]
        public async Task<IActionResult> CreateOrgCheckout([FromBody] CreateOrgCheckoutRequest req)
        {
            try
            {
                var userId = GetUserId();
                var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
                var priceId = req.Plan == "enterprise"
                    ? _config["Stripe:Prices:OrgEnterprise"]!
                    : _config["Stripe:Prices:OrgTeam"]!;

                var url = await _stripe.CreateOrgCheckoutSessionAsync(
                    req.OrgId, userId, priceId,
                    successUrl: $"{frontendUrl}/org/success?orgId={req.OrgId}",
                    // Note: Stripe appends ?session_id= — frontend ignores it
                    cancelUrl: $"{frontendUrl}/org"
                );
                return Ok(new ResponseDTO { Result = new { url } });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create org checkout");
                return StatusCode(500, new ResponseDTO { IsSuccess = false, Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("org-portal/{orgId}")]
        public async Task<IActionResult> CreateOrgPortal(int orgId)
        {
            try
            {
                var userId = GetUserId();
                var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
                var url = await _stripe.CreateOrgPortalSessionAsync(orgId, userId, returnUrl: $"{frontendUrl}/org");
                return Ok(new ResponseDTO { Result = new { url } });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new ResponseDTO { IsSuccess = false, Message = ex.Message }); }
        }

        // ── Stripe Webhook ────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // Stripe requires raw body — must read before any middleware consumes it
            HttpContext.Request.EnableBuffering();
            var json = await new StreamReader(HttpContext.Request.Body, leaveOpen: true).ReadToEndAsync();
            HttpContext.Request.Body.Position = 0;

            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(stripeSignature))
                return BadRequest("Missing Stripe-Signature header");

            try
            {
                await _stripe.HandleWebhookAsync(json, stripeSignature);
                return Ok();
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogWarning("Stripe webhook signature invalid: {Msg}", ex.Message);
                return BadRequest("Invalid signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook handler error");
                return StatusCode(500);
            }
        }
    }

    public class CreateCheckoutRequest
    {
        public string? PriceId { get; set; }
    }

    public class CreateOrgCheckoutRequest
    {
        public int OrgId { get; set; }
        /// <summary>team | enterprise</summary>
        public string Plan { get; set; } = "team";
    }
}
