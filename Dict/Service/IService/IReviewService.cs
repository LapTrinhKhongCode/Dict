using Dict.DTO.Deck;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Service.IService
{
    public interface IReviewService
    {
        Task<IEnumerable<CardDto>> GetReviewQueueAsync(int deckId, int userId);
        Task<bool> ProcessAnswerAsync(AnswerRequestDto answer, int userId);
        Task<bool> ResetCardProgressAsync(int cardId, int userId);
    }
}
