using Dict.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // <-- Giữ nguyên
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dict.Data
{
    /// <summary>
    /// Lớp tĩnh (static) để khởi tạo dữ liệu mồi (Roles và Admin User)
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Chạy hàm này từ Program.cs để tạo Roles và Admin
        /// </summary>
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            // 1. Lấy các dịch vụ (Manager)
            using (var serviceScope = services.CreateScope())
            {
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // === SỬA LỖI CS0718 ===
                // Lấy ILoggerFactory thay vì ILogger<DbSeeder> vì DbSeeder là static
                var loggerFactory = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("DbSeeder"); // Tạo logger với tên
                // =======================

                // 2. TẠO CÁC ROLE CƠ BẢN — dùng UPPERCASE nhất quán với Role enum
                string[] roleNames = { "ADMIN", "MODERATOR", "USER", "PREMIUM_USER" };

                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                        logger.LogInformation($"Role '{roleName}' đã được tạo.");
                    }
                }

                // 3. TẠO TÀI KHOẢN ADMIN ĐẦU TIÊN
                var adminEmail = "admin@dict.com";
                var adminPass = "SuperPassword123!";

                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        AvatarUrl = ""
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPass);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRolesAsync(adminUser, new[] { "ADMIN", "USER" });
                        logger.LogInformation($"Tài khoản Admin '{adminEmail}' đã được tạo và gán role 'ADMIN' + 'USER'.");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError($"LỖI: Không thể tạo tài khoản Admin: {errors}");
                    }
                }
                else
                {
                    // Migrate existing admin: đổi role cũ Capitalize → UPPERCASE nếu cần
                    var existingRoles = await userManager.GetRolesAsync(adminUser);
                    if (existingRoles.Contains("Admin") && !existingRoles.Contains("ADMIN"))
                    {
                        await userManager.RemoveFromRoleAsync(adminUser, "Admin");
                        await userManager.AddToRoleAsync(adminUser, "ADMIN");
                        logger.LogInformation("Migrated admin role: Admin → ADMIN");
                    }
                    if (existingRoles.Contains("User") && !existingRoles.Contains("USER"))
                    {
                        await userManager.RemoveFromRoleAsync(adminUser, "User");
                        await userManager.AddToRoleAsync(adminUser, "USER");
                        logger.LogInformation("Migrated admin role: User → USER");
                    }
                }
            }
        }
    }
}