using Dict.Data;
using Dict.DTO.Deck; // Namespace của AnswerRequestDto
using Dict.Models;
using Dict.Service; // Namespace của ReviewService
using Dict.Service.IService;
using Dict.Tests.Setup; // Namespace của TestApplicationDbContext
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dict.Tests.IntegrationTests.Database;

// Tên hằng số (chép từ service của bạn) để test cho chính xác
public static class SrsConstants
{
    public static readonly int[] LEARNING_STEPS_MINUTES = { 1, 10 };
    public static readonly int GRADUATING_INTERVAL_DAYS = 1;
    public static readonly int EASY_INTERVAL_DAYS = 4;
}

public class ReviewServiceTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly IReviewService _service;
    private readonly IDbContextTransaction _transaction;

    // Dữ liệu mồi (seed) được tạo mới cho mỗi test
    private ApplicationUser _testUser = null!;
    private Deck _testDeck = null!;

    public ReviewServiceTests()
    {
        Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new TestApplicationDbContext(options);

        _service = new ReviewService(_context);
        _transaction = _context.Database.BeginTransaction();

        // Tạo dữ liệu nền (User, Deck)
        SeedBaseDataAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    #region Helper Functions (Dựa trên Schema)

    /// <summary>
    /// Tạo 1 User. Dựa trên schema, User cần:
    /// Username, Email, PasswordHash, Role, AvatarUrl (NOT NULL)
    /// </summary>
    private async Task SeedBaseDataAsync()
    {
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 10);
        _testUser = new ApplicationUser
        {
            UserName = $"review_user_{uniqueId}",
            Email = $"review_{uniqueId}@example.com",
            PasswordHash = "dummy_hash_123", // NOT NULL
            //Role = "User", // NOT NULL (Dù có Default)
            AvatarUrl = "", // NOT NULL (Dù có Default)
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(_testUser);
        await _context.SaveChangesAsync(); // Phải Save để User có Id

        // Tạo Deck. Deck cần: Name, Description (NOT NULL)
        _testDeck = new Deck
        {
            Name = "Review Test Deck",
            Description = "Default Description", // NOT NULL
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Decks.Add(_testDeck);
        await _context.SaveChangesAsync(); // Phải Save để Deck có Id
    }

    /// <summary>
    /// Tạo một Card hợp lệ. Card cần:
    /// FrontText, BackText, Tags (NOT NULL)
    /// </summary>
    private async Task<Card> CreateValidCardAsync(string frontText = "Test Front")
    {
        var card = new Card
        {
            DeckId = _testDeck.Id,
            FrontText = frontText,
            BackText = "Test Back", // NOT NULL
            Tags = "", // NOT NULL
            Template = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return card;
    }

    /// <summary>
    /// Hàm helper chính: Tạo một CardState có kịch bản CỤ THỂ
    /// (Tất cả các trường trong CardState đều Nullable)
    /// </summary>
    private async Task<CardState> CreateTestStateAsync(int cardId, int intervalMins, float ease, int reps, int lapses)
    {
        var state = new CardState
        {
            UserId = _testUser.Id,
            CardId = cardId,
            Ease = ease,
            Interval = intervalMins, // Quan trọng
            Reps = reps,
            Lapses = lapses,
            DueDate = DateTime.UtcNow.AddMinutes(intervalMins)
        };
        _context.CardStates.Add(state);
        await _context.SaveChangesAsync();
        return state;
    }

    /// <summary>
    /// Tạo một ReviewLog hợp lệ (chỉ 'Note' là NOT NULL)
    /// </summary>
    private async Task<ReviewLog> CreateTestLogAsync(int cardId, int cardStateId)
    {
        var log = new ReviewLog
        {
            CardStateId = cardStateId,
            UserId = _testUser.Id,
            CardId = cardId,
            Note = "Test Log", // NOT NULL
            Timestamp = DateTime.UtcNow
        };
        _context.ReviewLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    // Hàm helper để tạo DTO cho dễ
    private AnswerRequestDto CreateAnswer(int cardId, int quality)
    {
        return new AnswerRequestDto { CardId = cardId, Quality = quality };
    }

    #endregion

    #region Tests cho Thẻ Mới / Thẻ Đang Học (Learning)

    [Fact]
    // Đổi tên test cho rõ hơn (hoặc giữ nguyên)
    public async Task ProcessAnswerAsync_NewCard_QualityGood_MovesToFirstStep()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        var answer = CreateAnswer(card.Id, 3); // 3 = Good

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        Assert.NotNull(state);

        // SỬA Ở ĐÂY: Mong đợi bước 1 (1 phút)
        Assert.Equal(SrsConstants.LEARNING_STEPS_MINUTES[0], state.Interval); // Expected: 1

        Assert.Equal(2.5f, state.Ease);
        Assert.Equal(0, state.Reps);
    }

    [Fact]
    public async Task ProcessAnswerAsync_LearningCardStep1_QualityGood_MovesToStep2()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();

        // Tạo 1 CardState đang ở bước 1 (1 phút)
        await CreateTestStateAsync(
            cardId: card.Id,
            intervalMins: SrsConstants.LEARNING_STEPS_MINUTES[0], // = 1
            ease: 2.5f,
            reps: 0,
            lapses: 0
        );

        var answer = CreateAnswer(card.Id, 3); // 3 = Good

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        Assert.NotNull(state);

        // MONG ĐỢI BƯỚC 2 (10 phút)
        Assert.Equal(SrsConstants.LEARNING_STEPS_MINUTES[1], state.Interval); // Expected: 10
    }

    [Fact]
    public async Task ProcessAnswerAsync_NewCard_QualityEasy_GraduatesImmediately()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        var answer = CreateAnswer(card.Id, 4); // 4 = Easy

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        Assert.NotNull(state);
        // Tốt nghiệp 4 ngày (tính bằng phút)
        Assert.Equal(SrsConstants.EASY_INTERVAL_DAYS * 24 * 60, state.Interval);
        Assert.Equal(2.5f, state.Ease);
    }

    [Fact]
    public async Task ProcessAnswerAsync_LearningCard_QualityAgain_ResetsToFirstStep()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        // Thẻ đang ở bước 2 (10 phút)
        await CreateTestStateAsync(card.Id, intervalMins: 10, ease: 2.5f, reps: 0, lapses: 0);
        var answer = CreateAnswer(card.Id, 1); // 1 = Again

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        // Quay về bước 1 (1 phút)
        Assert.Equal(SrsConstants.LEARNING_STEPS_MINUTES[0], state.Interval);
    }

    [Fact]
    public async Task ProcessAnswerAsync_LearningCard_QualityGood_Graduates()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        // Thẻ đang ở bước cuối (10 phút)
        await CreateTestStateAsync(card.Id, intervalMins: 10, ease: 2.5f, reps: 0, lapses: 0);
        var answer = CreateAnswer(card.Id, 3); // 3 = Good

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        // Tốt nghiệp 1 ngày (tính bằng phút)
        Assert.Equal(SrsConstants.GRADUATING_INTERVAL_DAYS * 24 * 60, state.Interval);
    }

    #endregion

    #region Tests cho Thẻ Ôn tập (Review)

    [Fact]
    public async Task ProcessAnswerAsync_ReviewCard_QualityGood_CalculatesInterval()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        // Thẻ đã tốt nghiệp, interval 10 ngày (tính bằng phút), ease 2.5
        int previousInterval = 10 * 24 * 60;
        float ease = 2.5f;
        await CreateTestStateAsync(card.Id, previousInterval, ease, reps: 5, lapses: 0);
        var answer = CreateAnswer(card.Id, 3); // 3 = Good

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        // Interval mới = 10 * 2.5 = 25 ngày
        int expectedInterval = (int)(previousInterval * ease);
        Assert.Equal(expectedInterval, state.Interval);
        Assert.Equal(2.5f, state.Ease); // Ease không đổi
        Assert.Equal(6, state.Reps); // Reps + 1
    }

    [Fact]
    public async Task ProcessAnswerAsync_ReviewCard_QualityAgain_LapsesCard()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        int previousInterval = 10 * 24 * 60; // 10 ngày
        await CreateTestStateAsync(card.Id, previousInterval, 2.5f, reps: 5, lapses: 1);
        var answer = CreateAnswer(card.Id, 1); // 1 = Again (Lapse)

        // ----- ACT -----
        await _service.ProcessAnswerAsync(answer, _testUser.Id);

        // ----- ASSERT -----
        var state = await _context.CardStates.FirstAsync(cs => cs.CardId == card.Id);
        Assert.Equal(SrsConstants.LEARNING_STEPS_MINUTES[0], state.Interval); // Quay về bước học 1 phút
        Assert.Equal(2.3f, state.Ease); // Ease bị giảm (2.5 - 0.2)
        Assert.Equal(2, state.Lapses); // Lapses + 1
        Assert.Equal(5, state.Reps); // Reps không đổi khi lapse
    }

    #endregion

    #region Tests cho các hàm khác

    [Fact]
    public async Task ResetCardProgressAsync_WhenStateExists_DeletesStateAndLogs()
    {
        // ----- ARRANGE -----
        var card = await CreateValidCardAsync();
        var cardState = await CreateTestStateAsync(card.Id, 10, 2.5f, 1, 0);
        var log = await CreateTestLogAsync(card.Id, cardState.Id);

        // ----- ACT -----
        var result = await _service.ResetCardProgressAsync(card.Id, _testUser.Id);

        // ----- ASSERT -----
        Assert.True(result);
        var stateInDb = await _context.CardStates.FirstOrDefaultAsync(cs => cs.Id == cardState.Id);
        var logInDb = await _context.ReviewLogs.FirstOrDefaultAsync(rl => rl.Id == log.Id);

        Assert.Null(stateInDb); // State đã bị xóa
        Assert.Null(logInDb); // Log cũng bị xóa
    }

    [Fact]
    public async Task GetReviewQueueAsync_ReturnsNewAndDueCardsOnly()
    {
        // ----- ARRANGE -----
        // Thẻ 1: Thẻ mới (chưa có state)
        var cardNew = await CreateValidCardAsync("Card New");

        // Thẻ 2: Thẻ đã học, due date trong quá khứ (cần review)
        var cardDue = await CreateValidCardAsync("Card Due");
        await CreateTestStateAsync(cardDue.Id, -60, 2.5f, 1, 0); // Due 60 phút trước

        // Thẻ 3: Thẻ đã học, due date trong tương lai (không review)
        var cardNotDue = await CreateValidCardAsync("Card Not Due");
        await CreateTestStateAsync(cardNotDue.Id, 60, 2.5f, 1, 0); // Due 60 phút nữa

        // ----- ACT -----
        var queue = await _service.GetReviewQueueAsync(_testDeck.Id, _testUser.Id);

        // ----- ASSERT -----
        var queueList = queue.ToList();
        Assert.Equal(2, queueList.Count);
        Assert.Contains(queueList, c => c.Id == cardNew.Id); // Thẻ 1 (mới)
        Assert.Contains(queueList, c => c.Id == cardDue.Id); // Thẻ 2 (due)
        Assert.DoesNotContain(queueList, c => c.Id == cardNotDue.Id); // Thẻ 3 (chưa due)
    }

    #endregion
}