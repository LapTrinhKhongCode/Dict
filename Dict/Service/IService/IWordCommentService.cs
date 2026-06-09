using Dict.DTO;

namespace Dict.Service.IService
{
    public interface IWordCommentService
    {
        Task<IEnumerable<WordCommentDTO>> GetByWordAsync(string wordLabel);
        Task<WordCommentDTO> AddAsync(int userId, CreateWordCommentDTO dto);
        Task<bool> DeleteAsync(int userId, int commentId);
    }
}
