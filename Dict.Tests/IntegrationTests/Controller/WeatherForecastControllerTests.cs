using System.Net;
using System.Net.Http.Json; // Cần thiết cho .ReadFromJsonAsync
using Dict.Tests.Setup; // Using cái factory ta vừa tạo
using Xunit;

// Giả định class WeatherForecast nằm trong namespace 'Dict'
// (Cùng namespace với Program.cs)
using Dict;

namespace Dict.Tests.IntegrationTests.Controllers;

public class WeatherForecastControllerTests : IClassFixture<SimpleTestWebAppFactory>
{
    private readonly HttpClient _client;

    public WeatherForecastControllerTests(SimpleTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WeatherForecast_ReturnsOkAndFiveItems()
    {
        // ----- ARRANGE -----
        // (Không cần chuẩn bị gì)

        // ----- ACT -----
        // Gửi request GET đến route "[controller]" -> "WeatherForecast"
        var response = await _client.GetAsync("/WeatherForecast");

        // ----- ASSERT -----

        // 1. Kiểm tra Status Code
        response.EnsureSuccessStatusCode(); // Đảm bảo là 2xx
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // 2. Đọc nội dung JSON trả về
        //    (Test project phải reference project chính để thấy class WeatherForecast)
        var forecasts = await response.Content.ReadFromJsonAsync<IEnumerable<WeatherForecast>>();

        // 3. Kiểm tra nội dung
        Assert.NotNull(forecasts);       // Không phải là null
        Assert.Equal(5, forecasts.Count()); // Phải trả về 5 ngày
    }
}