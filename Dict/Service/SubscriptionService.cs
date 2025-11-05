// File: Service/SubscriptionService.cs
using Dict.Data;
using Dict.DTO; // Namespace for ZaloPay DTOs and ResponseDTO
using Dict.Models; // Namespace for User model
using Dict.Models.Enum;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Required for .Any()
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dict.Service
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SubscriptionService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<SubscriptionService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        //    public async Task<CreateOrderServiceResult> CreateZaloPayOrderAsync(int userId)
        //    {
        //        _logger.LogInformation("--- Starting CreateZaloPayOrderAsync for UserId: {UserId} ---", userId);

        //        long amount = 10;
        //        string itemJson = JsonSerializer.Serialize(new[] {
        //    new { itemid = "premlife", itemname = "Dict Premium Lifetime", itemprice = amount, itemquantity = 1 }
        //});
        //        string descriptionTemplate = "Dict App - Thanh toan goi Premium tron doi #{0}";

        //        var user = await _context.Users.FindAsync(userId);
        //        if (user == null)
        //            throw new KeyNotFoundException("User not found.");

        //        var appId = _configuration["ZaloPay:AppId"];
        //        var key1 = _configuration["ZaloPay:Key1"];
        //        var createOrderUrl = _configuration["ZaloPay:CreateOrderUrl"];
        //        var callbackUrl = _configuration["ZaloPay:CallbackUrl"];

        //        if (new[] { appId, key1, createOrderUrl, callbackUrl }.Any(string.IsNullOrEmpty))
        //            throw new InvalidOperationException("ZaloPay config missing.");

        //        var vietnamTime = DateTime.UtcNow.AddHours(7);
        //        var appTransId = $"{vietnamTime:yyMMdd}_{Guid.NewGuid():N}".Substring(0, 18);
        //        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        //        var appUser = user.Username ?? $"user_{userId}";
        //        var description = string.Format(descriptionTemplate, appTransId);
        //        var embedData = new ZaloPayEmbedData { UserId = userId };
        //        var embedDataJson = JsonSerializer.Serialize(embedData);

        //        var dataToSign = $"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedDataJson}|{itemJson}";
        //        var mac = ZaloPayHelper.ComputeHmacSha256(dataToSign, key1);

        //        var requestData = new Dictionary<string, string>
        //{
        //    { "app_id", appId },
        //    { "app_user", appUser },
        //    { "app_time", appTime.ToString() },
        //    { "amount", amount.ToString() },
        //    { "app_trans_id", appTransId },
        //    { "embed_data", embedDataJson },
        //    { "item", itemJson },
        //    { "description", description },
        //    { "bank_code", "" },
        //    { "callback_url", callbackUrl },
        //    { "mac", mac }
        //};

        //        var httpContent = new FormUrlEncodedContent(requestData);
        //        var client = _httpClientFactory.CreateClient();

        //        _logger.LogInformation("Sending request to ZaloPay Sandbox: {Url}", createOrderUrl);
        //        var response = await client.PostAsync(createOrderUrl, httpContent);
        //        var responseString = await response.Content.ReadAsStringAsync();

        //        _logger.LogInformation("ZaloPay Sandbox Response: {Response}", responseString);

        //        var orderResponse = JsonSerializer.Deserialize<ZaloPayCreateOrderResponse>(responseString);
        //        if (orderResponse == null)
        //            throw new Exception("Cannot parse ZaloPay response.");

        //        if (orderResponse.ReturnCode != 1)
        //            throw new Exception($"ZaloPay Create Order failed: {orderResponse.ReturnMessage}");

        //        // ✅ Vì sandbox không có callback, tự xử lý nâng cấp luôn tại đây
        //        _logger.LogInformation("Sandbox mode detected — processing premium upgrade immediately for user {UserId}", userId);
        //        //await UpgradeUserToPremiumAsync(userId, appTransId);

        //        return new CreateOrderServiceResult
        //        {
        //            OrderUrl = orderResponse.OrderUrl,
        //            ZpTransToken = orderResponse.ZpTransToken
        //        };
        //    }
        public async Task<CreateOrderServiceResult> CreateZaloPayOrderAsync(int userId)
        {
            _logger.LogInformation("--- Starting CreateZaloPayOrderAsync for UserId: {UserId} ---", userId);

            long amount = 1000; // ví dụ gói premium lifetime
            string itemJson = JsonSerializer.Serialize(new[] {
        new { itemid = "premlife", itemname = "Dict Premium Lifetime", itemprice = amount, itemquantity = 1 }
    });
            string descriptionTemplate = "Dict App - Thanh toán gói Premium trọn đời #{0}";

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var appId = _configuration["ZaloPay:AppId"];
            var key1 = _configuration["ZaloPay:Key1"];
            var createOrderUrl = _configuration["ZaloPay:CreateOrderUrl"];
            var callbackUrl = _configuration["ZaloPay:CallbackUrl"];

            if (new[] { appId, key1, createOrderUrl, callbackUrl }.Any(string.IsNullOrEmpty))
                throw new InvalidOperationException("ZaloPay config missing.");

            var vietnamTime = DateTime.UtcNow.AddHours(7);
            var appTransId = $"{vietnamTime:yyMMdd}_{Guid.NewGuid():N}".Substring(0, 18);
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var appUser = user.UserName ?? $"user_{userId}";
            var description = string.Format(descriptionTemplate, appTransId);
            var embedData = new ZaloPayEmbedData { UserId = userId };
            var embedDataJson = JsonSerializer.Serialize(embedData);

            var dataToSign = $"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedDataJson}|{itemJson}";
            var mac = ZaloPayHelper.ComputeHmacSha256(dataToSign, key1);

            var requestData = new Dictionary<string, string>
    {
        { "app_id", appId },
        { "app_user", appUser },
        { "app_time", appTime.ToString() },
        { "amount", amount.ToString() },
        { "app_trans_id", appTransId },
        { "embed_data", embedDataJson },
        { "item", itemJson },
        { "description", description },
        { "bank_code", "" },
        { "callback_url", callbackUrl },
        { "mac", mac }
    };

            var httpContent = new FormUrlEncodedContent(requestData);
            var client = _httpClientFactory.CreateClient();

            _logger.LogInformation("Sending request to ZaloPay Sandbox: {Url}", createOrderUrl);
            var response = await client.PostAsync(createOrderUrl, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("ZaloPay Sandbox Response: {Response}", responseString);

            var orderResponse = JsonSerializer.Deserialize<ZaloPayCreateOrderResponse>(responseString);
            if (orderResponse == null)
                throw new Exception("Cannot parse ZaloPay response.");

            if (orderResponse.ReturnCode != 1)
                throw new Exception($"ZaloPay Create Order failed: {orderResponse.ReturnMessage}");

            // ❌ Không upgrade ngay tại đây — chờ người dùng quét mã và thanh toán thành công
            // ✅ Lưu appTransId để đối chiếu sau này (tuỳ theo hệ thống anh muốn lưu)
            _logger.LogInformation("Order created successfully for user {UserId}. AppTransId: {AppTransId}", userId, appTransId);

            return new CreateOrderServiceResult
            {
                OrderUrl = orderResponse.OrderUrl,
                ZpTransToken = orderResponse.ZpTransToken,
                AppTransId = appTransId
            };
        }
        public async Task<bool> QueryOrderAsync(string appTransId, int userId)
        {
            var appId = _configuration["ZaloPay:AppId"];
            var key1 = _configuration["ZaloPay:Key1"];
            var queryUrl = _configuration["ZaloPay:QueryUrl"];

            var dataToSign = $"{appId}|{appTransId}|{key1}";
            var mac = ZaloPayHelper.ComputeHmacSha256(dataToSign, key1);

            var client = _httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "app_id", appId },
        { "app_trans_id", appTransId },
        { "mac", mac }
    });

            var response = await client.PostAsync(queryUrl, form);
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ZaloPay Query Response for {AppTransId}: {Response}", appTransId, responseString);

            var queryResponse = JsonSerializer.Deserialize<ZaloPayQueryResponse>(responseString);
            if (queryResponse?.return_code == 1)
            {
                _logger.LogInformation("Payment success confirmed for user {UserId}, upgrading...", userId);
                await UpgradeUserToPremiumAsync(userId, appTransId);
                return true;
            }

            _logger.LogWarning("Payment not completed yet for AppTransId {AppTransId}", appTransId);
            return false;
        }



        private async Task UpgradeUserToPremiumAsync(int userId, string appTransId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found when upgrading: {UserId}", userId);
                return;
            }

            //if (user.Role == Role.USER)
            //{
            //    user.Role = Role.PREMIUM_USER;
            //    user.UpdatedAt = DateTime.UtcNow;
            //    await _context.SaveChangesAsync();

            //    _logger.LogInformation("User {UserId} upgraded to PREMIUM_USER (Sandbox immediate upgrade). AppTransId: {AppTransId}", userId, appTransId);
            //}
            //else
            //{
            //    _logger.LogInformation("User {UserId} already has role {Role}. Skip upgrade.", userId, user.Role);
            //}
        }

    }
}