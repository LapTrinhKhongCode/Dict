using Dict.Data;
using Dict.Service.IService; // Using service của bạn
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System;

namespace Dict.Tests.Setup;

// Factory này dùng cho các test không cần Database
public class SimpleTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Gỡ bỏ DbContext thật (nếu có)
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>)); // <-- THAY AppDbContext bằng tên DbContext của bạn

            // Thêm DbContext In-Memory (chỉ để app khởi động được)
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Gỡ bỏ Service thật (nếu có)
            services.RemoveAll<IWordService>();

            // Thêm Service giả (Moq) (chỉ để app khởi động được)
            services.AddScoped<IWordService>(_ => new Mock<IWordService>().Object);
        });
    }
}