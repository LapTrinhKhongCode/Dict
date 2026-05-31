using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO.Deck;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Dict.Tests.Services
{
    public class DeckServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly DeckService _sut;

        public DeckServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _sut = new DeckService(_db, _mockUserManager.Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateDeckAsync_WhenUserExists_ShouldCreateDeckAndCards()
        {
            // Arrange
            int userId = 1;
            var user = new ApplicationUser { Id = userId, UserName = "duc_anh", AvatarUrl = "" };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId.ToString()))
                            .ReturnsAsync(user);

            var dto = new DeckCreateDto
            {
                Title = "N3 Kanji",
                Description = "Học Kanji N3",
                IsPublic = true,
                Cards = new List<CardCreateDto>
                {
                    new CardCreateDto { FrontText = "猫", BackText = "Con mèo" },
                    new CardCreateDto { FrontText = "犬", BackText = "Con chó" }
                }
            };

            // Act
            var result = await _sut.CreateDeckAsync(dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("N3 Kanji");
            result.CardCount.Should().Be(2);
            result.AuthorName.Should().Be("duc_anh");

            var savedDeck = await _db.Decks.Include(d => d.Cards).FirstOrDefaultAsync(d => d.Id == result.Id);
            savedDeck.Should().NotBeNull();
            savedDeck!.Cards.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddCardToDeckAsync_WhenUserIsNotOwner_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            int ownerId = 1;
            int hackerId = 2;
            int deckId = 10;

            // ĐÃ FIX: Thêm Description = ""
            _db.Decks.Add(new Deck { Id = deckId, UserId = ownerId, Name = "My Secret Deck", Description = "" });
            await _db.SaveChangesAsync();

            var newCards = new List<CardCreateDto>
            {
                new CardCreateDto { FrontText = "A", BackText = "B" }
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.AddCardToDeckAsync(deckId, newCards, hackerId);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không có quyền thêm thẻ vào bộ này.");
        }

        [Fact]
        public async Task SaveDeckForUserAsync_WhenDeckIsPublic_ShouldCreateDeepCopy()
        {
            // Arrange
            int originalOwnerId = 1;
            int newOwnerId = 2;
            int originalDeckId = 100;

            // ĐÃ FIX: Thêm Description = ""
            var originalDeck = new Deck
            {
                Id = originalDeckId,
                UserId = originalOwnerId,
                Name = "JLPT N2",
                Description = "",
                IsPublic = true,
                Cards = new List<Card>
                {
                    new Card { Id = 1, FrontText = "食べる", BackText = "Ăn", Template = "{}", Tags = "" }
                },
                User = new ApplicationUser { Id = originalOwnerId, UserName = "Sensei", AvatarUrl = "" }
            };
            _db.Decks.Add(originalDeck);
            await _db.SaveChangesAsync();

            var newOwner = new ApplicationUser { Id = newOwnerId, UserName = "Student", AvatarUrl = "" };
            _mockUserManager.Setup(u => u.FindByIdAsync(newOwnerId.ToString()))
                            .ReturnsAsync(newOwner);

            // Act
            var result = await _sut.SaveDeckForUserAsync(originalDeckId, newOwnerId);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("JLPT N2");
            result.CardCount.Should().Be(1);
            result.NowAuthorName.Should().Be("Student");

            var copiedDeck = await _db.Decks.Include(d => d.Cards)
                .FirstOrDefaultAsync(d => d.Id == result.Id);

            copiedDeck.Should().NotBeNull();
            copiedDeck!.Id.Should().NotBe(originalDeckId);
            copiedDeck.IsPublic.Should().BeFalse();
            copiedDeck.UserId.Should().Be(newOwnerId);
            copiedDeck.Cards.First().FrontText.Should().Be("食べる");
        }

        [Fact]
        public async Task SaveDeckForUserAsync_WhenDeckIsPrivate_ShouldThrowInvalidOperationException()
        {
            // Arrange
            int originalOwnerId = 1;
            int newOwnerId = 2;
            int privateDeckId = 99;

            // ĐÃ FIX: Thêm Description = ""
            _db.Decks.Add(new Deck
            {
                Id = privateDeckId,
                UserId = originalOwnerId,
                Name = "Nhật ký bí mật",
                Description = "",
                IsPublic = false
            });
            await _db.SaveChangesAsync();

            // Act & Assert
            Func<Task> act = async () => await _sut.SaveDeckForUserAsync(privateDeckId, newOwnerId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Chỉ có thể lưu các bộ thẻ công khai.");
        }

        [Fact]
        public async Task DeleteCardAsync_WhenCardBelongsToUser_ShouldDeleteCardAndStates()
        {
            // Arrange
            int userId = 1;
            int deckId = 10;
            int cardId = 55;

            var deck = new Deck { Id = deckId, UserId = userId, Name = "Test Deck", Description = "" };
            var card = new Card
            {
                Id = cardId,
                DeckId = deckId,
                Deck = deck,
                FrontText = "Mặt trước", // <-- ĐÃ FIX
                BackText = "Mặt sau",   // <-- ĐÃ FIX
                Tags = "",
                Template = "{}",
                CardStates = new List<CardState>
                {
                    new CardState { Id = 1, CardId = cardId, UserId = userId }
                }
            };

            _db.Decks.Add(deck);
            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            // Act
            var result = await _sut.DeleteCardAsync(cardId, userId);

            // Assert
            result.Should().BeTrue();

            var cardInDb = await _db.Cards.FindAsync(cardId);
            cardInDb.Should().BeNull();

            var stateInDb = await _db.CardStates.FirstOrDefaultAsync(cs => cs.CardId == cardId);
            stateInDb.Should().BeNull();
        }
    }
}