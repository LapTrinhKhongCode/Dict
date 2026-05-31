using System;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.OCR;
using Dict.Models;
using Dict.Service;
using Dict.Tests.Setup; // Đảm bảo namespace này khớp với file của bạn
using DotNetEnv;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Dict.Tests.IntegrationTests.Database
{
    // Kế thừa IDisposable để Rollback DB sau mỗi test case
    public class OcrJobServiceIntegrationTests : IDisposable
    {
        private readonly TestApplicationDbContext _context;
        private readonly OcrJobService _sut; // System Under Test
        private readonly IDbContextTransaction _transaction;

        public OcrJobServiceIntegrationTests()
        {
            // 1. Load chuỗi kết nối SQL Server thật từ file .env
            Env.Load();
            var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("TEST_CONNECTION_STRING không được tìm thấy trong file .env!");
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            _context = new TestApplicationDbContext(options);
            _sut = new OcrJobService(_context);

            // 2. Mở Transaction trên DB THẬT
            _transaction = _context.Database.BeginTransaction();
        }

        public void Dispose()
        {
            // 3. Rollback lại toàn bộ dữ liệu vừa test để DB luôn sạch sẽ
            _transaction.Rollback();
            _transaction.Dispose();
            _context.Dispose();
        }

        [Fact]
        public async Task AppendDetectedTextAsync_UsesExecuteUpdate_ShouldAppendTextCorrectly()
        {
            // Arrange: Tạo 1 Job giả dưới CSDL thật
            var job = new OcrJob
            {
                UserId = 1,
                Status = "processing",
                DetectedText = "Dòng 1\n",
                CreatedAt = DateTime.UtcNow
            };
            _context.OcrJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act: Gọi hàm có chứa ExecuteUpdateAsync
            await _sut.AppendDetectedTextAsync(job.Id, "Dòng 2");

            // Assert: Kéo dữ liệu từ DB lên xem text đã được cộng dồn chưa
            var updatedJob = await _context.OcrJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
            updatedJob.Should().NotBeNull();
            updatedJob!.DetectedText.Should().Be("Dòng 1\nDòng 2");
        }

        [Fact]
        public async Task TryCompleteJobAsync_WhenAllPagesDone_ShouldUpdateStatusToCompleted()
        {
            // Arrange
            var job = new OcrJob
            {
                UserId = 1,
                Status = "processing",
                DetectedText = "Đang đọc...",
                CreatedAt = DateTime.UtcNow
            };
            _context.OcrJobs.Add(job);
            await _context.SaveChangesAsync();

            // Giả lập đã có 2 trang OCR được lưu thành công vào CSDL
            _context.OcrResults.Add(new OcrResult { OcrJobId = job.Id, PageNumber = 1, WordText = "A", BoundingBox = "{}" });
            _context.OcrResults.Add(new OcrResult { OcrJobId = job.Id, PageNumber = 2, WordText = "B", BoundingBox = "{}" });
            await _context.SaveChangesAsync();

            // Act: Tổng số trang yêu cầu là 2 -> Job này đã đủ điều kiện để Complete
            await _sut.TryCompleteJobAsync(job.Id, totalPages: 2);

            // Assert: Kiểm tra xem status đã tự động chuyển sang "completed" chưa
            var updatedJob = await _context.OcrJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
            updatedJob!.Status.Should().Be("completed");
        }

        [Fact]
        public async Task TryCompleteJobAsync_WhenMissingPages_ShouldKeepProcessingStatus()
        {
            // Arrange
            var job = new OcrJob
            {
                UserId = 1,
                Status = "processing",
                DetectedText = "Đang đọc...",
                CreatedAt = DateTime.UtcNow
            };
            _context.OcrJobs.Add(job);
            await _context.SaveChangesAsync();

            // Mới chỉ lưu được trang 1
            _context.OcrResults.Add(new OcrResult { OcrJobId = job.Id, PageNumber = 1, WordText = "A", BoundingBox = "{}" });
            await _context.SaveChangesAsync();

            // Act: Yêu cầu 2 trang nhưng mới có 1
            await _sut.TryCompleteJobAsync(job.Id, totalPages: 2);

            // Assert: Status phải giữ nguyên là processing
            var updatedJob = await _context.OcrJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
            updatedJob!.Status.Should().Be("processing");
        }
    }
}