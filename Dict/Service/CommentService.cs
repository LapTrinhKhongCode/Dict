using Dict.Data;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _db;
        public CommentService(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<string?> GetCommentJson(string label)
        {
            return await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "kanji" && k.Label == label)
                .Select(k => k.CommentRawJson)
                .FirstOrDefaultAsync();
        }
    }
}
