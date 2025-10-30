// File: Service/IService/ISubscriptionService.cs
using Dict.DTO;
namespace Dict.Service.IService { 
    public interface ISubscriptionService {
        Task<CreateOrderServiceResult> CreateZaloPayOrderAsync(int userId);
        //Task<ZaloPayCallbackResponse> HandleZaloPayCallbackAsync(string jsonCallbackBody);
        Task<bool> QueryOrderAsync(string appTransId, int userId);
    } 
}