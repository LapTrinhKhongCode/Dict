using Microsoft.Data.SqlClient;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;

class Program
{
    // Cấu hình Database & Qdrant
    private const string ConnectionString = @"Data Source=tuf-dash-f15\sqlserver;Initial Catalog=Dict;Integrated Security=True;Trust Server Certificate=True";
    private const string QdrantHost = "localhost";
    private const int QdrantPort = 6333;
    private const string CollectionName = "dictionary_vectors";

    // Cấu hình AI Model (Tải file .onnx về máy trước)
    private const string ModelPath = "multilingual-e5-small.onnx";
    private static InferenceSession _session;
    private static QdrantClient _qdrantClient;

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("--- KHỞI CHẠY VECTOR INDEXER (GPU ACCELERATED) ---");

        try
        {
            // 1. Khởi tạo AI Session trên RTX 3050
            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider_CUDA(0); // Dùng GPU Device 0 (RTX 3050)
            _session = new InferenceSession(ModelPath, sessionOptions);
            _qdrantClient = new QdrantClient(QdrantHost, QdrantPort);

            // 2. Tạo Collection nếu chưa có
            await PrepareQdrantCollection();

            // 3. Xử lý dữ liệu theo Batch (Để không treo RAM GPU)
            int batchSize = 100;
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var rows = await FetchBatchFromSql(offset, batchSize);
                if (rows.Count == 0) { hasMore = false; break; }

                Console.WriteLine($"Đang xử lý batch từ {offset}...");

                // Vector hóa và đẩy lên Qdrant
                await ProcessAndPushToVectorDb(rows);

                offset += batchSize;
                Console.WriteLine($"=> Xong {offset} từ.");
            }

            Console.WriteLine("--- HOÀN THÀNH ĐẨY 240K VECTOR ---");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi chí mạng: {ex.Message}");
        }
    }

    private static async Task ProcessAndPushToVectorDb(List<(int id, string label, string meaning)> rows)
    {
        var points = new List<PointStruct>();

        foreach (var row in rows)
        {
            // Format nội dung để AI hiểu ngữ cảnh (label + nghĩa ngắn)
            string textToEmbed = $"query: {row.label}: {row.meaning}";

            // Chạy Embedding trên GPU
            float[] vector = GetEmbedding(textToEmbed);

            points.Add(new PointStruct
            {
                Id = (ulong)row.id,
                Vectors = vector,
                Payload =
                {
                    { "label", row.label },
                    { "meaning", row.meaning }
                }
            });
        }

        // Đẩy cả mảng 100 vector lên Qdrant trong 1 lần gọi
        await _qdrantClient.UpsertAsync(CollectionName, points);
    }

    private static float[] GetEmbedding(string text)
    {
        // Lưu ý: Đoạn này cần Tokenizer để biến chữ thành số trước khi đưa vào ONNX
        // Để đơn giản cho đồ án, bạn có thể dùng thư viện BertTokenizers
        var inputs = PreProcess(text);

        using var results = _session.Run(inputs);
        // Lấy output từ layer cuối cùng (thường là mảng float 384 hoặc 768 chiều)
        var output = results.First().AsEnumerable<float>().ToArray();

        return Normalize(output); // Chuẩn hóa vector về độ dài 1 (Cosine Similarity)
    }

    private static async Task PrepareQdrantCollection()
    {
        var collections = await _qdrantClient.ListCollectionsAsync();
        if (!collections.Contains(CollectionName))
        {
            await _qdrantClient.CreateCollectionAsync(CollectionName,
                new VectorParams { Size = 384, Distance = Distance.Cosine });
            Console.WriteLine($"Đã tạo collection: {CollectionName}");
        }
    }

    private static async Task<List<(int id, string label, string meaning)>> FetchBatchFromSql(int offset, int limit)
    {
        var list = new List<(int id, string label, string meaning)>();
        const string sql = @"
            SELECT Id, Label, ShortMean
            FROM Entries
            WHERE Type = N'word'
            ORDER BY Id
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";

        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Offset", offset);
        cmd.Parameters.AddWithValue("@Limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add((
                reader.GetInt32(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2)
            ));
        }
        return list;
    }

    // Các hàm phụ trợ (Tokenizer, Normalize...) Đức Anh cài thêm thư viện hỗ trợ nhé
    private static float[] Normalize(float[] v) { /* ... */ return v; }
    private static List<NamedOnnxValue> PreProcess(string text) { /* ... */ return null; }
}