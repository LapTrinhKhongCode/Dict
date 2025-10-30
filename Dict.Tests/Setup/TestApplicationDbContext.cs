using Dict.Data;
using Microsoft.EntityFrameworkCore;

namespace Dict.Tests.Setup;

// 1. Kế thừa từ DbContext THẬT của bạn
public class TestApplicationDbContext : ApplicationDbContext
{
    // 2. Dùng constructor y hệt
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // 3. GHI ĐÈ OnConfiguring để KHÔNG LÀM GÌ CẢ
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Để trống!
        // KHÔNG gọi: base.OnConfiguring(optionsBuilder);
        // Điều này ngăn nó đọc file appsettings.json
    }
}