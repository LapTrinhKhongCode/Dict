using System;
using System.Linq;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.Deck;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.Services
{
    // Giả định cấu trúc của AnswerRequestDto (nếu project của bạn khác, hãy điều chỉnh lại nhé)
    public class AnswerRequestDtoMock : AnswerRequestDto
    {
        // Kế thừa tạm để nếu DTO của bạn thiếu constructor thì vẫn chạy được
    }

    public class ReviewServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly ReviewService _sut;

        public ReviewServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _sut = new ReviewService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        private async Task<(int userId, int deckId, int cardId)> SetupBaseDataAsync()
        {
            int userId = 1;
            int deckId = 10;
            int cardId = 100;

            _db.Decks.Add(new Deck { Id = deckId, UserId = userId, Name = "Test Deck", Description = "" });
            _db.Cards.Add(new Card
            {
                Id = cardId,
                DeckId = deckId,
                FrontText = "犬",
                BackText = "Chó",
                Tags = "",
                Template = "{}"
            });
            await _db.SaveChangesAsync();

            return (userId, deckId, cardId);
        }

        [Fact]
        public async Task ProcessAnswerAsync_NewCard_Easy_ShouldGraduateImmediately()
        {
            // Arrange: Thẻ hoàn toàn mới (chưa có CardState)
            var (userId, deckId, cardId) = await SetupBaseDataAsync();
            var answer = new AnswerRequestDto { CardId = cardId, Quality = 4 }; // 4 = Easy

            // Act
            var result = await _sut.ProcessAnswerAsync(answer, userId);

            // Assert
            result.Should().BeTrue();

            var state = await _db.CardStates.FirstOrDefaultAsync(cs => cs.CardId == cardId);
            state.Should().NotBeNull();

            // Theo thuật toán: Thẻ mới nhấn Easy -> Interval = 4 ngày (4 * 24 * 60 = 5760 phút)
            state!.Interval.Should().Be(4 * 24 * 60);

            // Kiểm tra log được lưu lại
            var log = await _db.ReviewLogs.FirstOrDefaultAsync(l => l.CardId == cardId);
            log.Should().NotBeNull();
            log!.Quality.Should().Be(4);
        }

        [Fact]
        public async Task ProcessAnswerAsync_NewCard_Again_ShouldSetToFirstLearningStep()
        {
            // Arrange: Thẻ mới
            var (userId, deckId, cardId) = await SetupBaseDataAsync();
            var answer = new AnswerRequestDto { CardId = cardId, Quality = 1 }; // 1 = Again

            // Act
            await _sut.ProcessAnswerAsync(answer, userId);

            // Assert
            var state = await _db.CardStates.FirstOrDefaultAsync(cs => cs.CardId == cardId);
            state!.Interval.Should().Be(1); // LEARNING_STEPS_MINUTES[0] = 1 phút
        }

        [Fact]
        public async Task ProcessAnswerAsync_ReviewCard_Good_ShouldMultiplyIntervalByEase()
        {
            // Arrange: Thẻ đang trong giai đoạn Ôn tập (Interval > 1 ngày)
            var (userId, deckId, cardId) = await SetupBaseDataAsync();

            int previousIntervalMinutes = 3 * 24 * 60; // 3 ngày
            float currentEase = 2.5f;

            _db.CardStates.Add(new CardState
            {
                CardId = cardId,
                UserId = userId,
                Interval = previousIntervalMinutes,
                Ease = currentEase,
                Reps = 5
            });
            await _db.SaveChangesAsync();

            var answer = new AnswerRequestDto { CardId = cardId, Quality = 3 }; // 3 = Good

            // Act
            await _sut.ProcessAnswerAsync(answer, userId);

            // Assert
            var state = await _db.CardStates.FirstOrDefaultAsync(cs => cs.CardId == cardId);

            // Theo thuật toán Review (Good): New Interval = previousInterval * Ease
            int expectedInterval = (int)(previousIntervalMinutes * currentEase);
            state!.Interval.Should().Be(expectedInterval);

            // Ease không đổi khi nhấn Good
            state.Ease.Should().Be(currentEase);
            state.Reps.Should().Be(6); // Tăng Reps
        }

        [Fact]
        public async Task ProcessAnswerAsync_ReviewCard_Again_ShouldResetIntervalAndDecreaseEase()
        {
            // Arrange: Thẻ ôn tập nhưng bị quên (Nhấn Again)
            var (userId, deckId, cardId) = await SetupBaseDataAsync();

            int previousIntervalMinutes = 10 * 24 * 60; // 10 ngày
            float currentEase = 2.5f;

            _db.CardStates.Add(new CardState
            {
                CardId = cardId,
                UserId = userId,
                Interval = previousIntervalMinutes,
                Ease = currentEase,
                Lapses = 0
            });
            await _db.SaveChangesAsync();

            var answer = new AnswerRequestDto { CardId = cardId, Quality = 1 }; // 1 = Again (Quên)

            // Act
            await _sut.ProcessAnswerAsync(answer, userId);

            // Assert
            var state = await _db.CardStates.FirstOrDefaultAsync(cs => cs.CardId == cardId);

            // Theo thuật toán Review (Again): 
            // - Quay về bước học đầu tiên (1 phút)
            // - Ease giảm 0.20
            // - Lapses tăng 1
            state!.Interval.Should().Be(1);
            state.Ease.Should().BeApproximately(2.3f, 0.01f); // 2.5 - 0.20
            state.Lapses.Should().Be(1);
        }

        [Fact]
        public async Task GetReviewQueueAsync_ShouldReturnOnlyDueCards()
        {
            // Arrange
            var (userId, deckId, cardId1) = await SetupBaseDataAsync();
            int cardId2 = 101;
            int cardId3 = 102; // Thẻ mới hoàn toàn (chưa có state)

            _db.Cards.Add(new Card { Id = cardId2, DeckId = deckId, FrontText = "猫", BackText = "Mèo", Tags = "", Template = "{}" });
            _db.Cards.Add(new Card { Id = cardId3, DeckId = deckId, FrontText = "鳥", BackText = "Chim", Tags = "", Template = "{}" });

            // Card 1: Đã quá hạn ôn tập (Due trong quá khứ) -> PHẢI XUẤT HIỆN
            _db.CardStates.Add(new CardState { CardId = cardId1, UserId = userId, DueDate = DateTime.UtcNow.AddMinutes(-10) });

            // Card 2: Chưa tới hạn ôn tập (Due trong tương lai) -> KHÔNG XUẤT HIỆN
            _db.CardStates.Add(new CardState { CardId = cardId2, UserId = userId, DueDate = DateTime.UtcNow.AddDays(1) });

            await _db.SaveChangesAsync();

            // Act
            var queue = await _sut.GetReviewQueueAsync(deckId, userId);

            // Assert
            queue.Should().NotBeNull();
            queue.Should().HaveCount(2); // Gồm Card 1 (quá hạn) và Card 3 (thẻ mới)
            queue.Should().Contain(c => c.Id == cardId1);
            queue.Should().Contain(c => c.Id == cardId3);
            queue.Should().NotContain(c => c.Id == cardId2);
        }
    }
}