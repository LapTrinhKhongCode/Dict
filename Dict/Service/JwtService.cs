using Dict.Models;
using Dict.Service.IService;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Dict.Data; // ✅ BỔ SUNG USING NÀY ĐỂ GỌI DATABASE

namespace Dict.Service
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db; // ✅ KHAI BÁO DATABASE CONTEXT

        // ✅ TIÊM THÊM ApplicationDbContext VÀO CONSTRUCTOR
        public JwtService(IConfiguration config, ApplicationDbContext db)
        {
            _config = config;
            _db = db;
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // ✅ TÌM WORKSPACE MẶC ĐỊNH CỦA USER NÀY
            // Lấy ID công ty đầu tiên mà user này đang tham gia. Nếu không có trả về 0.
            int defaultWorkspaceId = _db.WorkspaceMembers
                                        .Where(wm => wm.UserId == user.Id)
                                        .Select(wm => wm.WorkspaceId)
                                        .FirstOrDefault();

            // 🌟 2. HYBRID B2B + B2C: NẾU CHƯA CÓ, TỰ ĐỘNG TẠO WORKSPACE CÁ NHÂN
            if (defaultWorkspaceId == 0)
            {
                // Tạo một Workspace riêng cho khách hàng cá nhân này
                var personalWorkspace = new Workspace
                {
                    Name = $"Personal - {user.UserName}",
                    Description = "Không gian làm việc cá nhân",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Workspaces.Add(personalWorkspace);
                _db.SaveChanges(); // Lưu để lấy ID mới

                // Cho user này làm Admin của chính cái Workspace cá nhân đó
                var member = new WorkspaceMember
                {
                    WorkspaceId = personalWorkspace.Id,
                    UserId = user.Id,
                    Role = "Admin"
                };

                _db.WorkspaceMembers.Add(member);
                _db.SaveChanges();

                // Gán lại ID vừa tạo để nhét vào Token
                defaultWorkspaceId = personalWorkspace.Id;
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("userId", user.Id.ToString()), // Đã chốt dùng chuẩn này
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // Bây giờ Token luôn luôn có WorkspaceId (Dù là B2B hay B2C)
                new Claim("WorkspaceId", defaultWorkspaceId.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2400),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}