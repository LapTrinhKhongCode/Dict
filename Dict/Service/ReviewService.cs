using Dict.Data;
using Dict.DTO.Deck;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class ReviewService : IReviewService
    {

        private readonly ApplicationDbContext _db;

        // --- CÁC HẰNG SỐ CẤU HÌNH THUẬT TOÁN ANKI ---
        private const float STARTING_EASE = 2.5f; // Ease Factor mặc định (250%)
        private const float EASE_MODIFIER_AGAIN = -0.20f;
        private const float EASE_MODIFIER_HARD = -0.15f;
        private const float EASE_MODIFIER_EASY = 0.15f;
        private const float MIN_EASE = 1.3f; // Ease Factor tối thiểu (130%)

        private const float HARD_INTERVAL_MULTIPLIER = 1.2f;
        private const float EASY_BONUS = 1.3f;

        // Các bước học cho thẻ mới/học lại (tính bằng phút)
        // Ví dụ: Lần đầu nhấn Good -> 1 phút, lần sau nhấn Good -> 10 phút
        private static readonly int[] LEARNING_STEPS_MINUTES = { 1, 10 };
        // Khi một thẻ học lại xong các bước, nó sẽ được hẹn lại sau 1 ngày
        private const int GRADUATING_INTERVAL_DAYS = 1;
        // Khi một thẻ mới được nhấn "Easy", nó tốt nghiệp ngay và hẹn lại sau 4 ngày
        private const int EASY_INTERVAL_DAYS = 4;

        public ReviewService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ProcessAnswerAsync(AnswerRequestDto answer, int userId)
        {
            var cardState = await _db.CardStates
                .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.CardId == answer.CardId);

            var isNewCard = cardState == null;
            var previousInterval = 0;

            if (isNewCard)
            {
                cardState = new CardState
                {
                    UserId = userId,
                    CardId = answer.CardId,
                    Ease = STARTING_EASE,
                    Interval = 0,
                    Reps = 0,
                    Lapses = 0,
                    DeckPosition = 0,
                    Suspended = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.CardStates.Add(cardState);
            }
            else
            {
                previousInterval = cardState.Interval ?? 0;
            }

            // === LOGIC SRS ANKI MỚI - PHÂN BIỆT RÕ RÀNG TRẠNG THÁI HỌC VÀ ÔN TẬP ===

            // Một thẻ được coi là đang ôn tập (review) nếu interval trước đó của nó >= 1 ngày.
            // Ngược lại, nó đang trong giai đoạn học (learning/relearning).
            bool isReviewCard = previousInterval >= (GRADUATING_INTERVAL_DAYS * 24 * 60);

            if (isReviewCard)
            {
                // --- LOGIC CHO CÁC THẺ ĐANG ÔN TẬP (INTERVAL >= 1 NGÀY) ---
                switch (answer.Quality)
                {
                    case 1: // Again (Lapse) -> Thẻ bị quên, quay lại giai đoạn học
                        cardState.Lapses = (cardState.Lapses ?? 0) + 1;
                        cardState.Ease = Math.Max(MIN_EASE, (cardState.Ease ?? STARTING_EASE) + EASE_MODIFIER_AGAIN);
                        // Reset về bước học đầu tiên
                        cardState.Interval = LEARNING_STEPS_MINUTES[0];
                        break;
                    case 2: // Hard
                        cardState.Interval = (int)(previousInterval * HARD_INTERVAL_MULTIPLIER);
                        cardState.Ease = Math.Max(MIN_EASE, (cardState.Ease ?? STARTING_EASE) + EASE_MODIFIER_HARD);
                        cardState.Reps = (cardState.Reps ?? 0) + 1;
                        break;
                    case 3: // Good
                        cardState.Interval = (int)(previousInterval * (cardState.Ease ?? STARTING_EASE));
                        cardState.Reps = (cardState.Reps ?? 0) + 1;
                        break;
                    case 4: // Easy
                        cardState.Interval = (int)(previousInterval * (cardState.Ease ?? STARTING_EASE) * EASY_BONUS);
                        cardState.Ease = (cardState.Ease ?? STARTING_EASE) + EASE_MODIFIER_EASY;
                        cardState.Reps = (cardState.Reps ?? 0) + 1;
                        break;
                }
            }
            else
            {
                // --- LOGIC CHO CÁC THẺ MỚI/ĐANG HỌC (INTERVAL < 1 NGÀY) ---
                switch (answer.Quality)
                {
                    case 1: // Again -> Quay lại bước đầu tiên
                        cardState.Interval = LEARNING_STEPS_MINUTES[0];
                        break;
                    case 2: // Hard -> Lặp lại bước đầu tiên
                        cardState.Interval = LEARNING_STEPS_MINUTES[0];
                        break;
                    case 3: // Good -> Chuyển sang bước tiếp theo hoặc tốt nghiệp
                        // Tìm xem thẻ đang ở bước nào trong chuỗi học
                        var currentStepIndex = Array.IndexOf(LEARNING_STEPS_MINUTES, previousInterval);
                        if (currentStepIndex < 0) currentStepIndex = -1; // Nếu không tìm thấy, coi như chưa bắt đầu

                        var nextStepIndex = currentStepIndex + 1;
                        if (nextStepIndex < LEARNING_STEPS_MINUTES.Length)
                        {
                            // Chuyển sang bước học tiếp theo (ví dụ: từ 1 phút -> 10 phút)
                            cardState.Interval = LEARNING_STEPS_MINUTES[nextStepIndex];
                        }
                        else
                        {
                            // Đã hoàn thành tất cả các bước, "tốt nghiệp" thẻ
                            cardState.Interval = GRADUATING_INTERVAL_DAYS * 24 * 60; // 1 ngày
                        }
                        break;
                    case 4: // Easy -> Tốt nghiệp ngay lập tức
                        cardState.Interval = EASY_INTERVAL_DAYS * 24 * 60; // 4 ngày
                        break;
                }
            }

            // Cập nhật các thông số chung
            cardState.DueDate = DateTime.UtcNow.AddMinutes(cardState.Interval.Value);
            cardState.LastReviewedAt = DateTime.UtcNow;
            cardState.UpdatedAt = DateTime.UtcNow;

            // Ghi log
            var log = new ReviewLog
            {
                CardState = cardState,
                UserId = userId,
                CardId = answer.CardId,
                Timestamp = DateTime.UtcNow,
                Quality = answer.Quality,
                PreviousInterval = previousInterval,
                NewInterval = cardState.Interval,
                Ease = cardState.Ease,
                TimeTakenMs = 5000,
                Note = ""
            };
            _db.ReviewLogs.Add(log);

            await _db.SaveChangesAsync();
            return true;
        }

        // ... (GetReviewQueueAsync và ResetCardProgressAsync không đổi) ...
        public async Task<IEnumerable<CardDto>> GetReviewQueueAsync(int deckId, int userId)
        {
            var now = DateTime.UtcNow;
            // LEFT JOIN card_states một lần — tránh 2 ANY subquery trùng nhau
            return await _db.Cards
                .AsNoTracking()
                .Where(c => c.DeckId == deckId)
                .Select(c => new
                {
                    c.Id,
                    c.FrontText,
                    c.BackText,
                    State = c.CardStates.FirstOrDefault(cs => cs.UserId == userId)
                })
                .Where(x => x.State == null || x.State.DueDate <= now)
                .Select(x => new CardDto
                {
                    Id = x.Id,
                    CharBig = x.FrontText,
                    Pinyin = "",
                    Meaning = x.BackText,
                    NextReviewAt = x.State != null ? (x.State.DueDate ?? DateTime.MinValue) : DateTime.MinValue
                })
                .ToListAsync();
        }

        public async Task<bool> ResetCardProgressAsync(int cardId, int userId)
        {
            var cardState = await _db.CardStates
                .Include(cs => cs.ReviewLogs) // Tải các logs liên quan
                .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.CardId == cardId);

            if (cardState != null)
            {
                if (cardState.ReviewLogs.Any())
                {
                    _db.ReviewLogs.RemoveRange(cardState.ReviewLogs);
                }
                _db.CardStates.Remove(cardState);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

