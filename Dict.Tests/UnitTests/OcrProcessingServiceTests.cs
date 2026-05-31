using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.OCR;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dict.Tests.Services
{
    public class OcrProcessingServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<IHttpClientFactory> _mockHttpFactory;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<OcrProcessingService>> _mockLogger;
        private readonly Mock<IOcrJobService> _mockOcrJobService;
        private readonly Mock<IBlobService> _mockBlobService;
        private readonly OcrProcessingService _sut;

        public OcrProcessingServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            _mockHttpFactory = new Mock<IHttpClientFactory>();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OcrProcessingService>>();
            _mockOcrJobService = new Mock<IOcrJobService>();
            _mockBlobService = new Mock<IBlobService>();

            _sut = new OcrProcessingService(
                _mockHttpFactory.Object,
                _mockConfig.Object,
                _mockLogger.Object,
                _mockOcrJobService.Object,
                _db,
                _mockBlobService.Object,
                _mockConfig.Object // Dùng lại mock config
            );
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreatePdfJobAsync_ShouldReturnPendingJobDto()
        {
            // Arrange
            int userId = 1;
            int workspaceId = 10;
            int? projectId = 100;
            string fileName = "book.pdf";
            int totalPages = 5;
            int expectedJobId = 99;

            // Giả lập hàm CreateAsync của IOcrJobService
            _mockOcrJobService.Setup(s => s.CreateAsync(It.IsAny<OcrJobCreateDto>()))
                .ReturnsAsync(new OcrJobDto { Id = expectedJobId, Status = "pending" });

            // Act
            var result = await _sut.CreatePdfJobAsync(userId, workspaceId, projectId, fileName, totalPages);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(expectedJobId);
            result.Status.Should().Be("pending");

            // Đảm bảo hàm CreateAsync đã được gọi 1 lần với status = pending
            _mockOcrJobService.Verify(s => s.CreateAsync(It.Is<OcrJobCreateDto>(dto => dto.Status == "pending")), Times.Once);
        }

        [Fact]
        public async Task UploadImageOnlyAsync_ShouldUploadToBlob_SaveMediaStore_AndCreateJob()
        {
            // Arrange
            int userId = 1;
            int workspaceId = 10;
            int? projectId = 100;
            string expectedBlobUrl = "https://azure.blob.com/ocr-images/test.png";
            int expectedJobId = 88;

            // 1. Mock file ảnh (IFormFile)
            var mockFile = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "test.png";
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/png");

            // Mock CopyToAsync để nó ghi dữ liệu ảo vào MemoryStream nội bộ của Service
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Callback<Stream, System.Threading.CancellationToken>((stream, token) =>
                    {
                        ms.Position = 0;
                        ms.CopyTo(stream);
                    })
                    .Returns(Task.CompletedTask);

            // 2. Mock Blob Service trả về URL thành công
            _mockBlobService.Setup(b => b.UploadFileBlobAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedBlobUrl);

            // 3. Mock OcrJobService trả về DTO
            _mockOcrJobService.Setup(j => j.CreateAsync(It.IsAny<OcrJobCreateDto>()))
                .ReturnsAsync(new OcrJobDto { Id = expectedJobId, Status = "pending" });

            // Act
            var result = await _sut.UploadImageOnlyAsync(mockFile.Object, userId, workspaceId, projectId);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(expectedJobId);
            result.Status.Should().Be("pending");
            result.ImageUrl.Should().Be(expectedBlobUrl);

            // Đảm bảo MediaStore đã được lưu xuống DB ảo thành công
            var savedMedia = await _db.MediaStore.FirstOrDefaultAsync(m => m.OwnerId == userId);
            savedMedia.Should().NotBeNull();
            savedMedia!.StorageUrl.Should().Be(expectedBlobUrl);
            savedMedia.WorkspaceId.Should().Be(workspaceId);
            savedMedia.FileName.Should().Be(fileName);

            // Đảm bảo Blob upload được gọi đúng 1 lần
            _mockBlobService.Verify(b => b.UploadFileBlobAsync("ocr-images", It.IsAny<Stream>(), "image/png", It.IsAny<string>()), Times.Once);
        }
    }
}