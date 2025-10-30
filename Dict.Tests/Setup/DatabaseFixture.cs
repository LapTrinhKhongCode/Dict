using Dict.Data;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient; // <-- Cần thêm using này
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Dict.Tests.Setup;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;
    private const string TestDatabaseName = "dict_test_db"; // <-- 1. Đặt tên DB


    public string ConnectionString { get; private set; } = null!;

    public DatabaseFixture()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("localdev_123")
            .Build();
        // Xóa dòng .WithDatabase() bị lỗi
    }

    public async Task InitializeAsync()
    {
        // 1. Khởi động container
        await _dbContainer.StartAsync();

        // 2. Lấy connection string GỐC (trỏ tới 'master')
        var rawConnectionString = _dbContainer.GetConnectionString();

        // 3. TẠO DATABASE BẰNG TAY
        // Kết nối đến 'master' để tạo DB mới
        var masterConnectionString = new SqlConnectionStringBuilder(rawConnectionString)
        {
            InitialCatalog = "master" // Trỏ vào 'master'
        }.ConnectionString;

        await using (var masterConnection = new SqlConnection(masterConnectionString))
        {
            await masterConnection.OpenAsync();
            await using (var command = masterConnection.CreateCommand())
            {
                // Dùng IF NOT EXISTS để tránh lỗi nếu chạy lại
                command.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{TestDatabaseName}') CREATE DATABASE [{TestDatabaseName}]";
                await command.ExecuteNonQueryAsync();
            }
        }

        // 4. TẠO CONNECTION STRING CUỐI CÙNG (trỏ tới DB mới)
        this.ConnectionString = new SqlConnectionStringBuilder(rawConnectionString)
        {
            InitialCatalog = TestDatabaseName // <-- Trỏ vào DB 'dict_test_db'
        }.ConnectionString;


        // 5. CHẠY MIGRATION (trên DB mới)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(this.ConnectionString) // Dùng connection string mới
            .Options;

        await using (var context = new TestApplicationDbContext(options))
        {
            // Bây giờ MigrateAsync() sẽ chạy trên 'dict_test_db'
            // và lệnh Full-Text Search sẽ thành công
            await context.Database.EnsureCreatedAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}