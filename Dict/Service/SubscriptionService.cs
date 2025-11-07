using Dict.Data;
using Dict.DTO; // Namespace for ZaloPay DTOs and ResponseDTO
using Dict.Models; // Namespace for User model
// using Dict.Models.Enum; // <-- XÓA
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
using Microsoft.AspNetCore.Identity; // <-- THÊM

namespace Dict.Service
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context; // Vẫn cần DbContext cho các logic khác (nếu có)
        private readonly ILogger<SubscriptionService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        // 1. THÊM MANAGER
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        // 2. CẬP NHẬT CONSTRUCTOR
        public SubscriptionService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<SubscriptionService> logger,
            IHttpClientFactory httpClientFactory,
            UserManager<ApplicationUser> userManager, // THÊM
            RoleManager<ApplicationRole> roleManager) // THÊM
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userManager = userManager; // GÁN
            _roleManager = roleManager; // GÁN
        }

        public async Task<CreateOrderServiceResult> CreateZaloPayOrderAsync(int userId)
        {
            // (Hàm này giữ nguyên, không thay đổi)
            _logger.LogInformation("--- Starting CreateZaloPayOrderAsync for UserId: {UserId} ---", userId);
            long amount = 1000;
            string itemJson = JsonSerializer.Serialize(new[] {
                new { itemid = "premlife", itemname = "Dict Premium Lifetime", itemprice = amount, itemquantity = 1 }
            });
            string descriptionTemplate = "Dict App - Thanh toán gói Premium trọn đời #{0}";
            var user = await _userManager.FindByIdAsync(userId.ToString());
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
                { "app_id", appId }, { "app_user", appUser }, { "app_time", appTime.ToString() },
                { "amount", amount.ToString() }, { "app_trans_id", appTransId }, { "embed_data", embedDataJson },
                { "item", itemJson }, { "description", description }, { "bank_code", "" },
                { "callback_url", callbackUrl }, { "mac", mac }
            };
            var httpContent = new FormUrlEncodedContent(requestData);
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(createOrderUrl, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ZaloPay Sandbox Response: {Response}", responseString);
            var orderResponse = JsonSerializer.Deserialize<ZaloPayCreateOrderResponse>(responseString);
            if (orderResponse == null)
                throw new Exception("Cannot parse ZaloPay response.");
            if (orderResponse.ReturnCode != 1)
                throw new Exception($"ZaloPay Create Order failed: {orderResponse.ReturnMessage}");

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
            // (Hàm này giữ nguyên, không thay đổi)
            var appId = _configuration["ZaloPay:AppId"];
            var key1 = _configuration["ZaloPay:Key1"];
            var queryUrl = _configuration["ZaloPay:QueryUrl"];
            var dataToSign = $"{appId}|{appTransId}|{key1}";
            var mac = ZaloPayHelper.ComputeHmacSha256(dataToSign, key1);
            var client = _httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "app_id", appId }, { "app_trans_id", appTransId }, { "mac", mac }
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


        // === HÀM ĐÃ SỬA THEO YÊU CẦU MỚI ===
        // Logic: Chỉ THÊM Role "PREMIUM_USER", không XÓA Role "USER"
        private async Task UpgradeUserToPremiumAsync(int userId, string appTransId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found when upgrading: {UserId}", userId);
                return;
            }

            // Dùng đúng tên Role trong DB của bạn
            string premiumRoleName = "PREMIUM_USER";

            // 1. Sửa lỗi Race Condition: Kiểm tra xem user đã là Premium chưa?
            if (await _userManager.IsInRoleAsync(user, premiumRoleName))
            {
                _logger.LogInformation("User {UserId} is already {Role}. Skip upgrade (duplicate call).", userId, premiumRoleName);
                return; // Đã được nâng cấp rồi, không làm gì cả, không báo lỗi.
            }

            // 2. Yêu cầu mới: CHỈ THÊM role "PREMIUM_USER"
            _logger.LogInformation("User {UserId} is NOT {Role}. Proceeding with upgrade...", userId, premiumRoleName);

            // (Chúng ta KHÔNG còn kiểm tra hay xóa Role "USER" nữa)

            var addResult = await _userManager.AddToRoleAsync(user, premiumRoleName);

            if (addResult.Succeeded)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user); // Lưu lại UpdatedAt

                _logger.LogInformation("User {UserId} upgraded to {Role}. AppTransId: {AppTransId}", userId, premiumRoleName, appTransId);
            }
            else
            {
                // Log lỗi nếu vì lý do nào đó không thể THÊM role
                var addErrors = string.Join(",", addResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to add role {Role} for user {UserId}. AddErrors: {AddErrors}",
                    premiumRoleName, userId, addErrors);
            }
        }
    }
}