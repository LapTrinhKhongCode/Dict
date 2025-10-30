using Dict.Data;
using Dict.Models;
using Dict.Service; // Namespace của WordService
using Dict.Tests.Setup; // Namespace của TestApplicationDbContext
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Dict.Tests.IntegrationTests.Database;

// 1. Đổi tên class cho đúng. Bỏ IClassFixture, chỉ cần IDisposable
public class WordServiceTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly WordService _service;
    private readonly IDbContextTransaction _transaction;

    public WordServiceTests()
    {
        Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new TestApplicationDbContext(options);

        // 4. Khởi tạo Service THẬT
        _service = new WordService(_context);

        // 5. Bắt đầu transaction
        // (Vẫn cần, phòng trường hợp bạn thêm test GHI/SỬA/XÓA sau này)
        _transaction = _context.Database.BeginTransaction();

        // 6. XÓA BỎ GỌI SEEDDATA()
        // SeedData(); 
    }

    // 7. XÓA BỎ CÁC HÀM SEEDDATA() VÀ CREATETESTENTRY()
    // private void SeedData() { ... }
    // private Entry CreateTestEntry(...) { ... }

    // 8. Hàm dọn dẹp (Rollback) (Giữ nguyên)
    public void Dispose()
    {
        _transaction.Rollback(); // Hoàn tác mọi thay đổi (thêm, sửa, xóa)
        _transaction.Dispose();
        _context.Dispose();
    }

    // ----- CÁC TEST (Sửa lại Assert) -----

    [Fact]
    public async Task GetWordJson_WhenSearchingByLabel_FindsCorrectEntry()
    {
        // ARRANGE 
        // Giả sử "日本語" tồn tại trong DB test của bạn
        var labelToSearch = "日本語";

        // ACT
        var result = await _service.GetWordJson(labelToSearch);

        // ASSERT
        // (Kiểm tra xem nó có lấy đúng entry "word" không)
        Assert.NotNull(result);

        // THAY ĐỔI ASSERT NÀY:
        // Bạn cần kiểm tra xem 'result' có chứa 
        // một phần JSON thật từ DB của bạn
        // Ví dụ: Assert.Contains("nghĩa là tiếng Nhật", result);
    }

    [Fact]
    public async Task GetWordJson_WhenSearchingByPhonetic_FindsCorrectEntry()
    {
        // ARRANGE
        // Giả sử "てすと" (test) tồn tại trong DB test của bạn
        var phoneticToSearch = "アイテム";

        // ACT
        var result = await _service.GetWordJson(phoneticToSearch);

        // ASSERT
        Assert.NotNull(result);

        // THAY ĐỔI ASSERT NÀY:
        // Bạn cần kiểm tra xem 'result' có chứa 
        // một phần JSON thật từ DB của bạn
        // Ví dụ: Assert.Contains("nghĩa là bài test", result);
    }

    [Fact]
    public async Task GetWordJson_WhenWordDoesNotExist_ReturnsNull()
    {
        // ARRANGE
        // Test này hoàn hảo, không cần sửa
        var labelToSearch = "存在しない_abc_123"; // Một chuỗi chắc chắn không có

        // ACT
        var result = await _service.GetWordJson(labelToSearch);

        // ASSERT
        Assert.Null(result);
    }
}