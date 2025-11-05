using Dict.Models;
using Dict.Service.IService;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
// ✨ Add this using statement to access ClaimTypes
using System.Security.Claims;

namespace Dict.Service
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // ✨ FIX: Use a List<Claim> to easily add new claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("userId", user.Id.ToString()), // This is correct for your controller's GetAdminId()
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // ✨ FIX: Add the Role claim
                // We cast the enum Role (which is an int) to a string (e.g., "1", "2")
                // This is what [Authorize(Roles = Role.ADMIN)] will check against.
                new Claim(ClaimTypes.Role, ((user.Role).ToString()))
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims, // Pass the list of claims
                                // ✨ FIX: Use DateTime.UtcNow for standardized time
                expires: DateTime.UtcNow.AddHours(240000), // Token expires in 100 days
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
