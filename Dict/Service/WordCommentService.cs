using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class WordCommentService : IWordCommentService
    {
        private readonly ApplicationDbContext _db;

        public WordCommentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<WordCommentDTO>> GetByWordAsync(string wordLabel)
        {
            return await _db.WordComments
                .Include(c => c.User)
                .Where(c => c.WordLabel == wordLabel && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new WordCommentDTO
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User.UserName ?? "Thành viên",
                    AvatarUrl = c.User.AvatarUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<WordCommentDTO> AddAsync(int userId, CreateWordCommentDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 1000)
                throw new ArgumentException("Nội dung không hợp lệ.");

            var comment = new WordComment
            {
                WordLabel = dto.WordLabel,
                UserId = userId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.WordComments.Add(comment);
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);

            return new WordCommentDTO
            {
                Id = comment.Id,
                UserId = userId,
                UserName = user?.UserName ?? "Thành viên",
                AvatarUrl = user?.AvatarUrl,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<bool> DeleteAsync(int userId, int commentId)
        {
            var comment = await _db.WordComments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

            if (comment == null) return false;

            comment.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
