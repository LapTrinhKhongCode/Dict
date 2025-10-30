using Dict.Data;
using Dict.Models;
using Dict.Service; // Namespace của DeckService
using Dict.Service.IService;
using Dict.DTO.Deck; // Namespace của các DTOs
using Dict.Tests.Setup; // Namespace của TestApplicationDbContext
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System; // Cần cho Guid, DateTime
using Microsoft.AspNetCore.Identity; // <-- THÊM
using Moq; // <-- THÊM
using DotNetEnv;
namespace Dict.Tests.IntegrationTests.Database
{
    public class DeckServiceTests : IDisposable
    {
        private readonly TestApplicationDbContext _context;
        private readonly IDeckService _service;
        private readonly IDbContextTransaction _transaction;

        // 1. THÊM MOCK CHO USER MANAGER
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

        // Chúng ta sẽ tạo 2 user: 1 chủ sở hữu, 1 "kẻ tấn công"
        private ApplicationUser _testUser = null!;
        private ApplicationUser _attackerUser = null!;

        public DeckServiceTests()
        {
            Env.Load();

            var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            _context = new TestApplicationDbContext(options);

            // 2. KHỞI TẠO MOCK USER MANAGER (GIỐNG NHƯ TRONG UserServiceTests)
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // 3. KHỞI TẠO SERVICE (ĐÃ CẬP NHẬT)
            // Truyền cả context THẬT và manager MOCK
            _service = new DeckService(_context, _mockUserManager.Object);

            _transaction = _context.Database.BeginTransaction();

            // Chạy hàm seed (tạo user)
            SeedDataAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Dọn dẹp (Rollback) (chạy sau mỗi test)
        /// </summary>
        public void Dispose()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _context.Dispose();
        }

        #region Helper Functions (Quan trọng nhất)

        /// <summary>
        /// Tạo 2 user test.
        /// (Hàm này giữ nguyên, vì nó seed data vào DB THẬT của test)
        /// </summary>
        private async Task SeedDataAsync()
        {
            var uniqueId1 = Guid.NewGuid().ToString("N").Substring(0, 10);
            _testUser = new ApplicationUser
            {
                UserName = $"test_user_{uniqueId1}",
                Email = $"test_{uniqueId1}@example.com",
                PasswordHash = "dummy_hash_123",
                AvatarUrl = ""
            };

            var uniqueId2 = Guid.NewGuid().ToString("N").Substring(0, 10);
            _attackerUser = new ApplicationUser
            {
                UserName = $"attacker_{uniqueId2}",
                Email = $"attacker_{uniqueId2}@example.com",
                PasswordHash = "dummy_hash_456",
                AvatarUrl = ""
            };

            // Dùng _context.Users (DbSet từ IdentityDbContext)
            _context.Users.AddRange(_testUser, _attackerUser);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Tạo một Deck hợp lệ. (Giữ nguyên)
        /// </summary>
        private async Task<Deck> CreateValidDeckAsync(int userId, string name = "Test Deck")
        {
            var deck = new Deck
            {
                Name = name,
                Description = "Default Description",
                UserId = userId,
                IsPublic = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Decks.Add(deck);
            await _context.SaveChangesAsync();
            return deck;
        }

        /// <summary>
        /// Tạo một Card hợp lệ. (Giữ nguyên)
        /// </summary>
        private async Task<Card> CreateValidCardAsync(int deckId, string frontText = "Front")
        {
            var card = new Card
            {
                DeckId = deckId,
                FrontText = frontText,
                BackText = "Default Back",
                Tags = "",
                Template = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Cards.Add(card);
            await _context.SaveChangesAsync();
            return card;
        }

        /// <summary>
        /// Tạo một CardState (Giữ nguyên)
        /// </summary>
        private async Task<CardState> CreateValidCardStateAsync(int cardId, int userId)
        {
            var state = new CardState
            {
                CardId = cardId,
                UserId = userId,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            _context.CardStates.Add(state);
            await _context.SaveChangesAsync();
            return state;
        }

        #endregion

        #region CRUD Tests (Tạo, Sửa, Xóa)

        [Fact]
        public async Task CreateDeckAsync_WithCards_SavesDeckAndCardsCorrectly()
        {
            // ----- ARRANGE -----
            var deckDto = new DeckCreateDto
            {
                Title = "Bộ thẻ Test của tôi",
                Description = "Mô tả test",
                IsPublic = false,
                Cards = new List<CardCreateDto>
                {
                    new CardCreateDto { FrontText = "Mặt trước 1", BackText = "Mặt sau 1", Tags = "tag1" },
                    new CardCreateDto { FrontText = "Mặt trước 2", BackText = "Mặt sau 2", Tags = "tag2" }
                }
            };

            // 4. "DẠY" MOCK MANAGER
            // Hàm CreateDeckAsync sẽ gọi _userManager.FindByIdAsync,
            // nên ta phải "dạy" mock cách trả về user thật (đã được seed)
            _mockUserManager.Setup(m => m.FindByIdAsync(_testUser.Id.ToString()))
                            .ReturnsAsync(_testUser);

            // ----- ACT -----
            var result = await _service.CreateDeckAsync(deckDto, _testUser.Id);

            // ----- ASSERT -----
            Assert.NotNull(result);
            Assert.Equal("Bộ thẻ Test của tôi", result.Name);
            Assert.Equal(2, result.CardCount);
            Assert.Equal(_testUser.UserName, result.AuthorName);

            var deckInDb = await _context.Decks
                .Include(d => d.Cards)
                .FirstOrDefaultAsync(d => d.Id == result.Id);

            Assert.NotNull(deckInDb);
            Assert.Equal(_testUser.Id, deckInDb.UserId);
            Assert.Equal(2, deckInDb.Cards.Count);
            Assert.Equal("tag1", deckInDb.Cards.First().Tags);
        }

        [Fact]
        public async Task UpdateDeckAsync_WhenUserIsOwner_UpdatesDeck()
        {
            // (Hàm này không gọi UserManager, không cần mock)
            // ----- ARRANGE -----
            var originalDeck = await CreateValidDeckAsync(_testUser.Id, "Tên Gốc");

            var updateDto = new DeckUpdateDto
            {
                Title = "Tên Đã Sửa",
                Description = "Mô tả Đã Sửa",
                IsPublic = true
            };

            // ----- ACT -----
            var result = await _service.UpdateDeckAsync(originalDeck.Id, updateDto, _testUser.Id);

            // ----- ASSERT -----
            Assert.True(result);

            var updatedDeck = await _context.Decks.FindAsync(originalDeck.Id);
            Assert.NotNull(updatedDeck);
            Assert.Equal("Tên Đã Sửa", updatedDeck.Name);
            Assert.Equal("Mô tả Đã Sửa", updatedDeck.Description);
            Assert.True(updatedDeck.IsPublic);
        }

        [Fact]
        public async Task UpdateDeckAsync_WhenUserIsNotOwner_ReturnsFalse()
        {
            // (Hàm này không gọi UserManager, không cần mock)
            // ----- ARRANGE -----
            var originalDeck = await CreateValidDeckAsync(_testUser.Id, "Deck Của Chủ");
            var updateDto = new DeckUpdateDto { Title = "Tên Bị Hack" };

            // ----- ACT -----
            var result = await _service.UpdateDeckAsync(originalDeck.Id, updateDto, _attackerUser.Id);

            // ----- ASSERT -----
            Assert.False(result);
            var deckInDb = await _context.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == originalDeck.Id);
            Assert.Equal("Deck Của Chủ", deckInDb.Name);
        }

        [Fact]
        public async Task DeleteDeckAsync_WhenUserIsOwner_DeletesDeck()
        {
            // (Hàm này không gọi UserManager, không cần mock)
            // ----- ARRANGE -----
            var deck = await CreateValidDeckAsync(_testUser.Id, "Sắp Bị Xóa");

            // ----- ACT -----
            var result = await _service.DeleteDeckAsync(deck.Id, _testUser.Id);

            // ----- ASSERT -----
            Assert.True(result);
            var deletedDeck = await _context.Decks.FindAsync(deck.Id);
            Assert.Null(deletedDeck);
        }

        #endregion

        #region Card Logic Tests (Nghiệp vụ Card)

        [Fact]
        public async Task AddCardToDeckAsync_WhenDeckExists_AddsCards()
        {
            // (Hàm này không gọi UserManager, không cần mock)
            // ----- ARRANGE -----
            var deck = await CreateValidDeckAsync(_testUser.Id, "Deck Rỗng");
            var newCardsDto = new List<CardCreateDto>
            {
                new CardCreateDto { FrontText = "Card Mới 1", BackText = "Mặt sau Mới 1", Tags = "new" }
            };

            // ----- ACT -----
            var result = await _service.AddCardToDeckAsync(deck.Id, newCardsDto, _testUser.Id);

            // ----- ASSERT -----
            Assert.NotNull(result);
            Assert.Single(result);
            var cardsInDb = await _context.Cards.Where(c => c.DeckId == deck.Id).ToListAsync();
            Assert.Single(cardsInDb);
            Assert.Equal("Mặt sau Mới 1", cardsInDb.First().BackText);
            Assert.Equal("new", cardsInDb.First().Tags);
        }

        [Fact]
        public async Task DeleteCardAsync_WhenCardExists_DeletesCardAndStates()
        {
            // (Hàm này không gọi UserManager, không cần mock)
            // ----- ARRANGE -----
            var deck = await CreateValidDeckAsync(_testUser.Id);
            var card = await CreateValidCardAsync(deck.Id);
            var cardState = await CreateValidCardStateAsync(card.Id, _testUser.Id);

            // ----- ACT -----
            var result = await _service.DeleteCardAsync(card.Id, _testUser.Id);

            // ----- ASSERT -----
            Assert.True(result);
            var deletedCard = await _context.Cards.FindAsync(card.Id);
            var deletedState = await _context.CardStates.FindAsync(cardState.Id);
            Assert.Null(deletedCard);
            Assert.Null(deletedState);
        }

        #endregion

        #region Read/Query Tests (Test Đọc)

        [Fact]
        public async Task GetUserDecksAsync_WhenUserHasDecks_ReturnsDecks()
        {
            // (Hàm này không gọi UserManager, không cần mock)
            // ----- ARRANGE -----
            await CreateValidDeckAsync(_testUser.Id, "Deck A");
            await CreateValidDeckAsync(_testUser.Id, "Deck B");
            await CreateValidDeckAsync(_attackerUser.Id, "Deck Của Kẻ Tấn Công");

            // ----- ACT -----
            var decks = await _service.GetUserDecksAsync(_testUser.Id);

            // ----- ASSERT -----
            Assert.NotNull(decks);
            Assert.Equal(2, decks.Count());
            Assert.Contains(decks, d => d.Name == "Deck A");
            Assert.DoesNotContain(decks, d => d.Name == "Deck Của Kẻ Tấn Công");
        }

        // 5. NÊN THÊM TEST CHO HÀM SaveDeckForUserAsync (VÌ HÀM NÀY CÓ DÙNG MANAGER)
        [Fact]
        public async Task SaveDeckForUserAsync_WhenValid_ClonesDeck()
        {
            // ----- ARRANGE -----
            var originalDeck = await CreateValidDeckAsync(_testUser.Id, "Deck Công Khai");
            originalDeck.IsPublic = true; // Đặt là public
            await _context.SaveChangesAsync();

            // "DẠY" MOCK MANAGER
            // Hàm này sẽ tìm "newOwner" (là attacker)
            _mockUserManager.Setup(m => m.FindByIdAsync(_attackerUser.Id.ToString()))
                            .ReturnsAsync(_attackerUser);

            // ----- ACT -----
            var result = await _service.SaveDeckForUserAsync(originalDeck.Id, _attackerUser.Id);

            // ----- ASSERT -----
            Assert.NotNull(result);
            Assert.Equal("Deck Công Khai", result.Name);
            Assert.Equal(_attackerUser.UserName, result.NowAuthorName); // Tác giả mới là attacker
            Assert.Equal(_testUser.UserName, result.AuthorName); // Tác giả gốc là testUser

            // Kiểm tra DB xem deck mới đã được tạo cho attacker chưa
            var newDeckInDb = await _context.Decks.FirstOrDefaultAsync(
                d => d.UserId == _attackerUser.Id && d.Name == "Deck Công Khai");

            Assert.NotNull(newDeckInDb);
            Assert.False(newDeckInDb.IsPublic); // Bản sao phải là private
        }


        #endregion
    }
}