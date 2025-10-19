using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Retry config (network errors/timeouts)
    private const int MaxRetries = 4;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);
    private static readonly Random jitterer = new Random();

    // Connection string và URL API
    private const string ConnectionString = @"Data Source=tuf-dash-f15\sqlserver;Initial Catalog=Dict;Integrated Security=True;Trust Server Certificate=True";
    private const string ApiUrl = "https://api.mazii.net/api/get-mean";

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rows = await ReadIdsLabelsAndMobileIdAsync();
        Console.WriteLine($"Đã đọc {rows.Count} hàng cần xử lý.");

        int success = 0, failed = 0, skipped = 0;

        foreach (var item in rows)
        {
            int id = item.id;
            string label = item.label;
            int? mobileId = item.mobileId;

            if (mobileId == null)
            {
                Console.WriteLine($"Id={id}: MobileId NULL -> bỏ qua.");
                skipped++;
                continue;
            }

            // Gọi API và lấy response
            string responseBody = await CallApiGetMeanAsync(id, mobileId.Value);

            if (responseBody == null)
            {
                failed++;
                continue;
            }

            // Lưu vào CommentRawJson với Hán tự trực tiếp
            try
            {
                string unescapedJson;
                try
                {
                    var obj = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    unescapedJson = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                }
                catch
                {
                    // nếu parse fail, fallback lưu nguyên string
                    unescapedJson = responseBody;
                }

                await UpdateCommentRawJsonAsync(id, unescapedJson);
                Console.WriteLine($"Id={id}: Đã cập nhật CommentRawJson (len={unescapedJson.Length}).");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Id={id}: Lỗi khi cập nhật DB: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"Hoàn thành. Success={success}, Failed={failed}, Skipped={skipped}.");
    }

    private static async Task<string> CallApiGetMeanAsync(int id, int mobileId)
    {
        var payload = new
        {
            dict = "javi",
            type = "word",
            wordId = mobileId
        };

        string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                using var resp = await httpClient.PostAsync(ApiUrl, content);

                string body = await resp.Content.ReadAsStringAsync();
                return body;
            }
            catch (HttpRequestException hre)
            {
                Console.WriteLine($"HttpRequestException Id={id}, attempt {attempt}: {hre.Message}");
            }
            catch (TaskCanceledException tce)
            {
                Console.WriteLine($"Timeout Id={id}, attempt {attempt}: {tce.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error Id={id}, attempt {attempt}: {ex.Message}");
            }

            if (attempt < MaxRetries)
            {
                double backoffSeconds = BaseDelay.TotalSeconds * Math.Pow(2, attempt - 1);
                double jitter = jitterer.NextDouble() * 0.5;
                int ms = (int)((backoffSeconds + jitter) * 1000);
                Console.WriteLine($"Id={id}: Chờ {ms}ms trước retry...");
                await Task.Delay(ms);
            }
        }

        Console.WriteLine($"Id={id}: Gọi API thất bại sau {MaxRetries} attempt -> bỏ qua.");
        return null;
    }

    private static async Task<List<(int id, string label, int? mobileId)>> ReadIdsLabelsAndMobileIdAsync()
    {
        var list = new List<(int id, string label, int? mobileId)>();

        const string sql = @"
            SELECT Id, Label, MobileId
            FROM Entries
            WHERE Type = N'word'
            ORDER BY Id
        ";

        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(reader.GetOrdinal("Id"));
            string label = reader.IsDBNull(reader.GetOrdinal("Label")) ? null : reader.GetString(reader.GetOrdinal("Label"));
            int? mobileId = null;
            int moOrdinal = reader.GetOrdinal("MobileId");
            if (!reader.IsDBNull(moOrdinal)) mobileId = reader.GetInt32(moOrdinal);

            list.Add((id, label, mobileId));
        }

        return list;
    }

    private static async Task UpdateCommentRawJsonAsync(int id, string commentRawJson)
    {
        const string updateSql = @"
            UPDATE Entries
            SET CommentRawJson = @CommentRawJson,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;
        ";

        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(updateSql, conn);
        cmd.Parameters.Add("@CommentRawJson", System.Data.SqlDbType.NVarChar, -1).Value = (object)commentRawJson ?? DBNull.Value;
        cmd.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        await cmd.ExecuteNonQueryAsync();
    }
}
