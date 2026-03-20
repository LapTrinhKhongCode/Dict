using System.Net;
using System.Text.Json;
using Dict.Service.IService; // <-- Using service
using Moq; // <-- Using Moq
using Xunit;

namespace Dict.Tests;

public class WordControllerTests : IClassFixture<WordControllerTestFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IWordService> _wordServiceMock;

    public WordControllerTests(WordControllerTestFactory factory)
    {
        _client = factory.CreateClient();
        _wordServiceMock = factory.WordServiceMock;
    }

    // Kịch bản 1: Test "Happy Path" - Khi service tìm thấy từ
    [Fact]
    public async Task GetWordJson_WhenWordExists_ReturnsOkWithJson()
    {
        // ----- ARRANGE (Sắp đặt) -----
        var label = "test-word";
        var mockJson = "{\"key\":\"value\"}"; // Dữ liệu JSON giả

        // Thiết lập Mock: "Nếu ai đó gọi GetWordJson với label 'test-word'..."
        _wordServiceMock
            .Setup(s => s.GetWordJson(label))
            .ReturnsAsync(mockJson); // "...thì hãy trả về chuỗi JSON này"

        // ----- ACT (Hành động) -----
        var response = await _client.GetAsync($"/api/word/GetWordJson/{label}");

        // ----- ASSERT (Kiểm chứng) -----
        response.EnsureSuccessStatusCode(); // Đảm bảo là 2xx
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Kiểm tra xem nội dung JSON trả về có đúng không
        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);
        Assert.Equal("value", jsonDoc.RootElement.GetProperty("key").GetString());
    }

    // Kịch bản 2: Test khi service trả về NULL
    //[Fact]
    //public async Task GetWordJson_WhenServiceReturnsNull_ReturnsNotFound()
    //{
    //    // ----- ARRANGE (Sắp đặt) -----
    //    var label = "null-word";

    //    // Thiết lập Mock: Trả về null
    //    _wordServiceMock
    //        .Setup(s => s.GetWordJson(label))
    //        .ReturnsAsync((string?)null);

    //    // ----- ACT (Hành động) -----
    //    var response = await _client.GetAsync($"/api/word/GetWordJson/{label}");

    //    // ----- ASSERT (Kiểm chứng) -----
    //    // Logic của controller là: if (string.IsNullOrEmpty(json)) return NotFound();
    //    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    //}

    // Kịch bản 3: Test khi service trả về chuỗi RỖNG
    //[Fact]
    //public async Task GetWordJson_WhenServiceReturnsEmpty_ReturnsNotFound()
    //{
    //    // ----- ARRANGE (Sắp đặt) -----
    //    var label = "empty-word";

    //    // Thiết lập Mock: Trả về chuỗi rỗng
    //    _wordServiceMock
    //        .Setup(s => s.GetWordJson(label))
    //        .ReturnsAsync(string.Empty);

    //    // ----- ACT (Hành động) -----
    //    var response = await _client.GetAsync($"/api/word/GetWordJson/{label}");

    //    // ----- ASSERT (Kiểm chứng) -----
    //    // Logic của controller là: if (string.IsNullOrEmpty(json)) return NotFound();
    //    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    //}
}