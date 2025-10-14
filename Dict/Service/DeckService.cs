using Dict.Data;
using Dict.DTO.Deck;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class DeckService : IDeckService
    {
        private readonly ApplicationDbContext _db;

        public DeckService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<DeckSummaryDto>> GetPublicDecksAsync()
        {
            return await _db.Decks
                .AsNoTracking()
                .Where(d => d.IsPublic == true) // So sánh với true để xử lý bool?
                .Select(d => new DeckSummaryDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description ?? "", // ✨ SỬA: Cung cấp giá trị mặc định
                    IsPublic = d.IsPublic ?? false,    // ✨ SỬA: Cung cấp giá trị mặc định
                    CardCount = d.Cards.Count(),
                    AuthorName = d.User.Username ?? "Anonymous"
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<DeckSummaryDto>> GetUserDecksAsync(int userId)
        {
            return await _db.Decks
                .AsNoTracking()
                .Where(deck => deck.UserId == userId)
                .OrderByDescending(deck => deck.UpdatedAt)
                .Select(deck => new DeckSummaryDto
                {
                    Id = deck.Id,
                    Name = deck.Name,
                    Description = deck.Description ?? "", // ✨ SỬA: Cung cấp giá trị mặc định
                    IsPublic = deck.IsPublic ?? false,    // ✨ SỬA: Cung cấp giá trị mặc định
                    CardCount = deck.Cards.Count(),
                    AuthorName = deck.User.Username ?? "N/A"
                })
                .ToListAsync();
        }

        public async Task<DeckDetailDto?> GetDeckDetailsAsync(int deckId, int userId)
        {
            return await _db.Decks
                .AsNoTracking()
                .Where(d => d.Id == deckId)
                .Select(d => new DeckDetailDto
                {
                    Id = d.Id,
                    Title = d.Name,
                    Description = d.Description ?? "", // ✨ SỬA: Cung cấp giá trị mặc định
                    Cards = d.Cards.Select(c => new CardDto
                    {
                        Id = c.Id,
                        CharBig = c.FrontText,
                        Meaning = c.BackText,
                        Pinyin = "",
                        NextReviewAt = c.CardStates
                            .Where(cs => cs.UserId == userId)
                            .Select(cs => cs.DueDate)
                            .FirstOrDefault() ?? DateTime.MinValue // ✨ SỬA: Cung cấp giá trị mặc định
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<DeckSummaryDto> CreateDeckAsync(DeckCreateDto deckDto, int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("Người dùng không tồn tại.");
            }

            var newDeck = new Deck
            {
                Name = deckDto.Title,
                Description = deckDto.Description,
                IsPublic = deckDto.IsPublic,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Decks.Add(newDeck);
            await _db.SaveChangesAsync();

            return new DeckSummaryDto
            {
                Id = newDeck.Id,
                Name = newDeck.Name,
                Description = newDeck.Description ?? "", // ✨ SỬA: Cung cấp giá trị mặc định
                IsPublic = newDeck.IsPublic ?? false,    // ✨ SỬA: Cung cấp giá trị mặc định
                CardCount = 0,
                AuthorName = user.Username ?? "N/A"
            };
        }

        public async Task<bool> UpdateDeckAsync(int deckId, DeckUpdateDto deckDto, int userId)
        {
            var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
            if (deck == null) return false;

            deck.Name = deckDto.Title;
            deck.Description = deckDto.Description;
            deck.IsPublic = deckDto.IsPublic;
            deck.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDeckAsync(int deckId, int userId)
        {
            var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
            if (deck == null) return false;

            _db.Decks.Remove(deck);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<CardDto> AddCardToDeckAsync(int deckId, CardCreateDto cardDto, int userId)
        {
            var deckExists = await _db.Decks.AnyAsync(d => d.Id == deckId && d.UserId == userId);
            if (!deckExists)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thêm thẻ vào bộ này.");
            }

            var newCard = new Card
            {
                DeckId = deckId,
                FrontText = cardDto.FrontText,
                BackText = cardDto.BackText,
                Tags = cardDto.Tags ?? "",
                Template = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _db.Cards.Add(newCard);
            await _db.SaveChangesAsync();

            return new CardDto
            {
                Id = newCard.Id,
                CharBig = newCard.FrontText,
                Meaning = newCard.BackText,
                Pinyin = ""
            };
        }

        public async Task<bool> UpdateCardAsync(int cardId, CardUpdateDto cardDto, int userId)
        {
            var card = await _db.Cards.Include(c => c.Deck)
                                      .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null || card.Deck.UserId != userId) return false;

            card.FrontText = cardDto.FrontText;
            card.BackText = cardDto.BackText;
            card.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCardAsync(int cardId, int userId)
        {
            var card = await _db.Cards.Include(c => c.Deck)
                                      .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null || card.Deck.UserId != userId) return false;

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDeckPublicStatusAsync(int deckId, bool isPublic, int userId)
        {
            var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
            if (deck == null) return false;

            deck.IsPublic = isPublic;
            deck.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}