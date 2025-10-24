using Dict.Data;
using Dict.DTO.Deck;
using Dict.Models;
using Dict.Service.IService;
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
                    AuthorName = d.User.Username ?? "Anonymous",
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
                    UserId = d.UserId,
                    IsPublic = d.IsPublic, // ✅ Thêm dòng này
                    Title = d.Name,
                    Description = d.Description ?? "",
                    AuthorName = d.User.Username,
                    Cards = d.Cards.Select(c => new CardDto
                    {
                        Id = c.Id,
                        CharBig = c.FrontText,
                        Meaning = c.BackText,
                        Pinyin = "",
                        NextReviewAt = c.CardStates
                            .Where(cs => cs.UserId == userId)
                            .Select(cs => cs.DueDate)
                            .FirstOrDefault() ?? DateTime.MinValue
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
                UpdatedAt = DateTime.UtcNow,
                Cards = new List<Card>() // ✅ Chuẩn bị danh sách card
            };

            // ✅ Nếu có cards được gửi kèm
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

            return new DeckSummaryDto
            {
                Id = newDeck.Id,
                Name = newDeck.Name,
                Description = newDeck.Description ?? "",
                IsPublic = newDeck.IsPublic,
                CardCount = newDeck.Cards.Count,
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

        public async Task<List<CardDto>> AddCardToDeckAsync(int deckId, List<CardCreateDto> cardDtos, int userId)
        {
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
            var card = await _db.Cards
                .Include(c => c.CardStates)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
                return false;

            // Xóa tất cả card_states liên quan
            _db.CardStates.RemoveRange(card.CardStates);

            // Sau đó xóa card
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
        public async Task<IEnumerable<UserDeckSummary>> GetPublicDecksByUsernameAsync(string username)
        {
            // Tìm kiếm các bộ thẻ thuộc về người dùng có username tương ứng
            // và chỉ lấy những bộ thẻ có trạng thái IsPublic = true.
            var decks = await _db.Decks
               .Where(deck => deck.User.Username.ToLower() == username.ToLower() && deck.IsPublic == true)
                .Select(deck => new UserDeckSummary
                {
                    Id = deck.Id,
                    Name = deck.Name,
                    Description = deck.Description,
                    CardCount = deck.Cards.Count(),
                    AuthorUsername = deck.User.Username // Lấy username của tác giả
                })
                .ToListAsync();

            return decks;
        }
        public async Task<IEnumerable<UserDeckSummary>> SearchPublicDecksByNameAsync(string nameQuery)
        {
            var query = nameQuery.ToLower().Trim(); // Chuẩn hóa từ khóa tìm kiếm

            var decks = await _db.Decks
                // Chỉ tìm trong các bộ thẻ công khai
                .Where(deck => deck.IsPublic == true &&
                               // Tên bộ thẻ chứa từ khóa (không phân biệt hoa/thường)
                               deck.Name.ToLower().Contains(query))
                .Select(deck => new UserDeckSummary
                {
                    Id = deck.Id,
                    Name = deck.Name,
                    Description = deck.Description,
                    CardCount = deck.Cards.Count(),
                    AuthorUsername = deck.User.Username
                })
                .ToListAsync();

            return decks;
        }
        public async Task<DeckSummaryDto> SaveDeckForUserAsync(int originalDeckId, int newOwnerUserId)
        {
            // 1. Tìm deck gốc, kèm theo User (tác giả gốc) và Cards
            var originalDeck = await _db.Decks
                .Include(d => d.User)   // Tác giả gốc
                .Include(d => d.Cards)  // Card gốc
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == originalDeckId);

            // 2. Kiểm tra deck gốc hợp lệ
            if (originalDeck == null)
                throw new KeyNotFoundException("Deck gốc không tồn tại.");
            if (originalDeck.IsPublic != true)
                throw new InvalidOperationException("Chỉ có thể lưu các bộ thẻ công khai.");
            if (originalDeck.UserId == newOwnerUserId)
                throw new InvalidOperationException("Bạn không thể lưu bộ thẻ của chính mình.");

            // 3. Tìm chủ sở hữu mới
            var newOwner = await _db.Users.FindAsync(newOwnerUserId);
            if (newOwner == null)
                throw new KeyNotFoundException("Người dùng không tồn tại.");

            // 4. Kiểm tra xem user đã lưu deck này chưa (theo tên, mô tả, và số lượng thẻ)
            var originalCardCount = originalDeck.Cards?.Count ?? 0;

            bool alreadySaved = await _db.Decks
                .Include(d => d.Cards)
                .AnyAsync(d =>
                    d.UserId == newOwnerUserId &&
                    d.Name == originalDeck.Name &&
                    d.Description == originalDeck.Description &&
                    d.Cards.Count == originalCardCount);

            if (alreadySaved)
                throw new InvalidOperationException("Bạn đã lưu bộ thẻ này trước đó.");

            // 5. Tạo Deck mới (bản sao)
            var newDeck = new Deck
            {
                Name = originalDeck.Name,
                Description = originalDeck.Description,
                IsPublic = false, // Bản sao mặc định là Private
                UserId = newOwnerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Cards = new List<Card>()
            };

            // 6. Sao chép các Cards từ deck gốc
            if (originalDeck.Cards != null && originalDeck.Cards.Any())
            {
                foreach (var originalCard in originalDeck.Cards)
                {
                    newDeck.Cards.Add(new Card
                    {
                        FrontText = originalCard.FrontText,
                        BackText = originalCard.BackText,
                        Tags = originalCard.Tags,
                        Template = originalCard.Template,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // 7. Lưu deck mới vào DB
            _db.Decks.Add(newDeck);
            await _db.SaveChangesAsync();

            // 8. Trả về DTO kết quả
            return new DeckSummaryDto
            {
                Id = newDeck.Id,
                Name = newDeck.Name,
                Description = newDeck.Description ?? "",
                IsPublic = newDeck.IsPublic ?? false,
                CardCount = newDeck.Cards.Count,
                // Thông tin tác giả GỐC
                AuthorName = originalDeck.User?.Username ?? "Unknown",
                AuthorImageUrl = GetAvatarUrl(originalDeck.User),
                // Thông tin người CLONE (chủ sở hữu hiện tại)
                NowAuthorName = newOwner.Username,
                NowAuthorImageUrl = GetAvatarUrl(newOwner)
            };
        }

        private string GetAvatarUrl(User? user)
        {
            return string.IsNullOrEmpty(user?.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl;
        }
    }
}