using Dict.Models;
using System.Collections.Generic; // Thêm

namespace Dict.Service.IService
{
    public interface IJwtService
    {
        // Cập nhật chữ ký: Thêm tham số IList<string> roles
        string GenerateToken(ApplicationUser user, IList<string> roles);
    }
}