using Dict.Service.IService; // <-- Thêm using cho IWordService
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq; // <-- Thêm using cho Moq

namespace Dict.Tests;

public class WordControllerTestFactory : WebApplicationFactory<Program>
{
    // Tạo một đối tượng Mock công khai để các test có thể truy cập
    public readonly Mock<IWordService> WordServiceMock = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1. Gỡ bỏ IWordService "thật" đã được đăng ký trong Program.cs
            services.RemoveAll<IWordService>();

            // 2. Thêm IWordService "giả" (mock) của chúng ta vào
            //    Dùng .Object để lấy đối tượng IWordService từ mock
            services.AddScoped<IWordService>(provider => WordServiceMock.Object);
        });
    }
}