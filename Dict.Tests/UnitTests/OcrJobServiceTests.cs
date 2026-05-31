using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.OCR;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.Services
{
    public class OcrJobServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly OcrJobService _sut;

        public OcrJobServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _sut = new OcrJobService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateJobAndReturnDto()
        {
            // Arrange
            var createDto = new OcrJobCreateDto
            {
                UserId = 1,
                MediaId = 10,
                ProjectId = 100,
                Status = "pending",
                DetectedText = ""
            };

            // Act
            var result = await _sut.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("pending");
            result.UserId.Should().Be(1);

            var savedJob = await _db.OcrJobs.FindAsync(result.Id);
            savedJob.Should().NotBeNull();
            savedJob!.MediaId.Should().Be(10);
            savedJob.ProjectId.Should().Be(100);
        }

        [Fact]
        public async Task AppendResultsAsync_WhenJobExists_ShouldSaveResults()
        {
            // Arrange
            int jobId = 5;
            _db.OcrJobs.Add(new OcrJob { Id = jobId, Status = "processing", DetectedText = "" });
            await _db.SaveChangesAsync();

            var newResults = new List<CreateOcrResultDto>
            {
                new CreateOcrResultDto { PageNumber = 1, WordText = "日本語", BoundingBox = "[{x:1,y:2}]" },
                new CreateOcrResultDto { PageNumber = 1, WordText = "テスト", BoundingBox = "[{x:3,y:4}]" }
            };

            // Act
            await _sut.AppendResultsAsync(jobId, newResults);

            // Assert
            var savedResults = await _db.OcrResults.Where(r => r.OcrJobId == jobId).ToListAsync();
            savedResults.Should().HaveCount(2);
            savedResults.First().WordText.Should().Be("日本語");
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenJobExists_ShouldUpdateFields()
        {
            // Arrange
            int jobId = 10;
            _db.OcrJobs.Add(new OcrJob { Id = jobId, Status = "pending", DetectedText = "Cũ" });
            await _db.SaveChangesAsync();

            var updateDto = new OcrJobUpdateStatusDto
            {
                Status = "completed",
                DetectedText = "Văn bản đã nhận diện xong"
            };

            // Act
            await _sut.UpdateStatusAsync(jobId, updateDto);

            // Assert
            var updatedJob = await _db.OcrJobs.FindAsync(jobId);
            updatedJob!.Status.Should().Be("completed");
            updatedJob.DetectedText.Should().Be("Văn bản đã nhận diện xong");
        }
    }
}