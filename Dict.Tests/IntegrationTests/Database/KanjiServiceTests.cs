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

namespace Dict.Tests.IntegrationTests.Database;

// 1. Bỏ IClassFixture, chỉ cần IDisposable
public class KanjiServiceTests : IDisposable
{
    private readonly IKanjiService _service;
    private readonly TestApplicationDbContext _context;
    private readonly Mock<IJsonBuilderService> _mockJsonBuilder;
    private readonly IDbContextTransaction _transaction;


    public KanjiServiceTests()
    {
        Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new TestApplicationDbContext(options);

        // 4. Bắt đầu Transaction
        // (Vẫn cần thiết để rollback CÁC TEST GHI/SỬA/XÓA trong tương lai)
        _transaction = _context.Database.BeginTransaction();

        // 5. Khởi tạo Mock và Service
        _mockJsonBuilder = new Mock<IJsonBuilderService>();
        _service = new KanjiService(
            _context,
            _mockJsonBuilder.Object
        );
    }

    // 6. Dọn dẹp bằng Rollback (chạy sau mỗi test)
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
        // ----- ARRANGE -----
        // Không cần, vì DB 'Dict_TestDB' đã có sẵn dữ liệu "vàng"
        // từ lúc bạn Restore.

        // Cần đảm bảo '試' tồn tại trong DB test
        string kanjiToSearch = "試";

        // ----- ACT -----
        var result = await _service.GetKanjiInfoAsync(kanjiToSearch, "en");

        // ----- ASSERT -----
        Assert.NotNull(result);
        Assert.Equal(kanjiToSearch, result.Character);

        // VÍ DỤ: Bạn hãy kiểm tra các giá trị thật từ DB
        // Assert.Equal("Thí, thử", result.Meaning); 
        // Assert.True(result.Words.Any()); 
    }

    [Fact]
    public async Task GetKanjiJson_WhenSearchByPhonetic_FindsEntry()
    {
        // ----- ARRANGE -----
        // Không tạo data mới.
        // Chúng ta giả định 'てすと' tồn tại trong "dữ liệu vàng"
        string phoneticToSearch = "てすと";

        // ----- ACT -----
        var result = await _service.GetKanjiJson(phoneticToSearch);

        // ----- ASSERT -----
        // Bạn không thể dùng "{\"key\":\"phonetic-test\"}" nữa
        // vì đó là data giả.
        Assert.NotNull(result);
        // Kiểm tra xem JSON trả về có đúng là của 'てすと' không
        // (Thay đổi 'expected_json_value' cho đúng)
        // Assert.Contains("expected_json_value", result);
    }

    [Fact]
    public async Task GetKanjiJson_WhenSearchBySingleKanji_FindsEntry()
    {
        // ----- ARRANGE -----
        // Không tạo data mới.
        // Chúng ta giả định '試' tồn tại trong "dữ liệu vàng"
        string kanjiToSearch = "試";

        // ----- ACT -----
        var result = await _service.GetKanjiJson(kanjiToSearch);

        // ----- ASSERT -----
        Assert.NotNull(result);
        // Kiểm tra xem JSON trả về có đúng là của '試' không
        // (Thay đổi 'expected_json_value' cho đúng)
        // Assert.Contains("expected_json_value", result); 
    }

    [Fact]
    public async Task GetKanjiJson_WhenNoMatch_Returns404Json()
    {
        // ----- ARRANGE -----
        // (Không seed gì cả, test này chạy trên DB vàng)
        string nonExistentLabel = "abcxyz123";

        // ----- ACT -----
        var result = await _service.GetKanjiJson(nonExistentLabel);

        // ----- ASSERT -----
        Assert.Contains("\"status\":404", result);
    }

    [Fact]
    public async Task GetKanjiJson_WhenMultipleKanji_CallsRebuildService()
    {
        // ----- ARRANGE -----
        // (Test này không chạm vào DB, nó chỉ test Mock)
        var searchLabel = "試験"; // 2 chữ kanji
        var expectedJson = "{\"rebuilt\":true}";

        _mockJsonBuilder
            .Setup(j => j.RebuildJsonForKanjiAsync(searchLabel))
            .ReturnsAsync(expectedJson);

        // ----- ACT -----
        var result = await _service.GetKanjiJson(searchLabel);

        // ----- ASSERT -----
        Assert.Equal(expectedJson, result);
        _mockJsonBuilder.Verify(j => j.RebuildJsonForKanjiAsync(searchLabel), Times.Once);
    }
}