using Microsoft.Data.SqlClient;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet; // THƯ VIỆN "CHÂN ÁI" MỚI
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text;

class Program
{
    // --- CẤU HÌNH HỆ THỐNG ---
    private const string ConnectionString = @"Data Source=tuf-dash-f15\sqlserver;Initial Catalog=Dict;Integrated Security=True;Trust Server Certificate=True";
    private const string QdrantHost = "localhost";
    private const int QdrantPort = 6334;
    private const string CollectionName = "dictionary_vectors";
    private const string ModelPath = "multilingual-e5-small.onnx";

    // FILE JSON VỪA TẢI VỀ
    private const string TokenizerPath = "tokenizer.json";

    private static InferenceSession _session = null!;
    private static QdrantClient _qdrantClient = null!;
    private static Tokenizer _tokenizer = null!; // SỬ DỤNG CLASS CỦA TOKENIZERS.DOTNET

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("🚀 --- KHỞI CHẠY VECTOR INDEXER (RTX 3050 GPU) ---");

        try
        {
            // 1. Khởi tạo Tokenizer siêu đơn giản với thư viện mới (Chỉ cần 1 dòng)
            Console.WriteLine("⏳ Đang nạp Tokenizer...");
            _tokenizer = new Tokenizer(vocabPath: TokenizerPath);

            // 2. Khởi tạo AI Session (CUDA)
            Console.WriteLine("⏳ Đang khởi tạo ONNX Runtime (CUDA)...");
            var sessionOptions = new SessionOptions();
            //sessionOptions.AppendExecutionProvider_CUDA(0);
            _session = new InferenceSession(ModelPath, sessionOptions);

            _qdrantClient = new QdrantClient(QdrantHost, QdrantPort);
            await PrepareQdrantCollection();

            int batchSize = 100;
            int offset = 0;
            bool hasMore = true;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            while (hasMore)
            {
                var rows = await FetchBatchFromSql(offset, batchSize);
                if (rows.Count == 0) { hasMore = false; break; }

                await ProcessAndPushToVectorDb(rows);

                offset += batchSize;
                double speed = offset / watch.Elapsed.TotalSeconds;
                Console.WriteLine($"✅ Đã xong: {offset} từ | Tốc độ: {speed:F1} từ/giây");
            }

            watch.Stop();
            Console.WriteLine($"--- 🎉 HOÀN THÀNH TRONG {watch.Elapsed.TotalMinutes:F1} PHÚT ---");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi: {ex.Message}");
        }


        //Console.OutputEncoding = Encoding.UTF8;
        //Console.WriteLine("⏳ Đang nạp Tokenizer...");
        //_tokenizer = new Tokenizer(vocabPath: TokenizerPath);

        //Console.WriteLine("⏳ Đang khởi tạo ONNX Runtime...");
        //var sessionOptions = new SessionOptions();
        //_session = new InferenceSession(ModelPath, sessionOptions);

        //_qdrantClient = new QdrantClient(QdrantHost, QdrantPort);

        //await TestSemanticSearch();
    }
    private static async Task TestSemanticSearch()
    {
        Console.WriteLine("\n=============================================");
        Console.WriteLine("🔍 TEST: TRA TỪ THEO NGỮ CẢNH (CÓ PRE-FILTERING)");
        Console.WriteLine("=============================================");

        string[][] tests = new string[][]
        {
            // 1. Test từ chuyên ngành ông vừa nạp (Kiểm tra xem lấy đúng data mới không)
            new string[] { "開発工程", "アジャイル手法を取り入れて、ソフトウェアの開発工程を短縮する。" },
            new string[] { "認証管理", "ゼロトラストセキュリティの観点から、システムの認証管理を強化した。" },
            new string[] { "教師あり学習", "大量のラベル付きデータを用いて、AIモデルに教師あり学習を行わせる。" },

            // 2. Test từ ĐA NGHĨA (Đây là bài test "sát thủ" nhất cho Semantic Search)
            // Nghĩa 1: Đồng bộ (IT)
            new string[] { "同期", "クラウド上のデータベースとローカルのファイルを自動的に同期する。" },
            // Nghĩa 2: Bạn cùng kỳ/Đồng nghiệp (Phổ thông)
            new string[] { "同期", "彼は大学時代の同期で、今は同じ部署で働いている。" },

            // 3. Test từ IT kinh điển (Kiểm tra độ nhạy của Context)
            new string[] { "デプロイ", "バグ修正が完了したので、新しいバージョンを本番サーバーにデプロイした。" },
            new string[] { "実装", "ユーザーからの要望を受けて、新しい検索アルゴリズムを実装する。" }
        };

        foreach (var test in tests)
        {
            string targetWord = test[0];
            string context = test[1];

            Console.WriteLine($"\n🎯 Đang tra từ: 【{targetWord}】 trong câu: \"{context}\"");
            var watch = System.Diagnostics.Stopwatch.StartNew();

            float[] queryVector = GetEmbedding($"query: {context}");

            // BỘ LỌC CỨNG (PRE-FILTER): Ép Qdrant chỉ được tìm những điểm có label = "する"
            // BỘ LỌC KÉP (PRE-FILTER): Tìm từ khóa trong 'label' HOẶC 'phonetic'
            var filter = new Qdrant.Client.Grpc.Filter
            {
                Should = // Should trong Qdrant tương đương với điều kiện OR
                {
                    new Qdrant.Client.Grpc.Condition
                    {
                        Field = new Qdrant.Client.Grpc.FieldCondition
                        {
                            Key = "label",
                            Match = new Qdrant.Client.Grpc.Match { Keyword = targetWord }
                        }
                    },
                    new Qdrant.Client.Grpc.Condition
                    {
                        Field = new Qdrant.Client.Grpc.FieldCondition
                        {
                            Key = "phonetic",
                            Match = new Qdrant.Client.Grpc.Match { Keyword = targetWord }
                        }
                    }
                }
            };

            // GỌI QDRANT KÈM THEO FILTER
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: CollectionName,
                vector: queryVector,
                filter: filter,   // <-- THÊM DÒNG NÀY VÀO LÀ CHUẨN BÀI
                limit: 1
            );

            watch.Stop();
            Console.WriteLine($"⏱️ Xử lý mất {watch.ElapsedMilliseconds} ms. KẾT QUẢ TOP 1:");

            if (searchResult.Count > 0)
            {
                var hit = searchResult[0];
                var p = hit.Payload;

                string meaning = p.ContainsKey("meaning") ? p["meaning"].StringValue : "";
                string exJp = p.ContainsKey("example_jp") ? p["example_jp"].StringValue : "";
                string exVi = p.ContainsKey("example_vi") ? p["example_vi"].StringValue : "";

                Console.WriteLine($"  👉 NGHĨA TÌM ĐƯỢC: {meaning}");
                Console.WriteLine($"  👉 Ví dụ gốc trong DB: {exJp}");
                Console.WriteLine($"  👉 Nghĩa tiếng Việt: {exVi}");
                Console.WriteLine($"  👉 Điểm tự tin (Score): {hit.Score:F4}");
            }
            else
            {
                Console.WriteLine("  ❌ Dữ liệu của ông không có chữ này!");
            }
            Console.WriteLine("  ---------------------------------");
        }
    }
    private static async Task ProcessAndPushToVectorDb(List<(int entryId, int exampleId, string label, string phonetic, string meaning, string exampleJp, string exampleVi, string domain)> rows)
    {
        var points = new List<PointStruct>();

        foreach (var row in rows)
        {
            string phoneticPart = string.IsNullOrEmpty(row.phonetic) ? "" : $" ({row.phonetic})";
            // Prompt cho Vector: Đưa thêm Context chuyên ngành vào để AI hiểu sâu hơn
            string textToEmbed = $"[{row.domain}] passage: {row.label}{phoneticPart}: {row.meaning}. Ví dụ: {row.exampleJp} - Nghĩa: {row.exampleVi}";

            float[] vector = GetEmbedding(textToEmbed);

            points.Add(new PointStruct
            {
                Id = (ulong)row.exampleId,
                Vectors = vector,
                Payload =
            {
                { "entry_id", row.entryId },
                { "label", row.label },
                { "phonetic", row.phonetic },
                { "meaning", row.meaning },
                { "example_jp", row.exampleJp },
                { "example_vi", row.exampleVi },
                { "domain", row.domain } // NÉM THÊM DOMAIN VÀO ĐÂY ĐỂ LỌC
            }
            });
        }
        await _qdrantClient.UpsertAsync(CollectionName, points);
    }

    private static float[] GetEmbedding(string text)
    {
        var inputs = PreProcess(text);
        using var results = _session.Run(inputs);

        var outputTensor = results.First().AsTensor<float>();

        int dim = 384;
        int seqLen = (int)outputTensor.Dimensions[1];
        float[] meanVector = new float[dim];

        for (int d = 0; d < dim; d++)
        {
            float sum = 0;
            for (int s = 0; s < seqLen; s++)
            {
                sum += outputTensor[0, s, d];
            }
            meanVector[d] = sum / seqLen;
        }

        return Normalize(meanVector);
    }

    // HÀM PREPROCESS ĐÃ ĐƯỢC LÀM MỚI HOÀN TOÀN
    private static List<NamedOnnxValue> PreProcess(string text)
    {
        // 1. Thư viện mới trả về trực tiếp mảng các ID (chuẩn như Python)
        var tokens = _tokenizer.Encode(text);
        long[] inputIds = tokens.Select(t => (long)t).ToArray();

        // 2. Tự tạo Attention Mask (Vì không độn Padding nên tất cả từ đều là 1)
        long[] attentionMask = Enumerable.Repeat(1L, inputIds.Length).ToArray();

        // 3. Token Type Ids (Model E5 không dùng cái này, ta tạo mảng số 0)
        long[] tokenTypeIds = new long[inputIds.Length];

        // 4. Cắt chuỗi an toàn nếu vượt quá giới hạn 512 token
        int maxLen = 512;
        if (inputIds.Length > maxLen)
        {
            inputIds = inputIds.Take(maxLen).ToArray();
            attentionMask = attentionMask.Take(maxLen).ToArray();
            tokenTypeIds = tokenTypeIds.Take(maxLen).ToArray();
        }

        int[] shape = new int[] { 1, inputIds.Length };

        return new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, shape)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, shape)),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypeIds, shape))
        };
    }

    private static float[] Normalize(float[] v)
    {
        double sumSq = 0;
        for (int i = 0; i < v.Length; i++) sumSq += v[i] * v[i];
        float norm = (float)Math.Sqrt(sumSq);
        if (norm > 1e-10)
        {
            for (int i = 0; i < v.Length; i++) v[i] /= norm;
        }
        return v;
    }

    private static async Task PrepareQdrantCollection()
    {
        var collections = await _qdrantClient.ListCollectionsAsync();
        if (!collections.Contains(CollectionName))
        {
            await _qdrantClient.CreateCollectionAsync(CollectionName,
                new VectorParams { Size = 384, Distance = Distance.Cosine });
        }
    }

    // Thêm tham số string domain vào List
    private static async Task<List<(int entryId, int exampleId, string label, string phonetic, string meaning, string exampleJp, string exampleVi, string domain)>> FetchBatchFromSql(int offset, int limit)
    {
        var list = new List<(int, int, string, string, string, string, string, string)>();

        string sql = $@"
    WITH CTE AS (
        SELECT 
            e.Id AS EntryId, 
            ex.Id AS ExampleId, 
            e.Label, 
            e.Phonetic, 
            e.Weight,
            s.[Domain], -- LẤY THÊM DOMAIN Ở ĐÂY
            (
                SELECT STRING_AGG(g.Text, '; ') 
                FROM [glosses] g 
                WHERE g.SenseId = s.Id
            ) AS SpecificMeaning, 
            ex.ContentJp,
            ex.ContentTranslated,
            ROW_NUMBER() OVER(PARTITION BY e.Id, ex.ContentJp ORDER BY ex.Id) as rn
        FROM [entries] e
        INNER JOIN [senses] s ON e.Id = s.EntryId
        INNER JOIN [examples] ex ON s.Id = ex.SenseId
        WHERE e.Type = 'word' 
          AND ex.ContentJp IS NOT NULL
    )
    SELECT 
        EntryId, ExampleId, Label, Phonetic, SpecificMeaning, ContentJp, ContentTranslated, ISNULL(Domain, 'General') AS Domain
    FROM CTE
    WHERE rn = 1
    ORDER BY Weight ASC, ExampleId ASC
    OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add((
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5),
                reader.IsDBNull(6) ? "" : reader.GetString(6),
                reader.GetString(7) // Đọc thêm cột Domain
            ));
        }
        return list;
    }




}