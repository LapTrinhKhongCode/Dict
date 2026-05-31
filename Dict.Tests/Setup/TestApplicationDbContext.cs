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
        // Nếu optionsBuilder chưa được cấu hình (ví dụ khi chạy lệnh Update-Database từ PMC)
        // Ta sẽ ép nó dùng SQL Server Test kèm thời gian Timeout lớn (5 phút = 300 giây)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                "Server=.;Database=Dict_TestDB;Trusted_Connection=True;TrustServerCertificate=True",
                opts => opts.CommandTimeout(300) // 💡 TĂNG TIMEOUT LÊN 5 PHÚT TẠI ĐÂY
            );
        }
    }
}