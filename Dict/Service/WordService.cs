using Dict.Data;
using Dict.Service.IService;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class WordService : IWordService
    {
        private readonly ApplicationDbContext _db;

        public WordService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<string?> GetWordJson(string label)
        {
            return await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "word" &&
                    (EF.Functions.Collate(k.Label, "Japanese_CS_AS_KS_WS") == label ||
                     EF.Functions.Collate(k.Phonetic, "Japanese_CS_AS_KS_WS") == label))
                .OrderBy(k => k.Id)
                .Select(k => k.RawJson)
                .FirstOrDefaultAsync();
        }
    }
}
