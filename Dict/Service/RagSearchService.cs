using Dict.DTO;
using Dict.Service.IService;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Qdrant.Client;
using System.Text;
using System.Text.Json;
using Tokenizers.DotNet;

namespace Dict.Service
{
    public class RagSearchService : IRagSearchService, IDisposable
    {
        private readonly Tokenizer _tokenizer;
        private readonly InferenceSession _session;
        private readonly QdrantClient _qdrantClient;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private const string CollectionName = "dictionary_vectors";

        public RagSearchService(IConfiguration config)
        {
            _config = config;

            // 1. Khởi tạo Tokenizer
            string tokenizerPath = Path.Combine(Directory.GetCurrentDirectory(), "tokenizer.json");
            _tokenizer = new Tokenizer(vocabPath: tokenizerPath);

            // 2. Khởi tạo ONNX Runtime
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "multilingual-e5-small.onnx");
            var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions();
            // 🔥 ĐÒN ÉP XUNG CPU CỰC MẠNH CHO ARM64
            // Khóa mõm tính năng tìm GPU, ép chạy 100% bằng CPU
            sessionOptions.AppendExecutionProvider_CPU();

            // Ép ONNX vắt kiệt 4 nhân vật lý của Raspberry Pi 5 để xử lý song song
            sessionOptions.IntraOpNumThreads = 4;

            // Bật tối ưu hóa đồ thị mạng nơ-ron ở mức cao nhất
            sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(modelPath, sessionOptions);

            // 3. Khởi tạo Client
            string qdrantUrl = _config["QdrantCloud:Url"];
            string qdrantApiKey = _config["QdrantCloud:ApiKey"];

            _qdrantClient = new QdrantClient(
                host: qdrantUrl,
                https: true,        // Bắt buộc true khi lên mây
                apiKey: qdrantApiKey
            );
            _httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
        }

        // =========================================================================
        // API 1: TÌM KIẾM VECTOR SIÊU TỐC (RAG CHUẨN) - TRẢ VỀ TRONG VÀI CHỤC MILI-GIÂY
        // =========================================================================
        public async Task<RagSearchResponseDto> SearchVectorAsync(string keyword, string context)
        {
            // 1. Băm câu ví dụ ra Vector
            string textToEmbed = $"query: {context}";
            float[] queryVector = GetEmbedding(textToEmbed);

            // 2. Xây dựng bộ lọc kép (Pre-filter)
            var filter = new Qdrant.Client.Grpc.Filter
            {
                Should =
                {
                    new Qdrant.Client.Grpc.Condition
                    {
                        Field = new Qdrant.Client.Grpc.FieldCondition
                        {
                            Key = "label",
                            Match = new Qdrant.Client.Grpc.Match { Keyword = keyword }
                        }
                    },
                    new Qdrant.Client.Grpc.Condition
                    {
                        Field = new Qdrant.Client.Grpc.FieldCondition
                        {
                            Key = "phonetic",
                            Match = new Qdrant.Client.Grpc.Match { Keyword = keyword }
                        }
                    }
                }
            };

            // 3. Gọi Qdrant lấy Top 5
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: CollectionName,
                vector: queryVector,
                filter: filter,
                limit: 5
            );

            var responseDto = new RagSearchResponseDto
            {
                Word = keyword,
                ContextUsed = new List<RagContextItem>()
            };

            if (searchResult.Count == 0)
            {
                responseDto.Explanation = "Hệ thống không tìm thấy từ vựng này trong cơ sở dữ liệu Qdrant.";
                return responseDto;
            }

            // Chỉ việc map dữ liệu từ Qdrant sang DTO trả về cho UI
            foreach (var hit in searchResult)
            {
                var p = hit.Payload;
                responseDto.ContextUsed.Add(new RagContextItem
                {
                    Label = p.ContainsKey("label") ? p["label"].StringValue : "",
                    Meaning = p.ContainsKey("meaning") ? p["meaning"].StringValue : "",
                    ExampleJp = p.ContainsKey("example_jp") ? p["example_jp"].StringValue : "",
                    ExampleVi = p.ContainsKey("example_vi") ? p["example_vi"].StringValue : "",
                    Score = hit.Score
                });
            }

            return responseDto; // Trả về ngay lập tức, không đợi Gemini!
        }

        // =========================================================================
        // API 2: GỌI GEMINI GIẢI THÍCH CHUYÊN SÂU (CHỈ CHẠY KHI USER BẤM NÚT)
        // =========================================================================
        public async Task<(string word, string best_meaning, string explanation)> ExplainWithGeminiAsync(string keyword, string context, List<RagContextItem> ragContexts)
        {
            // 1. XÓA BỎ CHỐT CHẶN RỖNG (Early Return)
            // Giờ đây dù ragContexts rỗng, ta vẫn tiếp tục xây dựng prompt cho Gemini.

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Bạn là một chuyên gia từ điển Nhật-Việt cao cấp. Người dùng đang tra từ \"{keyword}\" trong ngữ cảnh câu: \"{context}\"");

            // 2. ĐIỀU CHỈNH PHẦN NẠP DỮ LIỆU THAM KHẢO
            if (ragContexts != null && ragContexts.Count > 0)
            {
                promptBuilder.AppendLine("\nDưới đây là các định nghĩa được hệ thống tự động trích xuất từ cơ sở dữ liệu (có thể ĐÚNG hoặc SAI):");
                for (int i = 0; i < ragContexts.Count; i++)
                {
                    var item = ragContexts[i];
                    promptBuilder.AppendLine($"- Tùy chọn {i + 1}: Nghĩa: {item.Meaning} | Ví dụ: {item.ExampleJp}");
                }
            }
            else
            {
                // Thông báo rõ cho AI là không có dữ liệu tham khảo để nó tự "vận công"
                promptBuilder.AppendLine("\nLưu ý: Hệ thống không tìm thấy định nghĩa nào trong cơ sở dữ liệu cho từ này.");
            }

            promptBuilder.AppendLine("\nNHIỆM VỤ CỦA BẠN:");
            promptBuilder.AppendLine("1. Phân tích ngữ cảnh câu của người dùng để xác định nghĩa đúng nhất của từ.");

            // Điều chỉnh logic nhiệm vụ để bao quát cả trường hợp không có RAG
            if (ragContexts != null && ragContexts.Count > 0)
            {
                promptBuilder.AppendLine("2. NẾU có 'Tùy chọn' nào ở trên khớp ý nghĩa: Hãy chọn nghĩa đó và giải thích sắc thái sâu hơn.");
                promptBuilder.AppendLine("3. NẾU tất cả 'Tùy chọn' đều sai hoặc không phù hợp: HÃY BỎ QUA CHÚNG. Tự dùng kiến thức chuyên gia của bạn để đưa ra nghĩa đúng nhất và giải thích.");
            }
            else
            {
                promptBuilder.AppendLine("2. Vì không có dữ liệu tham khảo, hãy sử dụng kiến thức chuyên gia của bạn để giải nghĩa từ này dựa hoàn toàn vào ngữ cảnh câu đã cho.");
            }

            // LUẬT TRÌNH BÀY (Giữ nguyên các "Luật thép" của ông)
            promptBuilder.AppendLine("\nLUẬT TRÌNH BÀY (TUYỆT ĐỐI TUÂN THỦ):");
            promptBuilder.AppendLine("- Phải giải thích một cách tự nhiên, trôi chảy như một người thầy giáo đang giảng bài.");
            promptBuilder.AppendLine("- KHÔNG ĐƯỢC phép nhắc đến các từ ngữ như: 'Tùy chọn 1, 2, 3', 'danh sách trên', 'cơ sở dữ liệu', 'RAG' hay 'hệ thống'.");
            promptBuilder.AppendLine("- Tuyệt đối giấu kín việc bạn đang tham khảo dữ liệu từ các Tùy chọn (nếu có).");

            // Định dạng JSON (Giữ nguyên)
            promptBuilder.AppendLine("\nTrình bày kết quả dưới định dạng JSON nguyên bản. Cấu trúc BẮT BUỘC phải y hệt như sau:");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("  \"word\": \"từ đang tra\",");
            promptBuilder.AppendLine("  \"best_meaning\": \"nghĩa ngắn gọn chính xác nhất\",");
            promptBuilder.AppendLine("  \"explanation\": \"giải thích chi tiết tại sao dịch như vậy\"");
            promptBuilder.AppendLine("}");

            // 3. GỌI GEMINI (Lúc này prompt luôn được gửi đi)
            return await CallGeminiAsync(promptBuilder.ToString());
        }

        private async Task<(string word, string best_meaning, string explanation)> CallGeminiAsync(string prompt)
        {
            try
            {
                string apiKey = _config["GoogleCloud:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return ("", "Lỗi cấu hình Key", "Không tìm thấy 'GoogleCloud:ApiKey'.");
                }

                apiKey = apiKey.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();

                string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
                string requestUrl = $"{endpoint}?key={apiKey}";

                var requestUri = new Uri(requestUrl);

                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new
                    {
                        response_mime_type = "application/json",
                        response_schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                word = new { type = "string" },
                                best_meaning = new { type = "string" },
                                explanation = new { type = "string" }
                            },
                            required = new[] { "word", "best_meaning", "explanation" }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    var serialized = JsonSerializer.Serialize(requestBody);
                    Console.WriteLine($"\n[GEMINI API ERROR] Status: {response.StatusCode}");
                    Console.WriteLine($"[GEMINI REQUEST BODY]: {serialized}");
                    Console.WriteLine($"[GEMINI ERROR BODY]: {errorContent}\n");
                    return ("", "Lỗi API", $"Mã lỗi {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);

                var resultText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString() ?? "";

                resultText = resultText.Replace("```json", "").Replace("```", "").Trim();

                // IN RA CONSOLE ĐỂ KIỂM TRA MẮT THẤY TAI NGHE
                Console.WriteLine("\n=== AI TRẢ VỀ JSON THÔ ===");
                Console.WriteLine(resultText);
                Console.WriteLine("==========================\n");

                // GIẢI MÃ MẠNH MẼ: KHÔNG PHÂN BIỆT HOA THƯỜNG
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var finalObj = JsonSerializer.Deserialize<GeminiRawResponse>(resultText, options);

                if (finalObj == null)
                {
                    return ("", "Lỗi Format", "Không thể đọc dữ liệu từ AI.");
                }

                // Gộp chung Best_Meaning và BestMeaning (trường hợp AI viết sai tên key)
                string finalMeaning = !string.IsNullOrWhiteSpace(finalObj.Best_Meaning) ? finalObj.Best_Meaning : finalObj.BestMeaning;

                return (finalObj.Word ?? "", finalMeaning ?? "", finalObj.Explanation ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[C# CATCH ERROR] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n");
                return ("", "Lỗi Code", ex.Message);
            }
        }
        // =========================================================================
        // CÁC HÀM PRIVATE DƯỚI NÀY GIỮ NGUYÊN KHÔNG ĐỔI
        // =========================================================================
        private float[] GetEmbedding(string text)
        {
            var tokens = _tokenizer.Encode(text);
            long[] inputIds = tokens.Select(t => (long)t).ToArray();
            long[] attentionMask = Enumerable.Repeat(1L, inputIds.Length).ToArray();
            long[] tokenTypeIds = new long[inputIds.Length];

            int maxLen = 512;
            if (inputIds.Length > maxLen)
            {
                inputIds = inputIds.Take(maxLen).ToArray();
                attentionMask = attentionMask.Take(maxLen).ToArray();
                tokenTypeIds = tokenTypeIds.Take(maxLen).ToArray();
            }

            int[] shape = new int[] { 1, inputIds.Length };

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, shape)),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, shape)),
                NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypeIds, shape))
            };

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

        private float[] Normalize(float[] v)
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

        public void Dispose()
        {
            _session?.Dispose();
            _httpClient?.Dispose();
        }
    }
    public class GeminiRawResponse
    {
        public string Word { get; set; } = string.Empty;
        public string Best_Meaning { get; set; } = string.Empty;
        public string BestMeaning { get; set; } = string.Empty; // Bắt cả trường hợp AI viết liền
        public string Explanation { get; set; } = string.Empty;
    }

}
