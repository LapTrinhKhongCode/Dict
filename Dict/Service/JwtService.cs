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

            // Lấy workspace đầu tiên của user — workspace cá nhân đã được tạo lúc confirm email
            int defaultWorkspaceId = _db.WorkspaceMembers
                                        .Where(wm => wm.UserId == user.Id)
                                        .Select(wm => wm.WorkspaceId)
                                        .FirstOrDefault();

            // Fallback: nếu vì lý do nào đó chưa có workspace, tạo tại chỗ (backward compat)
            if (defaultWorkspaceId == 0)
            {
                defaultWorkspaceId = CreatePersonalWorkspace(user);
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("userId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
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

        private int CreatePersonalWorkspace(ApplicationUser user)
        {
            var personalWorkspace = new Workspace
            {
                Name = $"Personal - {user.UserName}",
                Description = "Không gian làm việc cá nhân",
                CreatedAt = DateTime.UtcNow
            };
            _db.Workspaces.Add(personalWorkspace);
            _db.SaveChanges();

            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = personalWorkspace.Id,
                UserId = user.Id,
                Role = WorkspaceRole.OWNER  // OWNER của personal workspace
            });
            _db.SaveChanges();
            return personalWorkspace.Id;
        }
    }
}