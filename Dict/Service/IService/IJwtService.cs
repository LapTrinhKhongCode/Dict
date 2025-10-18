using Dict.Models;

namespace Dict.Service.IService
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
