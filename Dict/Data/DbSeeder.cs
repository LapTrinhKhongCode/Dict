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

                // 2. TẠO CÁC ROLE CƠ BẢN
                // (Chúng ta thêm "Moderator" và "Premium" luôn vì đằng nào cũng cần)
                string[] roleNames = { "Admin", "Moderator", "User", "Premium" };

                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        // === SỬA LỖI CS1729 ===
                        // Dùng constructor rỗng và gán thuộc tính Name
                        // thay vì new ApplicationRole(roleName)
                        await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                        // ========================

                        logger.LogInformation($"Role '{roleName}' đã được tạo.");
                    }
                }

                // 3. TẠO TÀI KHOẢN ADMIN ĐẦU TIÊN

                // === CẤU HÌNH ADMIN ACCOUNT ===
                var adminEmail = "admin@dict.com";
                var adminPass = "SuperPassword123!"; // <<< CẢNH BÁO: HÃY ĐỔI MẬT KHẨU NÀY
                // ==============================

                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin", // Bạn có thể đổi username
                        Email = adminEmail,
                        EmailConfirmed = true, // Xác thực email luôn
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        AvatarUrl = "" // (Tùy chọn)
                    };

                    // Tạo user mới với mật khẩu
                    var result = await userManager.CreateAsync(adminUser, adminPass);

                    if (result.Succeeded)
                    {
                        // Gán 2 vai trò: "Admin" (để quản lý) và "User" (để có thể dùng các tính năng)
                        await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "User" });
                        logger.LogInformation($"Tài khoản Admin '{adminEmail}' đã được tạo và gán role 'Admin' + 'User'.");
                    }
                    else
                    {
                        // Log lỗi nếu tạo thất bại
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError($"LỖI: Không thể tạo tài khoản Admin: {errors}");
                    }
                }
            }
        }
    }
}