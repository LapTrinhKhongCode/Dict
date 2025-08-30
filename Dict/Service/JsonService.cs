using Dict.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dict.Service
{
    public class JsonService
    {
        private readonly ApplicationDbContext _db;
        public JsonService(ApplicationDbContext db)
        {
            _db = db;
        }
        public Task Serialize()
        {
            // Lấy hết RawJson (có thể limit test trước, ví dụ Take(5000))
            var allJsons = _db.Entries
                .AsNoTracking()
                .Where(e => e.RawJson != null)
                .Where(e => e.Type == "word")
                .Select(e => e.RawJson)
                .ToList();

            var formats = new Dictionary<string, int>();
            var samples = new Dictionary<string, List<string>>();

            foreach (var raw in allJsons)
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;

                    // Tạo signature theo các key cấp 1 (sorted)
                    var keys = string.Join(",", root.EnumerateObject()
                                                    .Select(p => p.Name)
                                                    .OrderBy(k => k));

                    // Đếm số lượng
                    if (!formats.ContainsKey(keys))
                        formats[keys] = 0;
                    formats[keys]++;

                    // Lưu tối đa 2 ví dụ cho mỗi format
                    if (!samples.ContainsKey(keys))
                        samples[keys] = new List<string>();
                    if (samples[keys].Count < 2)
                        samples[keys].Add(raw);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid JSON: {ex.Message}");
                }
            }

            // In ra thống kê
            Console.WriteLine("== Các dạng JSON tìm thấy ==");
            foreach (var f in formats)
            {
                Console.WriteLine($"Format: {f.Key} - Count: {f.Value}");
            }

            // Lưu ví dụ ra file
            Directory.CreateDirectory("JsonSamplesWord");

            foreach (var kv in samples)
            {
                var safeKey = kv.Key.Replace(",", "_");
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    File.WriteAllText($"JsonSamplesWord/Sample_{safeKey}_{i + 1}.json", kv.Value[i]);
                }
            }

            Console.WriteLine("Đã xuất ví dụ vào thư mục JsonSamplesWord/");
            return Task.CompletedTask;
        }
        
    }
}
