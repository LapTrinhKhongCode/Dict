using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Dict.Service
{
    public class EmailBasedUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Dùng userId (số) làm định danh duy nhất — tránh trùng nếu user đổi email
            return connection.User?.FindFirst("userId")?.Value;
        }
    }
}
