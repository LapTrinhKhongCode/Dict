using System.Text.Json.Serialization;
namespace Dict.DTO
{
    //public class ZaloPayCheckoutRequestDto { [JsonPropertyName("planType")] public string PlanType { get; set; } }
    public class ZaloPayCreateOrderRequest { [JsonPropertyName("app_id")] public string AppId { get; set; } [JsonPropertyName("app_trans_id")] public string AppTransId { get; set; } [JsonPropertyName("app_user")] public string AppUser { get; set; } [JsonPropertyName("app_time")] public long AppTime { get; set; } [JsonPropertyName("amount")] public long Amount { get; set; } [JsonPropertyName("item")] public string Item { get; set; } [JsonPropertyName("description")] public string Description { get; set; } [JsonPropertyName("embed_data")] public string EmbedData { get; set; } [JsonPropertyName("bank_code")] public string BankCode { get; set; } = ""; [JsonPropertyName("callback_url")] public string CallbackUrl { get; set; } [JsonPropertyName("mac")] public string Mac { get; set; } }
    public class ZaloPayCreateOrderResponse { [JsonPropertyName("return_code")] public int ReturnCode { get; set; } [JsonPropertyName("return_message")] public string ReturnMessage { get; set; } [JsonPropertyName("order_url")] public string OrderUrl { get; set; } [JsonPropertyName("zp_trans_token")] public string ZpTransToken { get; set; } }
    public class ZaloPayCallbackData { [JsonPropertyName("app_id")] public int AppId { get; set; } [JsonPropertyName("app_trans_id")] public string AppTransId { get; set; } [JsonPropertyName("app_time")] public long AppTime { get; set; } [JsonPropertyName("app_user")] public string AppUser { get; set; } [JsonPropertyName("amount")] public long Amount { get; set; } [JsonPropertyName("embed_data")] public string EmbedData { get; set; } [JsonPropertyName("item")] public string Item { get; set; } [JsonPropertyName("zp_trans_id")] public long ZpTransId { get; set; } [JsonPropertyName("server_time")] public long ServerTime { get; set; } [JsonPropertyName("channel")] public int Channel { get; set; } [JsonPropertyName("merchant_user_id")] public string MerchantUserId { get; set; } [JsonPropertyName("user_fee_amount")] public long UserFeeAmount { get; set; } [JsonPropertyName("discount_amount")] public long DiscountAmount { get; set; } }
    public class ZaloPayEmbedData
    {
        [JsonPropertyName("user_id")] public int UserId { get; set; }
        // PlanType is removed as it's always lifetime
    }
    public class CreateOrderServiceResult { public string OrderUrl { get; set; } public string ZpTransToken { get; set; } public string AppTransId { get; set; } }
    public class ZaloPayCallbackResponse { [JsonPropertyName("return_code")] public int ReturnCode { get; set; } [JsonPropertyName("return_message")] public string ReturnMessage { get; set; } }
}