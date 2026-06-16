namespace Dict.Service.IService
{
    public interface IStripeService
    {
        Task<string> CreateCheckoutSessionAsync(int userId, string priceId, string successUrl, string cancelUrl);
        Task<string> CreateOrgCheckoutSessionAsync(int orgId, int requestingUserId, string priceId, string successUrl, string cancelUrl);
        Task<string> CreateCustomerPortalSessionAsync(int userId, string returnUrl);
        Task<string> CreateOrgPortalSessionAsync(int orgId, int requestingUserId, string returnUrl);
        Task HandleWebhookAsync(string json, string stripeSignature);
    }
}
