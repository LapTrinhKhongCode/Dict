using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.Services
{
    public class SearchServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TrieAutocompleteCache _trieCache;
        private readonly SearchService _sut; // SUT = System Under Test

        public SearchServiceTests()
        {
            // 1. Giả lập Database bằng InMemory (Cô lập hoàn toàn với SQL Server)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Tên DB ngẫu nhiên để các test case không đụng nhau
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // 2. Khởi tạo TrieCache trống
            _trieCache = new TrieAutocompleteCache();

            // 3. Khởi tạo Service cần test
            _sut = new SearchService(_dbContext, _trieCache);
        }

        // Dọn dẹp Database ảo sau khi mỗi Test case chạy xong
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task FindExactLabelMatchAsync_ShouldReturnEntry_WhenMatchIsExact()
        {
            // Arrange (Chuẩn bị dữ liệu)
            _dbContext.Entries.Add(new Entry
            {
                Id = 1,
                Label = "suru",
                Type = "word",
                ShortMean = "làm",
                RawJson = "{}",         // Bổ sung trường bắt buộc
                CommentRawJson = "[]"   // Bổ sung trường bắt buộc
            });

            _dbContext.Entries.Add(new Entry
            {
                Id = 2,
                Label = "suru-nai",
                Type = "word",
                ShortMean = "không làm",
                RawJson = "{}",         // Bổ sung trường bắt buộc
                CommentRawJson = "[]"   // Bổ sung trường bắt buộc
            });

            await _dbContext.SaveChangesAsync();

            // Act (Thực thi)
            var result = await _sut.FindExactLabelMatchAsync("suru");

            // Assert (Kiểm tra kết quả)
            result.Should().NotBeNull();
            result!.Label.Should().Be("suru");
            result.ShortMean.Should().Be("làm");
        }

        [Fact]
        public async Task GetAutocompleteSuggestionsAsync_WhenTrieIsLoaded_ShouldReturnFromRamFast()
        {
            // Arrange: Tự dựng một cây Trie "bằng tay" để giả lập dữ liệu đã nạp.
            // Giả lập cây có chứa từ "su"
            var root = new FlatTrieNode { Character = ' ', FirstChildIndex = 1, NextSiblingIndex = -1 };
            var nodeS = new FlatTrieNode { Character = 's', FirstChildIndex = 2, NextSiblingIndex = -1 };
            var nodeU = new FlatTrieNode
            {
                Character = 'u',
                FirstChildIndex = -1,
                NextSiblingIndex = -1,
                SuggestionOffset = 0,
                SuggestionCount = 1
            };

            _trieCache.NodePool = new[] { root, nodeS, nodeU };
            _trieCache.SuggestionPool = new[]
            {
                new AutocompleteSuggestionDto { Word = "suru", Reading = "する", Meaning = "làm" }
            };
            _trieCache.IsLoaded = true;

            // Act
            var results = await _sut.GetAutocompleteSuggestionsAsync("su");

            // Assert
            results.Should().NotBeNullOrEmpty();
            results.Should().HaveCount(1);
            results.First().Word.Should().Be("suru");

            // Đảm bảo DB chưa hề bị gọi tới (vì Trie đã gánh)
            _dbContext.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAutocompleteSuggestionsAsync_WhenTrieNotLoaded_ShouldFallbackToDatabase()
        {
            // Arrange: Trie chưa load
            _trieCache.IsLoaded = false;

            // Thêm dữ liệu vào DB ảo
            _dbContext.Entries.Add(new Entry
            {
                Id = 1,
                Label = "taberu",
                Phonetic = "たべる",
                ShortMean = "ăn",
                Type = "word",
                Weight = 10,
                RawJson = "{}",         // Bổ sung trường bắt buộc
                CommentRawJson = "[]"   // Bổ sung trường bắt buộc
            });

            _dbContext.Entries.Add(new Entry
            {
                Id = 2,
                Label = "nomu",
                Phonetic = "のむ",
                ShortMean = "uống",
                Type = "word",
                Weight = 5,
                RawJson = "{}",         // Bổ sung trường bắt buộc
                CommentRawJson = "[]"   // Bổ sung trường bắt buộc
            });

            await _dbContext.SaveChangesAsync();

            // Act
            var results = await _sut.GetAutocompleteSuggestionsAsync("tabe");

            // Assert
            results.Should().NotBeNullOrEmpty();
            results.Should().HaveCount(1);
            results.First().Word.Should().Be("taberu");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetSuggestionEntriesAsync_WithInvalidTerm_ShouldReturnEmptyList(string invalidTerm)
        {
            // Act
            var results = await _sut.GetSuggestionEntriesAsync(invalidTerm, 10);

            // Assert
            results.Should().BeEmpty();
        }
    }
}