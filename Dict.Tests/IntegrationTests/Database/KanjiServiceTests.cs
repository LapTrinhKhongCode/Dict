using Dict.Data;
using Dict.Models;
using Dict.Service; // Namespace của KanjiService
using Dict.Service.IService;
using Dict.Tests.Setup; // Namespace của TestApplicationDbContext
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging; // <-- THÊM
using System.Threading.Tasks; // <-- THÊM
using System.Linq; // <-- THÊM

namespace Dict.Tests.IntegrationTests.Database;

public class KanjiServiceTests : IDisposable
{
    private readonly IKanjiService _service;
    private readonly TestApplicationDbContext _context;
    private readonly Mock<IJsonBuilderService> _mockJsonBuilder;

    // 1. THÊM MOCK LOGGER
    private readonly Mock<ILogger<KanjiService>> _mockLogger;

    private readonly IDbContextTransaction _transaction;

    public KanjiServiceTests()
    {
        // (Env.Load() và ConnectionString giữ nguyên)
        Env.Load();
        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            // Cung cấp một giá trị dự phòng để tránh lỗi nếu .env không load được
            connectionString = "Server=.;Database=Dict_TestDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new TestApplicationDbContext(options);

        _transaction = _context.Database.BeginTransaction();

        // 5. Khởi tạo Mock và Service
        _mockJsonBuilder = new Mock<IJsonBuilderService>();

        // 6. KHỞI TẠO MOCK LOGGER MỚI
        _mockLogger = new Mock<ILogger<KanjiService>>();

        // 7. CẬP NHẬT CONSTRUCTOR (Thêm _mockLogger.Object)
        _service = new KanjiService(
            _context,
            _mockJsonBuilder.Object,
            _mockLogger.Object // <-- ĐÃ THÊM
        );
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    // ----- CÁC BÀI TEST -----

    [Fact]
    public async Task GetKanjiInfoAsync_WhenKanjiExists_ReturnsFullDto()
    {
        // (Test này giữ nguyên, nó không gọi hàm GetKanjiJson)
        // ----- ARRANGE -----
        string kanjiToSearch = "試"; // Cần đảm bảo '試' tồn tại trong DB test

        // ----- ACT -----
        var result = await _service.GetKanjiInfoAsync(kanjiToSearch, "en");

        // ----- ASSERT -----
        Assert.NotNull(result);
        Assert.Equal(kanjiToSearch, result.Character);
    }

    [Fact]
    public async Task GetKanjiJson_WhenSearchByPhonetic_FindsEntryAndLogsHit()
    {
        // ----- ARRANGE -----
        string phoneticToSearch = "てすと"; // Giả định 'てすと' tồn tại

        // Tìm EntryId thật trong DB Test để kiểm tra
        var entryInDb = await _context.Entries
            .FirstOrDefaultAsync(e => e.Type == "kanji" && e.Phonetic == phoneticToSearch);

        // Nếu DB Test của bạn không có từ này, test sẽ thất bại (đây là điều tốt)
        Assert.NotNull(entryInDb);

        // ----- ACT -----
        var result = await _service.GetKanjiJson(phoneticToSearch);

        // ----- ASSERT -----
        Assert.NotNull(result);
        Assert.Equal(entryInDb.RawJson, result); // So sánh với JSON thật

        // 8. KIỂM TRA LOGGING (QUAN TRỌNG)
        // (Sau khi migration, chúng ta kiểm tra StatsWordFreq.EntryId)
        var stat = await _context.StatsWordFreq.FirstOrDefaultAsync(s => s.EntryId == entryInDb.Id);
        Assert.NotNull(stat); // Phải tạo ra 1 dòng log
        Assert.Equal(1, stat.Occurrences); // Đếm là 1
    }

    [Fact]
    public async Task GetKanjiJson_WhenSearchBySingleKanji_FindsEntryAndLogsHit()
    {
        // ----- ARRANGE -----
        string kanjiToSearch = "試"; // Giả định '試' tồn tại

        var entryInDb = await _context.Entries
            .FirstOrDefaultAsync(e => e.Type == "kanji" && e.Label == kanjiToSearch);

        Assert.NotNull(entryInDb);

        // ----- ACT -----
        var result = await _service.GetKanjiJson(kanjiToSearch);

        // ----- ASSERT -----
        Assert.NotNull(result);
        Assert.Equal(entryInDb.RawJson, result);

        // 8. KIỂM TRA LOGGING (QUAN TRỌNG)
        var stat = await _context.StatsWordFreq.FirstOrDefaultAsync(s => s.EntryId == entryInDb.Id);
        Assert.NotNull(stat);
        Assert.Equal(1, stat.Occurrences);
    }

    [Fact]
    public async Task GetKanjiJson_WhenNoMatch_Returns404JsonAndLogsMiss()
    {
        // ----- ARRANGE -----
        string nonExistentLabel = "abcxyz123";

        // ----- ACT -----
        var result = await _service.GetKanjiJson(nonExistentLabel);

        // ----- ASSERT -----
        Assert.Contains("\"status\":404", result);

        // 8. KIỂM TRA LOGGING (QUAN TRỌNG)
        // (Dùng tên bảng SearchMiss (số ít) như bạn đã sửa)
        var miss = await _context.SearchMiss.FirstOrDefaultAsync(s => s.NormalizedTerm == nonExistentLabel);
        Assert.NotNull(miss);
        Assert.Equal(1, miss.SearchCount);
    }

    [Fact]
    public async Task GetKanjiJson_WhenMultipleKanji_CallsRebuildService()
    {
        // (Test này giữ nguyên, nó test Mock)
        // ----- ARRANGE -----
        var searchLabel = "試験";
        var expectedJson = "{\"rebuilt\":true}";

        _mockJsonBuilder
            .Setup(j => j.RebuildJsonForKanjiAsync(searchLabel))
            .ReturnsAsync(expectedJson);

        // ----- ACT -----
        var result = await _service.GetKanjiJson(searchLabel);

        // ----- ASSERT -----
        Assert.Equal(expectedJson, result);
        _mockJsonBuilder.Verify(j => j.RebuildJsonForKanjiAsync(searchLabel), Times.Once);

        // 9. KIỂM TRA: Hàm này KHÔNG được ghi log Miss
        var miss = await _context.SearchMiss.FirstOrDefaultAsync(s => s.NormalizedTerm == "試験");
        Assert.Null(miss); // Không được ghi log miss
    }
}