using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Dict.DataEnricher
{
    class Program
    {
        private static string _connectionString = string.Empty;
        private static string _geminiApiKey = string.Empty;

        // Dùng HttpMessageHandler để tối ưu hóa kết nối mạng khi chạy đa luồng
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 100 // Cho phép mở tối đa 100 kết nối cùng lúc tới Google
        });

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // =========================================================
            // 1. ĐỌC CONFIG
            // =========================================================
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _connectionString = config["ConnectionStrings:DefaultConnection"] ?? "";
            _geminiApiKey = config["GoogleCloud:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(_geminiApiKey) || string.IsNullOrEmpty(_connectionString))
            {
                Console.WriteLine("❌ LỖI: Không đọc được Config từ appsettings.json!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("================================================================");
            Console.WriteLine("🚀 KHỞI CHẠY TOOL SINH VÍ DỤ AI - PHIÊN BẢN TỐC ĐỘ CAO (PRO MAX)");
            Console.WriteLine("================================================================\n");

            // =========================================================
            // CẤU HÌNH ĐA LUỒNG (SỨC MẠNH CỦA $300 CLOUD)
            // =========================================================
            int batchSize = 500; // Mỗi mẻ lấy 500 từ thay vì 20 từ
            int maxDegreeOfParallelism = 10; // Chạy 20 từ CÙNG MỘT LÚC (Ông có thể tăng lên 50 nếu muốn mạo hiểm)

            bool hasMoreWork = true;
            int totalProcessed = 0;

            // Bộ đếm an toàn cho Thread-safe
            int successCount = 0;
            int failCount = 0;

            while (hasMoreWork)
            {
                var pendingWords = await FetchPendingWords(batchSize);

                if (pendingWords.Count == 0)
                {
                    Console.WriteLine("\n🎉 QUÁ TUYỆT! Toàn bộ từ vựng chuyên ngành đều đã có ví dụ.");
                    hasMoreWork = false;
                    break;
                }

                Console.WriteLine($"\n🔍 Đã bốc lên {pendingWords.Count} từ. Bắt đầu ép xung đa luồng ({maxDegreeOfParallelism} luồng)...");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Dùng SemaphoreSlim để giới hạn số luồng chạy song song
                using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
                var tasks = new List<Task>();

                foreach (var word in pendingWords)
                {
                    await semaphore.WaitAsync(); // Chờ đến khi có luồng trống

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var examples = await GenerateExamplesWithGemini(word.Label, word.Meaning, word.Domain);

                            if (examples.Count > 0)
                            {
                                await SaveExamplesToDatabase(word.SenseId, examples);
                                Interlocked.Increment(ref successCount);
                                Console.WriteLine($"   ✅ [Thành công] {word.Label}");
                            }
                            else
                            {
                                Interlocked.Increment(ref failCount);
                                Console.WriteLine($"   ❌ [Lỗi AI] {word.Label}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failCount);
                            Console.WriteLine($"   ❌ [Lỗi Code] {word.Label}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release(); // Trả luồng lại cho hệ thống
                        }
                    }));
                }

                // Chờ tất cả các task trong mẻ này chạy xong
                await Task.WhenAll(tasks);
                watch.Stop();

                totalProcessed += pendingWords.Count;
                Console.WriteLine($"\n🔄 Đã xong mẻ {pendingWords.Count} từ trong {watch.Elapsed.TotalSeconds:F1} giây!");
                Console.WriteLine($"   Thống kê: {successCount} thành công, {failCount} thất bại. Tổng tiến độ: {totalProcessed}");
            }

            Console.WriteLine("\n🚀 CHƯƠNG TRÌNH ĐÃ HOÀN THÀNH NHIỆM VỤ!");
            Console.ReadLine();
        }

        // =========================================================================
        // HÀM 1: LẤY CÁC TỪ CHƯA CÓ VÍ DỤ TỪ SQL
        // =========================================================================
        private static async Task<List<(int SenseId, string Label, string Meaning, string Domain)>> FetchPendingWords(int limit)
        {
            var list = new List<(int, string, string, string)>();

            string sql = $@"
                SELECT TOP {limit} s.Id, e.Label, g.Text AS Meaning, s.Domain
                FROM [entries] e
                INNER JOIN [senses] s ON e.Id = s.EntryId
                INNER JOIN [glosses] g ON s.Id = g.SenseId
                WHERE s.[Domain] IN ('Architecture', 'Electronics', 'Mechanical', 'IT')
                  AND NOT EXISTS (SELECT 1 FROM [examples] ex WHERE ex.SenseId = s.Id)";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                ));
            }
            return list;
        }

        // =========================================================================
        // HÀM 2: GỌI GEMINI SINH JSON (ĐÃ BẬT LOG LỖI CHI TIẾT)
        // =========================================================================
        // =========================================================================
        // HÀM 2: GỌI GEMINI SINH JSON (ĐÃ TRANG BỊ CƠ CHẾ AUTO-RETRY CHỐNG 503)
        // =========================================================================
        private static async Task<List<GeneratedExampleDto>> GenerateExamplesWithGemini(string keyword, string meaning, string domain)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Bạn là một kỹ sư người Nhật viết tài liệu kỹ thuật.");
            promptBuilder.AppendLine($"Hãy tạo 3 câu ví dụ thực tế, chuyên nghiệp cho từ vựng: '{keyword}' (Nghĩa: {meaning}) trong lĩnh vực {domain}.");
            promptBuilder.AppendLine("Yêu cầu:");
            promptBuilder.AppendLine("- Dùng văn phong tài liệu kỹ thuật (thể thường/thể Ru/Ta).");
            promptBuilder.AppendLine("- Trả về ĐÚNG định dạng JSON array sau, không kèm markdown code block, không thêm text giải thích:");
            promptBuilder.AppendLine("[");
            promptBuilder.AppendLine("  { \"ContentJp\": \"câu tiếng Nhật\", \"ContentTranslated\": \"câu tiếng Việt\", \"Transcription\": \"phiên âm hiragana/katakana cách nhau bởi dấu cách\" }");
            promptBuilder.AppendLine("]");

            string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
            string requestUrl = $"{endpoint}?key={_geminiApiKey}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = promptBuilder.ToString() } } } },
                generationConfig = new
                {
                    temperature = 0.2,
                    response_mime_type = "application/json"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            int maxRetries = 3; // Thử lại tối đa 3 lần
            int delayMs = 2000; // Khởi điểm đợi 2 giây

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.PostAsync(requestUrl, content);

                    // THÀNH CÔNG: Đọc JSON và thoát vòng lặp trả về luôn
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonResponse);

                        var resultText = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text").GetString() ?? "";

                        resultText = resultText.Replace("```json", "").Replace("```", "").Trim();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var examples = JsonSerializer.Deserialize<List<GeneratedExampleDto>>(resultText, options);

                        return examples ?? new List<GeneratedExampleDto>();
                    }

                    // THẤT BẠI: Nếu là lỗi 503 (Server bận) hoặc 429 (Giới hạn tỷ lệ) -> Chạy Retry
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"   ⏳ [Server Bận {response.StatusCode}] {keyword} - Thử lại lần {i + 1}/{maxRetries} sau {delayMs / 1000}s...");
                        await Task.Delay(delayMs);
                        delayMs *= 2; // Lần sau đợi lâu hơn gấp đôi (Exponential backoff)
                        continue;     // Vòng lại đầu loop for
                    }
                    else
                    {
                        // Lỗi khác (vd 400 Bad Request do vi phạm policy) -> Bỏ qua luôn không retry
                        string errorDetail = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"   ⚠️ [LỖI {response.StatusCode}] {keyword} - {errorDetail}");
                        return new List<GeneratedExampleDto>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ [LỖI MẠNG/CODE] {keyword} - {ex.Message}");
                    // Lỗi mạng đứt ngang cũng thử lại luôn
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
            }

            Console.WriteLine($"   ❌ [BỎ CUỘC] Đã thử {maxRetries} lần nhưng server Google vẫn từ chối từ '{keyword}'.");
            return new List<GeneratedExampleDto>();
        }

        // =========================================================================
        // HÀM 3: LƯU VÍ DỤ VÀO DATABASE
        // =========================================================================
        private static async Task SaveExamplesToDatabase(int senseId, List<GeneratedExampleDto> examples)
        {
            // Mở kết nối mới cho mỗi luồng để tránh xung đột Connection
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var ex in examples)
            {
                string insertSql = @"
                    INSERT INTO [examples] (SenseId, ContentJp, ContentTranslated, Transcription, SourceRef)
                    VALUES (@SenseId, @ContentJp, @ContentTranslated, @Transcription, 'AI_Gemini_Flash')";

                using var cmd = new SqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@SenseId", senseId);
                cmd.Parameters.AddWithValue("@ContentJp", ex.ContentJp);
                cmd.Parameters.AddWithValue("@ContentTranslated", ex.ContentTranslated);
                cmd.Parameters.AddWithValue("@Transcription", ex.Transcription);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    public class GeneratedExampleDto
    {
        public string ContentJp { get; set; } = string.Empty;
        public string ContentTranslated { get; set; } = string.Empty;
        public string Transcription { get; set; } = string.Empty;
    }
}