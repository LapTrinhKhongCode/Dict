using Dict.Data;
using Dict.DTO.Deck;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.AspNetCore.Identity; // <-- THÊM DÒNG NÀY
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;

namespace Dict.Service
{
    public class DeckService : IDeckService
    {
        private readonly ApplicationDbContext _db;
        private const string DefaultAvatarUrl = "https://ocrr.blob.core.windows.net/avatars/default_avatar_2ed7dd9d-82f6-46bf-b9ea-486b3a3c1b0a.jpg";

        public DeckService(ApplicationDbContext db)
        {
            _db = db;
        }

        // --- CÁC HÀM GET (KHÔNG CẦN SỬA) ---
        // Các hàm này đã đúng, vì EF tự động join Deck.User (ApplicationUser)
        public async Task<IEnumerable<DeckSummaryDto>> GetPublicDecksAsync(int page = 1, int pageSize = 20)
        {
            return await _db.Decks
                .AsNoTracking()
                .Where(d => d.IsPublic == true)
                .OrderByDescending(d => d.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeckSummaryDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description ?? "",
                    IsPublic = d.IsPublic ?? false,
                    CardCount = d.Cards.Count(),
                    AuthorName = d.User.UserName ?? "Anonymous",
                    AuthorImageUrl = d.User.AvatarUrl
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
                    Description = deck.Description ?? "",
                    IsPublic = deck.IsPublic ?? false,
                    CardCount = deck.Cards.Count(),
                    AuthorName = deck.User.UserName ?? "N/A"
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
                    UserId = d.UserId,
                    IsPublic = d.IsPublic,
                    Title = d.Name,
                    Description = d.Description ?? "",
                    AuthorName = d.User.UserName,
                    Cards = d.Cards.Select(c => new CardDto
                    {
                        Id = c.Id,
                        CharBig = c.FrontText,
                        Meaning = c.BackText,
                        Pinyin = "",
                        // Filter đúng theo userId — tránh lấy state của người khác
                        NextReviewAt = c.CardStates
                            .Where(cs => cs.UserId == userId)
                            .Select(cs => cs.DueDate)
                            .FirstOrDefault() ?? DateTime.MinValue
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        // --- CÁC HÀM CREATE/UPDATE (CẦN SỬA) ---

        public async Task<DeckSummaryDto> CreateDeckAsync(DeckCreateDto deckDto, int userId)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new Exception("Người dùng không tồn tại.");

            var newDeck = new Deck
            {
                Name = deckDto.Title,
                Description = deckDto.Description,
                IsPublic = deckDto.IsPublic,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Cards = new List<Card>()
            };

            if (deckDto.Cards != null && deckDto.Cards.Any())
            {
                foreach (var cardDto in deckDto.Cards)
                {
                    newDeck.Cards.Add(new Card
                    {
                        FrontText = cardDto.FrontText,
                        BackText = cardDto.BackText,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Tags = cardDto.Tags ?? ""
                    });
                }
            }

            _db.Decks.Add(newDeck);
            await _db.SaveChangesAsync();

            var userName = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            return new DeckSummaryDto
            {
                Id = newDeck.Id,
                Name = newDeck.Name,
                Description = newDeck.Description ?? "",
                IsPublic = newDeck.IsPublic,
                CardCount = newDeck.Cards.Count,
                AuthorName = userName ?? "N/A"
            };
        }


        public async Task<bool> UpdateDeckAsync(int deckId, DeckUpdateDto deckDto, int userId)
        {
            // (Hàm này không tìm User, chỉ kiểm tra UserId nên không cần sửa)
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
            // (Hàm này không tìm User, chỉ kiểm tra UserId nên không cần sửa)
            var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
            if (deck == null) return false;

            _db.Decks.Remove(deck);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<CardDto>> AddCardToDeckAsync(int deckId, List<CardCreateDto> cardDtos, int userId)
        {
            // (Hàm này không tìm User, chỉ kiểm tra UserId nên không cần sửa)
            var deckExists = await _db.Decks.AnyAsync(d => d.Id == deckId && d.UserId == userId);
            if (!deckExists)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thêm thẻ vào bộ này.");
            }

            var newCards = cardDtos.Select(dto => new Card
            {
                DeckId = deckId,
                FrontText = dto.FrontText,
                BackText = dto.BackText,
                Tags = dto.Tags ?? "",
                Template = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }).ToList();

            _db.Cards.AddRange(newCards);
            await _db.SaveChangesAsync();

            return newCards.Select(c => new CardDto
            {
                Id = c.Id,
                CharBig = c.FrontText,
                Meaning = c.BackText,
                Pinyin = ""
            }).ToList();
        }


        public async Task<bool> UpdateCardAsync(int cardId, CardUpdateDto cardDto, int userId)
        {
            // Chỉ select DeckId thay vì load toàn bộ Deck object
            var card = await _db.Cards
                .Where(c => c.Id == cardId)
                .Select(c => new { c.Id, c.Deck.UserId })
                .FirstOrDefaultAsync();

            if (card == null || card.UserId != userId) return false;

            await _db.Cards
                .Where(c => c.Id == cardId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.FrontText, cardDto.FrontText)
                    .SetProperty(c => c.BackText, cardDto.BackText)
                    .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));

            return true;
        }

        public async Task<bool> DeleteCardAsync(int cardId, int userId)
        {
            // (Hàm này không tìm User, chỉ kiểm tra UserId nên không cần sửa)
            var card = await _db.Cards
                .Include(c => c.CardStates)
                .Include(c => c.Deck) // <-- THÊM INCLUDE DECK
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
                return false;

            // Kiểm tra xem user có sở hữu deck chứa card này không
            if (card.Deck == null || card.Deck.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa thẻ này.");
            }

            // Xóa tất cả card_states liên quan
            _db.CardStates.RemoveRange(card.CardStates);

            // Sau đó xóa card
            _db.Cards.Remove(card);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDeckPublicStatusAsync(int deckId, bool isPublic, int userId)
        {
            // (Hàm này không tìm User, chỉ kiểm tra UserId nên không cần sửa)
            var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
            if (deck == null) return false;

            deck.IsPublic = isPublic;
            deck.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<UserDeckSummary>> GetPublicDecksByUsernameAsync(string username)
        {
            // (Hàm này không cần sửa, query qua Deck.User.UserName là đúng)
            var decks = await _db.Decks
              .Where(deck => deck.User.UserName.ToLower() == username.ToLower() && deck.IsPublic == true)
               .Select(deck => new UserDeckSummary
               {
                   Id = deck.Id,
                   Name = deck.Name,
                   Description = deck.Description,
                   CardCount = deck.Cards.Count(),
                   AuthorUsername = deck.User.UserName
               })
               .ToListAsync();

            return decks;
        }
        public async Task<IEnumerable<UserDeckSummary>> SearchPublicDecksByNameAsync(string nameQuery)
        {
            var query = nameQuery.Trim();

            return await _db.Decks
                .AsNoTracking()
                .Where(deck => deck.IsPublic == true &&
                               EF.Functions.Like(deck.Name, $"%{query}%"))
                .OrderByDescending(deck => deck.UpdatedAt)
                .Take(50)
                .Select(deck => new UserDeckSummary
                {
                    Id = deck.Id,
                    Name = deck.Name,
                    Description = deck.Description,
                    CardCount = deck.Cards.Count(),
                    AuthorUsername = deck.User.UserName,
                    Avatar = deck.User.AvatarUrl
                })
                .ToListAsync();
        }
        public async Task<DeckSummaryDto> SaveDeckForUserAsync(int originalDeckId, int newOwnerUserId)
        {
            var originalDeck = await _db.Decks
                .Include(d => d.User)
                .Include(d => d.Cards)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == originalDeckId);

            if (originalDeck == null)
                throw new KeyNotFoundException("Deck gốc không tồn tại.");
            if (originalDeck.IsPublic != true)
                throw new InvalidOperationException("Chỉ có thể lưu các bộ thẻ công khai.");
            if (originalDeck.UserId == newOwnerUserId)
                throw new InvalidOperationException("Bạn không thể lưu bộ thẻ của chính mình.");

            // Dùng DB query trực tiếp thay vì UserManager — nhẹ hơn nhiều
            var newOwner = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == newOwnerUserId)
                .Select(u => new { u.Id, u.UserName, u.AvatarUrl })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

            var originalCardCount = originalDeck.Cards?.Count ?? 0;

            bool alreadySaved = await _db.Decks
                .AnyAsync(d =>
                    d.UserId == newOwnerUserId &&
                    d.Name == originalDeck.Name &&
                    d.Description == originalDeck.Description &&
                    d.Cards.Count == originalCardCount);

            if (alreadySaved)
                throw new InvalidOperationException("Bạn đã lưu bộ thẻ này trước đó.");

            var newDeck = new Deck
            {
                Name = originalDeck.Name,
                Description = originalDeck.Description,
                IsPublic = false,
                UserId = newOwnerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Cards = originalDeck.Cards?.Select(c => new Card
                {
                    FrontText = c.FrontText,
                    BackText = c.BackText,
                    Tags = c.Tags,
                    Template = c.Template,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList() ?? new List<Card>()
            };

            _db.Decks.Add(newDeck);
            await _db.SaveChangesAsync();

            return new DeckSummaryDto
            {
                Id = newDeck.Id,
                Name = newDeck.Name,
                Description = newDeck.Description ?? "",
                IsPublic = newDeck.IsPublic ?? false,
                CardCount = newDeck.Cards.Count,
                AuthorName = originalDeck.User?.UserName ?? "Unknown",
                AuthorImageUrl = GetAvatarUrl(originalDeck.User),
                NowAuthorName = newOwner.UserName,
                NowAuthorImageUrl = string.IsNullOrEmpty(newOwner.AvatarUrl) ? DefaultAvatarUrl : newOwner.AvatarUrl
            };
        }

        private string GetAvatarUrl(ApplicationUser? user)
        {
            return string.IsNullOrEmpty(user?.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl;
        }
    }
}