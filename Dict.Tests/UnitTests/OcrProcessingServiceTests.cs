using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.OCR;
using Dict.Hubs;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using FluentAssertions;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dict.Tests.Services
{
    public class OcrProcessingServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<IHttpClientFactory> _mockHttpFactory;
        private readonly Mock<ILogger<OcrProcessingService>> _mockLogger;
        private readonly Mock<IOcrJobService> _mockOcrJobService;
        private readonly Mock<IBlobService> _mockBlobService;
        private readonly Mock<ImageAnnotatorClient> _mockVisionClient;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<IHubContext<NotificationHub>> _mockHub;
        private readonly OcrProcessingService _sut;

        public OcrProcessingServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            _mockHttpFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<OcrProcessingService>>();
            _mockOcrJobService = new Mock<IOcrJobService>();
            _mockBlobService = new Mock<IBlobService>();
            _mockVisionClient = new Mock<ImageAnnotatorClient>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Hub mock: stub Clients.Group(...).SendAsync(...) để không throw
            _mockHub = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            _mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            _sut = new OcrProcessingService(
                _mockHttpFactory.Object,
                _mockLogger.Object,
                _mockOcrJobService.Object,
                _db,
                _mockBlobService.Object,
                _mockVisionClient.Object,
                _memoryCache,
                _mockHub.Object
            );
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
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
        public async Task UploadImageOnlyAsync_ShouldUploadToBlob_CallVision_AndReturnCompleted()
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

            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Callback<Stream, System.Threading.CancellationToken>((stream, token) =>
                    {
                        ms.Position = 0;
                        ms.CopyTo(stream);
                    })
                    .Returns(Task.CompletedTask);

            // 2. Mock Blob Service
            _mockBlobService.Setup(b => b.UploadFileBlobAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedBlobUrl);

            // 3. Mock OcrJobService — CreateAsync trả về job processing, UpdateStatusAsync no-op
            _mockOcrJobService.Setup(j => j.CreateAsync(It.IsAny<OcrJobCreateDto>()))
                .ReturnsAsync(new OcrJobDto { Id = expectedJobId, Status = "processing" });
            _mockOcrJobService.Setup(j => j.AppendResultsAsync(It.IsAny<int>(), It.IsAny<List<CreateOcrResultDto>>()))
                .Returns(Task.CompletedTask);
            _mockOcrJobService.Setup(j => j.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<OcrJobUpdateStatusDto>()))
                .Returns(Task.CompletedTask);

            // 4. Mock Google Vision — trả về TextAnnotation rỗng (không crash)
            _mockVisionClient
                .Setup(v => v.DetectDocumentTextAsync(
                    It.IsAny<Google.Cloud.Vision.V1.Image>(),
                    It.IsAny<Google.Cloud.Vision.V1.ImageContext>(),
                    It.IsAny<Google.Api.Gax.Grpc.CallSettings>()))
                .ReturnsAsync((TextAnnotation)null);

            // Act
            var result = await _sut.UploadImageOnlyAsync(mockFile.Object, userId, workspaceId, projectId);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(expectedJobId);
            result.Status.Should().Be("completed");
            result.ImageUrl.Should().Be(expectedBlobUrl);

            // MediaStore được lưu
            var savedMedia = await _db.MediaStore.FirstOrDefaultAsync(m => m.OwnerId == userId);
            savedMedia.Should().NotBeNull();
            savedMedia!.StorageUrl.Should().Be(expectedBlobUrl);
            savedMedia.WorkspaceId.Should().Be(workspaceId);
            savedMedia.FileName.Should().Be(fileName);

            // Blob upload được gọi 1 lần
            _mockBlobService.Verify(b => b.UploadFileBlobAsync("ocr-images", It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Job được update thành completed
            _mockOcrJobService.Verify(j => j.UpdateStatusAsync(
                expectedJobId,
                It.Is<OcrJobUpdateStatusDto>(d => d.Status == "completed")), Times.Once);
        }
    }
}