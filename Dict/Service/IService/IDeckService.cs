using Dict.DTO.Deck;

namespace Dict.Service.IService
{
    public interface IDeckService
    {
        Task<IEnumerable<DeckSummaryDto>> GetPublicDecksAsync(int page = 1, int pageSize = 20);
        Task<IEnumerable<DeckSummaryDto>> GetUserDecksAsync(int userId);

        // ✨ SỬA: Thêm tham số userId để khớp với file Service
        Task<DeckDetailDto?> GetDeckDetailsAsync(int deckId, int userId);

        Task<DeckSummaryDto> CreateDeckAsync(DeckCreateDto deckDto, int userId);
        Task<bool> UpdateDeckAsync(int deckId, DeckUpdateDto deckDto, int userId);
        Task<bool> DeleteDeckAsync(int deckId, int userId);
        Task<List<CardDto>> AddCardToDeckAsync(int deckId, List<CardCreateDto> cardDto, int userId);
        Task<bool> UpdateCardAsync(int cardId, CardUpdateDto cardDto, int userId);
        Task<bool> DeleteCardAsync(int cardId, int userId);
        Task<bool> SetDeckPublicStatusAsync(int deckId, bool isPublic, int userId);
        Task<IEnumerable<UserDeckSummary>> GetPublicDecksByUsernameAsync(string username);
        Task<IEnumerable<UserDeckSummary>> SearchPublicDecksByNameAsync(string nameQuery);
        Task<DeckSummaryDto> SaveDeckForUserAsync(int originalDeckId, int newOwnerUserId);
    }
}