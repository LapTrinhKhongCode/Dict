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
        private readonly UserManager<ApplicationUser> _userManager; // <-- 1. THÊM MANAGER
        private const string DefaultAvatarUrl = "https://ocrr.blob.core.windows.net/avatars/default_avatar_2ed7dd9d-82f6-46bf-b9ea-486b3a3c1b0a.jpg";

        // 2. CẬP NHẬT CONSTRUCTOR
        public DeckService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager; // <-- GÁN MANAGER
        }

        // --- CÁC HÀM GET (KHÔNG CẦN SỬA) ---
        // Các hàm này đã đúng, vì EF tự động join Deck.User (ApplicationUser)
        public async Task<IEnumerable<DeckSummaryDto>> GetPublicDecksAsync()
        {
            return await _db.Decks
                .AsNoTracking()
                .Where(d => d.IsPublic == true)
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
                        NextReviewAt = c.CardStates
                            //.Where(cs => cs.UserId == userId) // Tạm thời comment dòng này nếu logic review của bạn là mỗi user 1 state
                            .Select(cs => cs.DueDate)
                            .FirstOrDefault() ?? DateTime.MinValue
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        // --- CÁC HÀM CREATE/UPDATE (CẦN SỬA) ---

        public async Task<DeckSummaryDto> CreateDeckAsync(DeckCreateDto deckDto, int userId)
        {
            // 1️⃣ KIỂM TRA NGƯỜI DÙNG CHƯA ĐĂNG NHẬP
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Vui lòng đăng nhập để tạo bộ thẻ (deck).");
            }

            // 2️⃣ KIỂM TRA TÊN DECK
            if (string.IsNullOrWhiteSpace(deckDto.Title))
            {
                throw new ArgumentException("Tên bộ thẻ (deck) không được để trống.");
            }

            // 3️⃣ LẤY THÔNG TIN USER
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new InvalidOperationException("Người dùng không tồn tại.");
            }

            // 4️⃣ KIỂM TRA USER BỊ KHÓA
            if (!user.IsActive)
            {
                throw new InvalidOperationException("Tài khoản của bạn đã bị khóa, không thể tạo bộ thẻ.");
            }

            // 5️⃣ TẠO DECK MỚI
            var newDeck = new Deck
            {
                Name = deckDto.Title.Trim(),
                Description = deckDto.Description ?? "",
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

            return new DeckSummaryDto
            {
                Id = newDeck.Id,
                Name = newDeck.Name,
                Description = newDeck.Description,
                IsPublic = newDeck.IsPublic,
                CardCount = newDeck.Cards.Count,
                AuthorName = user.UserName ?? "N/A"
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
            // (Hàm này không tìm User, chỉ kiểm tra UserId nên không cần sửa)
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
            // (Hàm này không cần sửa, query qua Deck.User.UserName là đúng)
            var query = nameQuery.ToLower().Trim();

            var decks = await _db.Decks
                 .Where(deck => deck.IsPublic == true &&
                                deck.Name.ToLower().Contains(query))
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

            return decks;
        }
        public async Task<DeckSummaryDto> SaveDeckForUserAsync(int originalDeckId, int newOwnerUserId)
        {
            // 1. Tìm deck gốc (OK)
            var originalDeck = await _db.Decks
                .Include(d => d.User)
                .Include(d => d.Cards)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == originalDeckId);

            // 2. Kiểm tra deck gốc (OK)
            if (originalDeck == null)
                throw new KeyNotFoundException("Deck gốc không tồn tại.");
            if (originalDeck.IsPublic != true)
                throw new InvalidOperationException("Chỉ có thể lưu các bộ thẻ công khai.");
            if (originalDeck.UserId == newOwnerUserId)
                throw new InvalidOperationException("Bạn không thể lưu bộ thẻ của chính mình.");

            // 3. SỬA LẠI CÁCH TÌM USER
            var newOwner = await _userManager.FindByIdAsync(newOwnerUserId.ToString());
            if (newOwner == null)
                throw new KeyNotFoundException("Người dùng không tồn tại.");

            // 4. Kiểm tra (OK)
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

            // 5. Tạo Deck mới (OK)
            var newDeck = new Deck
            {
                Name = originalDeck.Name,
                Description = originalDeck.Description,
                IsPublic = false,
                UserId = newOwnerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Cards = new List<Card>()
            };

            // 6. Sao chép (OK)
            if (originalDeck.Cards != null && originalDeck.Cards.Any())
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

            // 7. Lưu (OK)
            _db.Decks.Add(newDeck);
            await _db.SaveChangesAsync();

            // 8. Trả về (OK)
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
                NowAuthorImageUrl = GetAvatarUrl(newOwner)
            };
        }

        private string GetAvatarUrl(ApplicationUser? user)
        {
            return string.IsNullOrEmpty(user?.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl;
        }
    }
}