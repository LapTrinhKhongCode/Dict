using Dict.Data;
using Dict.DTO; // Namespace của các DTOs
using Dict.Models;
using Dict.Service; // Namespace của SearchService
using Dict.Service.IService;
using Dict.Tests.Setup; // Namespace của TestApplicationDbContext
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dict.Tests.IntegrationTests.Database;

public class SearchServiceTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly ISearchService _service;
    private readonly IDbContextTransaction _transaction;

    public SearchServiceTests()
    {
        Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new TestApplicationDbContext(options);

        //_service = new SearchService(_context);

        // Bắt đầu transaction (để đảm bảo test không "ghi" gì vào DB)
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback(); // Luôn rollback (dù là test đọc)
        _transaction.Dispose();
        _context.Dispose();
    }

    #region Tests for Read/Query Logic

    [Fact]
    public async Task FindExactLabelMatchAsync_WhenMatchExists_ReturnsEntry()
    {
        // ----- ARRANGE -----
        // 🛑 THAY THẾ CHUỖI NÀY: Dùng một 'Label' có thật trong DB
        string exactLabel = "日本語";

        // ----- ACT -----
        // Test hàm dùng EF.Functions.Collate
        var result = await _service.FindExactLabelMatchAsync(exactLabel);

        // ----- ASSERT -----
        Assert.NotNull(result);
        Assert.Equal(exactLabel, result.Label);
        Assert.Equal("word", result.Type);
    }

    [Fact]
    public async Task FindExactLabelMatchAsync_WhenNoMatch_ReturnsNull()
    {
        // ----- ARRANGE -----
        string nonExistentLabel = "abcxyz123_definitely_not_in_db";

        // ----- ACT -----
        var result = await _service.FindExactLabelMatchAsync(nonExistentLabel);

        // ----- ASSERT -----
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSuggestionEntriesAsync_WhenFTSMatchExists_ReturnsRankedEntries()
    {
        // ----- ARRANGE -----
        // 🛑 THAY THẾ CHUỖI NÀY: Dùng một từ khóa FTS có thật
        // (Ví dụ: "test" nếu bạn có entry "this is a test")
        string ftsTerm = "とる";

        // ----- ACT -----
        // Test hàm dùng EF.Functions.Contains (Full-Text Search)
        var results = await _service.GetSuggestionEntriesAsync(ftsTerm, 10);

        // ----- ASSERT -----
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        // (Bạn có thể Assert.Equal() thứ tự nếu bạn biết rõ ranking)
    }

    [Fact]
    public async Task GetAutocompleteSuggestionsAsync_WhenMatchExists_ReturnsRankedSuggestions()
    {
        // ----- ARRANGE -----
        // 1. Giả sử user gõ "nihon", và frontend chuyển nó thành "にほん"
        //    (Hoặc bạn có thể dùng "日本" nếu bạn muốn test tìm bằng Kanji)
        string startsWithTerm = "日本";

        // 2. Dựa trên JSON của bạn, kết quả đầu tiên phải là "日本"
        string expectedFirstWord = "日本";
        string expectedFirstReading = "にほん にっぽん";
        string expectedFirstMeaning = "nhật bản";

        // ----- ACT -----
        // Test hàm dùng FTS prefix ("にほん*") và LIKE (にほん%)
        var results = await _service.GetAutocompleteSuggestionsAsync(startsWithTerm);

        // ----- ASSERT -----
        Assert.NotNull(results);
        Assert.NotEmpty(results);

        // 1. Lấy kết quả đầu tiên (ranking cao nhất)
        var firstResult = results.First();

        // 2. Kiểm tra xem nó có đúng là "日本" không
        Assert.Equal(expectedFirstWord, firstResult.Word);
        Assert.Equal(expectedFirstReading, firstResult.Reading);
        Assert.Equal(expectedFirstMeaning, firstResult.Meaning);

        // 3. Kiểm tra xem nó có trả về nhiều kết quả không
        Assert.True(results.Count >= 10);
    }

    #endregion
}