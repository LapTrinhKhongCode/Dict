using Dict.Data;
using Dict.Models;
using Dict.Service; // Namespace của WordService
using Dict.Service.IService; // THÊM (cho IWordService)
using Dict.Tests.Setup; // Namespace của TestApplicationDbContext
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using Microsoft.Extensions.Logging; // THÊM
using Moq; // THÊM
using System.Threading.Tasks; // THÊM
using System.Linq; // THÊM

namespace Dict.Tests.IntegrationTests.Database;

public class WordServiceTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    // Sửa: Dùng Interface
    private readonly IWordService _service;
    private readonly IDbContextTransaction _transaction;

    // 1. THÊM MOCK LOGGER
    private readonly Mock<ILogger<WordService>> _mockLogger;

    public WordServiceTests()
    {
        Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=.;Database=Dict_TestDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new TestApplicationDbContext(options);

        // 2. KHỞI TẠO MOCK LOGGER
        _mockLogger = new Mock<ILogger<WordService>>();

        // 3. KHỞI TẠO SERVICE (ĐÃ SỬA)
        _service = new WordService(_context, _mockLogger.Object); // Thêm Logger

        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    // ----- CÁC TEST (Đã cập nhật Assert) -----

    [Fact]
    public async Task GetWordJson_WhenSearchingByLabel_FindsCorrectEntry_And_LogsHit()
    {
        // ARRANGE 
        var labelToSearch = "日本語"; // Giả sử "日本語" tồn tại trong DB test

        // Tìm EntryId thật trong DB Test để kiểm tra
        var entryInDb = await _context.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Type == "word" && e.Label == labelToSearch);

        Assert.NotNull(entryInDb); // Đảm bảo data test tồn tại

        // ACT
        var result = await _service.GetWordJson(labelToSearch);

        // ASSERT (Kiểm tra kết quả)
        Assert.NotNull(result);
        Assert.Equal(entryInDb.RawJson, result); // So sánh JSON thật

        // ASSERT (Kiểm tra Log Hit)
        var stat = await _context.StatsWordFreq.FirstOrDefaultAsync(s => s.EntryId == entryInDb.Id);
        Assert.NotNull(stat);
        Assert.Equal(1, stat.Occurrences);

        // Đảm bảo nó không ghi log Miss
        var miss = await _context.SearchMiss.FirstOrDefaultAsync(s => s.NormalizedTerm == labelToSearch.ToLower());
        Assert.Null(miss);
    }

    [Fact]
    public async Task GetWordJson_WhenSearchingByPhoneticLabel_FindsCorrectEntry_And_LogsHit()
    {
        // ARRANGE
        var phoneticToSearch = "アイテム"; // Giả sử "アイテム" (item) tồn tại

        var entryInDb = await _context.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Type == "word" && e.Label == phoneticToSearch);

        Assert.NotNull(entryInDb); // Đảm bảo data test tồn tại

        // ACT
        var result = await _service.GetWordJson(phoneticToSearch);

        // ASSERT (Kiểm tra kết quả)
        Assert.NotNull(result);
        Assert.Equal(entryInDb.RawJson, result);

        // ASSERT (Kiểm tra Log Hit)
        var stat = await _context.StatsWordFreq.FirstOrDefaultAsync(s => s.EntryId == entryInDb.Id);
        Assert.NotNull(stat);
        Assert.Equal(1, stat.Occurrences);
    }

    [Fact]
    public async Task GetWordJson_WhenWordDoesNotExist_ReturnsNull_And_LogsMiss()
    {
        // ARRANGE
        var labelToSearch = "存在しない_abc_123";

        // ACT
        var result = await _service.GetWordJson(labelToSearch);

        // ASSERT (Kiểm tra kết quả)
        Assert.Null(result);

        // ASSERT (Kiểm tra Log Miss)
        // (Dùng tên bảng SearchMiss (số ít) như bạn đã sửa)
        var miss = await _context.SearchMiss.FirstOrDefaultAsync(s => s.NormalizedTerm == labelToSearch.ToLower());
        Assert.NotNull(miss);
        Assert.Equal(1, miss.SearchCount);

        // Đảm bảo nó không ghi log Hit
        var statCount = await _context.StatsWordFreq.CountAsync();
        Assert.Equal(0, statCount); // (Giả định DB trống lúc bắt đầu test)
    }
}