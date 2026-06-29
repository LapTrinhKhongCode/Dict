using System.Text;
using System.Text.Json;

namespace Dict.Service
{
    public static class RagLlmRole
    {
        public const string Answer = "answer";
        public const string QueryExpansion = "query_expansion";
        public const string Hyde = "hyde";
        public const string Decomposition = "decomposition";
        public const string Rewrite = "rewrite";
        public const string Overview = "overview";
        public const string Rerank = "rerank";
    }

    public interface IRagLlmProvider
    {
        string Name { get; }
        Task<string?> GenerateJsonAsync(string prompt, string responseSchemaJson, string role, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> StreamTextAsync(string prompt, string role, bool disableThinking = false, CancellationToken cancellationToken = default);
    }

    public interface IRagLlmRouter
    {
        IRagLlmProvider GetProvider(string role);
        string GetProviderName(string role);
    }

    public sealed class RagLlmRouter : IRagLlmRouter
    {
        private readonly IConfiguration _config;
        private readonly GeminiRagLlmProvider _geminiProvider;
        private readonly LocalRagLlmProvider _localProvider;

        public RagLlmRouter(
            IConfiguration config,
            GeminiRagLlmProvider geminiProvider,
            LocalRagLlmProvider localProvider)
        {
            _config = config;
            _geminiProvider = geminiProvider;
            _localProvider = localProvider;
        }

        public IRagLlmProvider GetProvider(string role)
        {
            string providerName = GetProviderName(role);
            return providerName.Equals("local", StringComparison.OrdinalIgnoreCase)
                ? _localProvider
                : _geminiProvider;
        }

        public string GetProviderName(string role)
        {
            string? configured = role switch
            {
                RagLlmRole.Answer => _config["RagLlmRouting:AnswerProvider"],
                RagLlmRole.QueryExpansion => _config["RagLlmRouting:QueryExpansionProvider"],
                RagLlmRole.Hyde => _config["RagLlmRouting:HydeProvider"],
                RagLlmRole.Decomposition => _config["RagLlmRouting:DecompositionProvider"],
                RagLlmRole.Rewrite => _config["RagLlmRouting:RewriteProvider"],
                RagLlmRole.Overview => _config["RagLlmRouting:OverviewProvider"],
                RagLlmRole.Rerank => _config["RagLlmRouting:RerankProvider"],
                _ => null
            };

            return string.IsNullOrWhiteSpace(configured) ? "local" : configured.Trim().ToLowerInvariant();
        }
    }

    public sealed class GeminiRagLlmProvider : IRagLlmProvider, IDisposable
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        public string Name => "gemini";

        public GeminiRagLlmProvider(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
        }

        public async Task<string?> GenerateJsonAsync(string prompt, string responseSchemaJson, string role, CancellationToken cancellationToken = default)
        {
            string apiKey = _config["GoogleCloud:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            apiKey = apiKey.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
            string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
            string requestUrl = $"{endpoint}?key={apiKey}";

            using var schemaDoc = JsonDocument.Parse(responseSchemaJson);
            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    response_schema = schemaDoc.RootElement
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var resultText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;
                return resultText.Replace("```json", "").Replace("```", "").Trim();
            }
            catch
            {
                return null;
            }
        }

        public async IAsyncEnumerable<string> StreamTextAsync(string prompt, string role, bool disableThinking = false, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string apiKey = _config["GoogleCloud:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                yield return "Lỗi cấu hình API key.";
                yield break;
            }

            apiKey = apiKey.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:streamGenerateContent?alt=sse&key={apiKey}";

            object requestBody = disableThinking
                ? new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { thinkingConfig = new { thinkingBudget = 0 } }
                }
                : new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } }
                };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            HttpResponseMessage? response = null;
            string? connectionError = null;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception ex)
            {
                connectionError = $"Lỗi kết nối Gemini: {ex.Message}";
            }

            if (connectionError != null)
            {
                yield return connectionError;
                yield break;
            }

            if (!response!.IsSuccessStatusCode)
            {
                string errorBody = string.Empty;
                try { errorBody = await response.Content.ReadAsStringAsync(cancellationToken); } catch { }
                yield return $"Lỗi API Gemini: {response.StatusCode} - {errorBody}";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                {
                    continue;
                }

                string json = line["data:".Length..].Trim();
                if (json == "[DONE]")
                {
                    break;
                }

                string? chunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    chunk = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                }
                catch
                {
                }

                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    public sealed class LocalRagLlmProvider : IRagLlmProvider, IDisposable
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        public string Name => "local";

        public LocalRagLlmProvider(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient(new HttpClientHandler { UseProxy = false })
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public async Task<string?> GenerateJsonAsync(string prompt, string responseSchemaJson, string role, CancellationToken cancellationToken = default)
        {
            string baseUrl = (_config["LocalLlm:BaseUrl"] ?? "http://127.0.0.1:11434").TrimEnd('/');
            string model = _config["LocalLlm:Model"] ?? "qwen2.5:3b-instruct";
            int numCtx = int.TryParse(_config["LocalLlm:NumCtx"], out var n) ? n : 4096;

            using var schemaDoc = JsonDocument.Parse(responseSchemaJson);
            var requestBody = new
            {
                model,
                prompt,
                stream = false,
                format = schemaDoc.RootElement,
                options = new
                {
                    temperature = 0.1,
                    num_ctx = numCtx
                }
            };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(
                    $"{baseUrl}/api/generate",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                    cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                string text = doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
                return text.Replace("```json", "").Replace("```", "").Trim();
            }
            catch
            {
                return null;
            }
        }

        public async IAsyncEnumerable<string> StreamTextAsync(string prompt, string role, bool disableThinking = false, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string baseUrl = (_config["LocalLlm:BaseUrl"] ?? "http://127.0.0.1:11434").TrimEnd('/');
            string model = _config["LocalLlm:Model"] ?? "qwen2.5:3b-instruct";
            int numCtx = int.TryParse(_config["LocalLlm:NumCtx"], out var nc) ? nc : 4096;

            var requestBody = new
            {
                model,
                prompt,
                stream = true,
                options = new
                {
                    temperature = disableThinking ? 0.0 : 0.2,
                    num_ctx = numCtx
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            HttpResponseMessage? response = null;
            string? connectionError = null;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception ex)
            {
                connectionError = $"Lỗi kết nối Local LLM: {ex.Message}";
            }

            if (connectionError != null)
            {
                yield return connectionError;
                yield break;
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                yield return $"Lỗi API Local LLM: {response.StatusCode}. {body}".Trim();
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string? chunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    chunk = doc.RootElement.GetProperty("response").GetString();
                }
                catch
                {
                }

                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
