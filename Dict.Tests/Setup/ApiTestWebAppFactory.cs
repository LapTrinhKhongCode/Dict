using Dict.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MsSql;
namespace Dict.Tests.Setup;

// 1. Kế thừa WebApplicationFactory VÀ IAsyncLifetime
public class ApiTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 2. Định nghĩa container SQL Server
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest") // Dùng SQL Server 2022
        .WithPassword("localdev_123") // Đặt mật khẩu đủ mạnh
        
        .Build();

    // 3. Ghi đè (override) phương thức ConfigureWebHost
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 4. Tìm và gỡ bỏ DbContext thật (đăng ký trong Program.cs)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)); // <-- THAY AppDbContext BẰNG TÊN DbContext CỦA BẠN

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 5. Đăng ký lại DbContext, trỏ nó vào container Docker
            services.AddDbContext<ApplicationDbContext>(options => // <-- THAY AppDbContext
            {
                // Lấy connection string TỰ ĐỘNG từ container đang chạy
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
        });
    }

    // 6. (Từ IAsyncLifetime) Sẽ chạy TRƯỚC TẤT CẢ test
    public async Task InitializeAsync()
    {
        // Khởi động container
        await _dbContainer.StartAsync();

        // (Rất quan trọng) Tạo schema/chạy migration
        // Chúng ta cần tạo một scope để lấy DbContext và chạy migrate
        using (var scope = Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<ApplicationDbContext>(); // <-- THAY AppDbContext

            // Đảm bảo database được tạo và chạy migration
            await dbContext.Database.MigrateAsync();
        }
    }

    // 7. (Từ IAsyncLifetime) Sẽ chạy SAU TẤT CẢ test
    public new async Task DisposeAsync()
    {
        // Dừng và xóa sổ container
        await _dbContainer.StopAsync();
    }
}