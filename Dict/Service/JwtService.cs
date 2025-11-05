using Dict.Models;
using Dict.Service.IService;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic; // Thêm
using System.Linq; // Thêm

namespace Dict.Service
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        // 1. CẬP NHẬT CHỮ KÝ: (Đã đúng)
        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 2. DÙNG List<Claim>
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("userId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // << --- ĐÃ XÓA KHỐI CODE "user.Role" BỊ LỖI Ở ĐÂY --- >>
            };

            // 3. THÊM TẤT CẢ CÁC ROLE CỦA USER VÀO CLAIMS (Đây là code đúng)
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims, // Sử dụng danh sách claims đã cập nhật
                expires: DateTime.Now.AddHours(2400), // Token hết hạn sau 100 ngày
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}