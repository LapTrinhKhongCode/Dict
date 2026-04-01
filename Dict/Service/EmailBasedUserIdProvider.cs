using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Dict.Service
{
    public class EmailBasedUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var email = connection.User?.FindFirst("email")?.Value
                     ?? connection.User?.FindFirst(ClaimTypes.Email)?.Value;

            // THÊM DÒNG NÀY ĐỂ KIỂM TRA TRÊN CONSOLE CỦA BACKEND
            if (email != null)
                Console.WriteLine($"[SignalR] User connected with Email: {email}");
            else
                Console.WriteLine("[SignalR] Warning: Could not find Email claim for connection!");

            return email?.ToLower();
        }
    }
}
