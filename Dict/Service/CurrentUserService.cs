using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Dict.Service.IService;

namespace Dict.Service
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                // ✅ CHỈ ĐỊNH ĐÚNG TÊN CLAIM LÀ "userId" (Giống y hệt trong JwtService)
                var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId");
                
                // Nếu lấy được thì ép kiểu sang Int
                if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
                {
                    return userId;
                }
                
                return 0;
            }
        }

        public int WorkspaceId
        {
            get
            {
                // Parse ID của Công ty từ Token
                var wsClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("WorkspaceId");
                return wsClaim != null ? int.Parse(wsClaim.Value) : 0;
            }
        }
    }
}