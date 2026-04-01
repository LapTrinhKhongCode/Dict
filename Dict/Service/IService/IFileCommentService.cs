using Dict.DTO;

namespace Dict.Service.IService
{
    public interface IFileCommentService
    {
        Task<FileCommentDTO> AddCommentAsync(int userId, CreateCommentDTO dto);
        Task<IEnumerable<FileCommentDTO>> GetCommentsByFileAsync(int mediaStoreId);
        Task<bool> DeleteCommentAsync(int userId, int commentId);
    }
}
