using Dict.Models;
using Microsoft.EntityFrameworkCore;

namespace Dict.Data
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Phương thức mở rộng để nạp (Include) tất cả dữ liệu liên quan
        /// cần thiết cho việc xây dựng JSON hoàn chỉnh của một Entry.
        /// </summary>
        public static IQueryable<Entry> IncludeAllData(this IQueryable<Entry> query)
        {
            return query
                // Nạp Word và các Relation của Word đó
                .Include(e => e.Words)
                    .ThenInclude(w => w.Relations)
                        .ThenInclude(r => r.RelatedWord) // Nạp cả RelatedWord
                                                         // Nạp Senses và các bảng con trực tiếp của Senses
                .Include(e => e.Senses)
                    .ThenInclude(s => s.Glosses)
                .Include(e => e.Senses)
                    .ThenInclude(s => s.Examples)
                .Include(e => e.Senses)
                    .ThenInclude(s => s.SynsetEntries) // Nạp SynsetEntries
                        .ThenInclude(se => se.SynonymItems) // Nạp SynonymItems từ SynsetEntries
                                                            // Nạp Media (chỉ lấy hình ảnh)
                .Include(e => e.Media.Where(m => m.MediaType == "image"));
        }
    }
}
