using Azure.Core;
using Azure.Identity;

namespace Dict.Service
{
    public class AzureTokenProvider
    {
        private readonly ClientSecretCredential _credential;
        private readonly string[] _scopes = new[] { "https://management.azure.com/.default" };

        public AzureTokenProvider(IConfiguration configuration)
        {
            var config = configuration.GetSection("AzureAd");
            var tenantId = config["TenantId"];
            var clientId = config["ClientId"];
            var clientSecret = config["ClientSecret"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentNullException("AzureAd configuration is missing in appsettings.json");
            }

            // 1. Tạo đối tượng credential
            _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        }

        public virtual async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // 2. Lấy token.
                // Hàm này sẽ TỰ ĐỘNG dùng cache.
                // Nó chỉ gọi API tới Azure khi token chưa có hoặc đã hết hạn.
                var context = new TokenRequestContext(_scopes);
                AccessToken token = await _credential.GetTokenAsync(context);

                // 3. Trả về chuỗi token
                return token.Token;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu không thể lấy được token (sai config, mất mạng...)
                Console.WriteLine($"Error getting token: {ex.Message}");
                throw;
            }
        }
    }
}