using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Dict.Data;   // adjust namespace to your project
using Dict.Models; // adjust if necessary

namespace Dict.Service
{
    /// <summary>
    /// Import words from a TSV produced by jmdict_to_tsv.py (use canonical column).
    /// For each input token generate variants, call mazii /api/search/word, dedupe responses,
    /// and save as Entry (type="word") with RawJson and optionally Senses/Glosses if present.
    /// 
    /// Optimized behavior:
    /// - load processed labels + entSeqs from DB and optional progress file before starting
    /// - skip rows that are already processed (cheap HashSet checks) BEFORE calling the remote API
    /// - avoid expensive LIKE scans on the table
    /// - persist progress to a text file (append or buffered) only after DB SaveChanges succeeded
    /// </summary>
    public class WordImportService
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WordImportService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Random _rng = new();

        // HTTP / retry configuration
        private const int MaxHttpRetries = 5;
        private const int InitialBackoffMs = 300;
        private const int MaxBackoffMs = 10_000;
        private const int RequestDelayMs = 0; // small delay between requests to be polite

        private static readonly Regex CjkRegex = new(@"[\u4E00-\u9FFF\u3400-\u4DBF\uF900-\uFAFF]", RegexOptions.Compiled);

        // in-memory processed sets (fast checks)
        private readonly HashSet<string> _processedLabels = new(StringComparer.Ordinal);
        private readonly HashSet<long> _processedEntSeqs = new();

        // progress file to allow resume across runs
        private readonly string _progressFilePath = @"G:\N2\CODE\processed_labels.txt";
        private readonly object _progressFileLock = new();

        // buffered write to reduce disk I/O
        private readonly List<string> _progressBuffer = new();
        private readonly int _progressFlushSize = 100;

        public WordImportService(HttpClient http, ApplicationDbContext db, ILogger<WordImportService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        }

        /// <summary>
        /// Load processed labels and entSeqs from DB and merge optional progress file.
        /// This populates in-memory HashSets used to skip already-processed items BEFORE calling API.
        /// </summary>
        private async Task LoadProcessedStateAsync(CancellationToken cancellationToken)
        {
            // 1) load labels from DB
            try
            {
                var labelsFromDb = await _db.Entries
                                           .Where(e => e.Type == "word" && !string.IsNullOrEmpty(e.Label))
                                           .Select(e => e.Label)
                                           .ToListAsync(cancellationToken);
                foreach (var lbl in labelsFromDb)
                {
                    if (!string.IsNullOrEmpty(lbl))
                        _processedLabels.Add(lbl);
                }
                _logger.LogInformation("Loaded {count} labels from DB into processed set.", labelsFromDb.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load labels from DB into processed set; continuing (set may be empty).");
            }

            // 2) load entSeqs from DB (fast indexable check)
            try
            {
                var entseqs = await _db.Entries
                                       .Where(e => e.Type == "word" && e.EntSeq != null)
                                       .Select(e => e.EntSeq.Value)
                                       .ToListAsync(cancellationToken);
                foreach (var v in entseqs)
                    _processedEntSeqs.Add(v);

                _logger.LogInformation("Loaded {count} entSeq values from DB into processed set.", entseqs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load entSeq set from DB; continuing.");
            }

            // 3) merge progress file if exists (from previous runs)
            try
            {
                if (File.Exists(_progressFilePath))
                {
                    foreach (var line in File.ReadLines(_progressFilePath, Encoding.UTF8))
                    {
                        var lbl = line?.Trim();
                        if (!string.IsNullOrEmpty(lbl))
                            _processedLabels.Add(lbl);
                    }
                    _logger.LogInformation("Merged progress file into processed set (file: {file})", _progressFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read progress file {file}.", _progressFilePath);
            }

            _logger.LogInformation("Total processed labels in memory: {n}, entSeqs: {m}", _processedLabels.Count, _processedEntSeqs.Count);
        }

        /// <summary>
        /// Add label to in-memory set and append to progress file (buffered).
        /// Should be called only AFTER DB save succeeded for that label.
        /// </summary>
        private void SaveProcessedLabelBuffered(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return;

            // Add to in-memory set; HashSet.Add returns false if already present.
            if (!_processedLabels.Add(label)) return;

            lock (_progressFileLock)
            {
                _progressBuffer.Add(label);
                if (_progressBuffer.Count >= _progressFlushSize)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(_progressFilePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.AppendAllText(_progressFilePath, string.Join(Environment.NewLine, _progressBuffer) + Environment.NewLine, Encoding.UTF8);
                        _progressBuffer.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to append processed labels to progress file (buffer flush).");
                        // Keep buffer so we can try flush later.
                    }
                }
            }
        }

        /// <summary>
        /// Flush any remaining buffered processed labels to disk (call at the end of the import).
        /// </summary>
        private void FlushProgressBuffer()
        {
            lock (_progressFileLock)
            {
                if (_progressBuffer.Count == 0) return;
                try
                {
                    var dir = Path.GetDirectoryName(_progressFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.AppendAllText(_progressFilePath, string.Join(Environment.NewLine, _progressBuffer) + Environment.NewLine, Encoding.UTF8);
                    _progressBuffer.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to flush progress buffer to file on shutdown.");
                }
            }
        }

        /// <summary>
        /// Top-level import: read TSV and process each row. TSV expected with header containing canonical in 4th column.
        /// </summary>
        public async Task ImportWordsFromTsvAsync(string tsvPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tsvPath)) throw new ArgumentNullException(nameof(tsvPath));
            if (!File.Exists(tsvPath)) throw new FileNotFoundException("TSV file not found", tsvPath);

            using var sr = new StreamReader(tsvPath, Encoding.UTF8);
            string header = await sr.ReadLineAsync();
            _logger.LogInformation("Reading TSV {file} header={hdr}", tsvPath, header);

            int lineNo = 1;
            var strategy = _db.Database.CreateExecutionStrategy();

            // load processed state (DB + optional progress file)
            await LoadProcessedStateAsync(cancellationToken);

            try
            {
                while (!sr.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await sr.ReadLineAsync();
                    lineNo++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cols = line.Split('\t');
                    var entSeqStr = cols.Length > 0 ? cols[0].Trim() : string.Empty;
                    var headword = cols.Length > 1 ? cols[1].Trim() : string.Empty;
                    var reading = cols.Length > 2 ? cols[2].Trim() : string.Empty;
                    var canonical = cols.Length > 3 ? cols[3].Trim() : string.Empty;

                    if (string.IsNullOrEmpty(canonical))
                    {
                        canonical = !string.IsNullOrEmpty(headword) ? headword : reading;
                    }
                    if (string.IsNullOrWhiteSpace(canonical))
                    {
                        _logger.LogDebug("Skipping empty canonical at line {ln}", lineNo);
                        continue;
                    }

                    // Try parse entSeq from TSV (cheap check)
                    long? entSeqCandidate = null;
                    if (long.TryParse(entSeqStr, out var parsedEntSeq))
                        entSeqCandidate = parsedEntSeq;

                    // If entSeq already processed (fast HashSet), skip BEFORE calling API
                    if (entSeqCandidate.HasValue && _processedEntSeqs.Contains(entSeqCandidate.Value))
                    {
                        _logger.LogDebug("Skipping '{c}' because entSeq {es} already exists", canonical, entSeqCandidate.Value);
                        continue;
                    }

                    // If label already processed (fast HashSet), skip BEFORE calling API
                    if (_processedLabels.Contains(canonical))
                    {
                        _logger.LogDebug("Skipping '{c}' because already processed (label)", canonical);
                        continue;
                    }

                    try
                    {
                        await strategy.ExecuteAsync(async () =>
                        {
                            await ProcessSingleWordAsync(entSeqStr, canonical, headword, reading, cancellationToken);
                        });

                        // DO NOT save processed label here — we persist progress only after a successful DB save
                        // SaveProcessedLabelBuffered(canonical); <-- moved into SaveWordResultToDatabaseInternalAsync after SaveChanges
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled error processing TSV line {ln} canonical={c}", lineNo, canonical);
                    }

                    // tiny polite delay
                    await Task.Delay(0, cancellationToken);
                }
            }
            finally
            {
                // flush buffered progress to file on exit
                FlushProgressBuffer();
            }

            _logger.LogInformation("ImportWordsFromTsvAsync finished reading {file}", tsvPath);
        }

        private async Task ProcessSingleWordAsync(string entSeqStr, string canonical, string headword, string reading, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var variants = GenerateQueryVariants(canonical).Distinct(StringComparer.Ordinal).ToList();
            _logger.LogDebug("Processing word canonical='{c}' variants={vcount}", canonical, variants.Count);

            var results = new Dictionary<string, JsonElement>();
            string lastRawResponse = null;
            int totalFound = 0;

            foreach (var q in variants)
            {
                cancellationToken.ThrowIfCancellationRequested();

                JsonDocument doc = null;
                try
                {
                    doc = await PostSearchWordAsync(q, cancellationToken);
                    if (doc == null) continue;

                    lastRawResponse = doc.RootElement.GetRawText();

                    var items = new List<JsonElement>();
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                        {
                            if (data.TryGetProperty("words", out var words) && words.ValueKind == JsonValueKind.Array)
                                items.AddRange(words.EnumerateArray());
                            if (data.TryGetProperty("suggestWords", out var sgw) && sgw.ValueKind == JsonValueKind.Array)
                                items.AddRange(sgw.EnumerateArray());
                        }

                        if (root.TryGetProperty("results", out var res) && res.ValueKind == JsonValueKind.Array)
                            items.AddRange(res.EnumerateArray());
                        if (root.TryGetProperty("words", out var w2) && w2.ValueKind == JsonValueKind.Array)
                            items.AddRange(w2.EnumerateArray());
                        if (root.TryGetProperty("suggestWords", out var sw2) && sw2.ValueKind == JsonValueKind.Array)
                            items.AddRange(sw2.EnumerateArray());

                        if (items.Count == 0)
                        {
                            if (root.TryGetProperty("word", out _) || root.TryGetProperty("kanji", out _) || root.TryGetProperty("mobileId", out _) || root.TryGetProperty("_id", out _))
                                items.Add(root);
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        items.AddRange(doc.RootElement.EnumerateArray());
                    }

                    // dedupe
                    foreach (var el in items)
                    {
                        var key = DedupKeyFromResult(el);
                        if (key == null) continue;
                        if (!results.ContainsKey(key)) results[key] = el.Clone();
                    }

                    totalFound += items.Count;
                    _logger.LogDebug("Query '{q}' returned {n} items (deduped so far: {d})", q, items.Count, results.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Search call failed/parse failed for query '{q}'", q);
                }
                finally
                {
                    doc?.Dispose();
                }

                if (results.Count > 0 && results.Values.Any(r => LooksLikeCompleteWordResult(r)))
                {
                    _logger.LogDebug("Found complete result for canonical '{c}' - stopping further variant queries", canonical);
                    break;
                }

                await Task.Delay(RequestDelayMs + (int)(_rng.NextDouble() * 50), cancellationToken);
            }

            _logger.LogInformation("Canonical='{c}' totalFoundAcrossQueries={tf} dedupedResults={dr}", canonical, totalFound, results.Count);

            if (results.Count == 0)
            {
                _logger.LogInformation("No structured results found for '{c}' — saving raw response as backup entry (if any)", canonical);
                if (!string.IsNullOrEmpty(lastRawResponse))
                {
                    try
                    {
                        await SaveRawBackupEntryAsync(canonical, lastRawResponse, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save raw backup entry for '{c}'", canonical);
                    }
                }
                else
                {
                    _logger.LogDebug("No raw response captured for '{c}', nothing to save.", canonical);
                }
                return;
            }

            // Save results to DB
            // Note: SaveWordResultToDatabaseInternalAsync will persist progress (SaveProcessedLabelBuffered) after a successful SaveChanges.
            foreach (var kv in results)
            {
                var el = kv.Value;
                try
                {
                    await SaveWordResultToDatabaseInternalAsync(el, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save word result key={k}", kv.Key);
                }
            }
        }

        /// <summary>
        /// Post to /api/search/word with payload and return parsed JsonDocument (caller must dispose).
        /// Implements retry/backoff for transient errors and 429.
        /// </summary>
        private async Task<JsonDocument> PostSearchWordAsync(string query, CancellationToken cancellationToken)
        {
            var payload = new
            {
                dict = "javi",
                type = "word",
                query = query,
                limit = 20,
                page = 1
            };
            var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);

            for (int attempt = 0; attempt < MaxHttpRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                    using var resp = await _http.PostAsync("https://mazii.net/api/search/word", content, cancellationToken);

                    if (resp.IsSuccessStatusCode)
                    {
                        var s = await resp.Content.ReadAsStringAsync(cancellationToken);
                        try
                        {
                            return JsonDocument.Parse(s);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse JSON response for query '{q}' (len={l})", query, s?.Length ?? 0);
                            return null;
                        }
                    }

                    if ((int)resp.StatusCode == 429)
                    {
                        _logger.LogWarning("HTTP 429 received for query '{q}' attempt {a}", query, attempt + 1);
                    }
                    else if ((int)resp.StatusCode >= 500)
                    {
                        _logger.LogWarning("Server error HTTP {code} for query '{q}' attempt {a}", (int)resp.StatusCode, query, attempt + 1);
                    }
                    else
                    {
                        _logger.LogDebug("Non-success HTTP {code} for query '{q}' attempt {a}", (int)resp.StatusCode, query, attempt + 1);
                        return null; // client error -> don't retry
                    }
                }
                catch (HttpRequestException hre)
                {
                    _logger.LogWarning(hre, "HttpRequestException for query '{q}' attempt {a}", query, attempt + 1);
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Request timeout for query '{q}' attempt {a}", query, attempt + 1);
                }

                var backoff = Math.Min(InitialBackoffMs * (1 << attempt), MaxBackoffMs);
                backoff += (int)(_rng.NextDouble() * 200);
                await Task.Delay(backoff, cancellationToken);
            }

            _logger.LogWarning("Giving up HTTP for query '{q}' after {max} attempts", query, MaxHttpRetries);
            return null;
        }

        /// <summary>
        /// Build a dedupe key from API result element: prefer entSeq/mobileId/_id if present else word|reading combination.
        /// </summary>
        private static string DedupKeyFromResult(JsonElement el)
        {
            try
            {
                if (el.ValueKind != JsonValueKind.Object) return null;

                // common mobileId / entSeq
                if (el.TryGetProperty("mobileId", out var mid) && (mid.ValueKind == JsonValueKind.Number || mid.ValueKind == JsonValueKind.String))
                {
                    if (mid.ValueKind == JsonValueKind.Number && mid.TryGetInt64(out var v)) return "mid:" + v;
                    if (mid.ValueKind == JsonValueKind.String) return "mid_s:" + mid.GetString();
                }
                if (el.TryGetProperty("entSeq", out var ent) && ent.ValueKind == JsonValueKind.Number)
                {
                    if (ent.TryGetInt64(out var vv)) return "ent:" + vv;
                }
                if (el.TryGetProperty("_id", out var idp) && idp.ValueKind == JsonValueKind.String)
                {
                    return "_id:" + idp.GetString();
                }

                string word = null, reading = null;
                if (el.TryGetProperty("word", out var w) && w.ValueKind == JsonValueKind.String) word = w.GetString();
                if (string.IsNullOrEmpty(word) && el.TryGetProperty("kanji", out var k) && k.ValueKind == JsonValueKind.String) word = k.GetString();
                if (string.IsNullOrEmpty(word) && el.TryGetProperty("w", out var w2) && w2.ValueKind == JsonValueKind.String) word = w2.GetString(); // alternate names
                if (el.TryGetProperty("reading", out var r) && r.ValueKind == JsonValueKind.String) reading = r.GetString();
                if (string.IsNullOrEmpty(reading) && el.TryGetProperty("phonetic", out var p) && p.ValueKind == JsonValueKind.String) reading = p.GetString();
                if (string.IsNullOrEmpty(reading) && el.TryGetProperty("reb", out var reb) && reb.ValueKind == JsonValueKind.String) reading = reb.GetString();

                if (!string.IsNullOrEmpty(word) || !string.IsNullOrEmpty(reading))
                {
                    return $"w:{word ?? ""}|r:{reading ?? ""}";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeCompleteWordResult(JsonElement el)
        {
            // heuristics: has reading and has senses array or gloss-like fields
            try
            {
                bool hasReading = (el.TryGetProperty("reading", out var r) && r.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(r.GetString()))
                                  || (el.TryGetProperty("reb", out var reb) && reb.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(reb.GetString()))
                                  || (el.TryGetProperty("phonetic", out var p) && p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString()));
                bool hasSenses = el.TryGetProperty("senses", out var s) && s.ValueKind == JsonValueKind.Array && s.GetArrayLength() > 0;
                bool hasGloss = el.TryGetProperty("gloss", out var g) && g.ValueKind == JsonValueKind.String;
                bool hasMeans = el.TryGetProperty("means", out var m) && m.ValueKind == JsonValueKind.Array && m.GetArrayLength() > 0;
                return hasReading && (hasSenses || hasGloss || hasMeans);
            }
            catch { return false; }
        }

        /// <summary>
        /// Save a JSON result element into DB as Entry (type="word") and try to create Senses/Glosses if available.
        /// Keeps raw json in Entry.RawJson.
        /// NOTE: This method persists progress AFTER successful SaveChanges.
        /// </summary>
        private async Task SaveWordResultToDatabaseInternalAsync(JsonElement resultEl, CancellationToken cancellationToken)
        {
            if (resultEl.ValueKind != JsonValueKind.Object) return;

            long? entSeq = null;
            if (resultEl.TryGetProperty("entSeq", out var ent) && ent.ValueKind == JsonValueKind.Number)
            {
                if (ent.TryGetInt64(out long v)) entSeq = v;
            }
            if (!entSeq.HasValue && resultEl.TryGetProperty("mobileId", out var mid) && mid.ValueKind == JsonValueKind.Number)
            {
                if (mid.TryGetInt64(out long mv)) entSeq = mv;
            }

            string label = null;
            if (resultEl.TryGetProperty("word", out var w) && w.ValueKind == JsonValueKind.String) label = w.GetString();
            if (string.IsNullOrWhiteSpace(label) && resultEl.TryGetProperty("kanji", out var k) && k.ValueKind == JsonValueKind.String) label = k.GetString();
            if (string.IsNullOrWhiteSpace(label) && resultEl.TryGetProperty("reading", out var r2) && r2.ValueKind == JsonValueKind.String) label = r2.GetString();
            label = label ?? "ja_vi";

            var rawJson = resultEl.GetRawText();

            // FIND by entSeq only (avoid expensive label/table scans). If entSeq not present, create new entry.
            Entry entry = null;
            if (entSeq.HasValue)
            {
                entry = await _db.Entries.FirstOrDefaultAsync(e => e.EntSeq == entSeq.Value, cancellationToken);
            }

            if (entry == null)
            {
                entry = new Entry
                {
                    Type = "word",
                    Label = label,
                    RawJson = rawJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                if (entSeq.HasValue) entry.EntSeq = entSeq.Value;
                _db.Entries.Add(entry);
            }
            else
            {
                entry.RawJson = rawJson;
                entry.UpdatedAt = DateTime.UtcNow;
                if (entSeq.HasValue) entry.EntSeq = entSeq.Value;
            }

            // create senses & glosses if present
            Language viLang = null;
            try
            {
                viLang = await GetOrCreateLanguageAsync("vi", "Vietnamese", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get/create language 'vi'");
            }

            if (resultEl.TryGetProperty("senses", out var sensesEl) && sensesEl.ValueKind == JsonValueKind.Array)
            {
                int senseOrder = 1;
                foreach (var s in sensesEl.EnumerateArray())
                {
                    var sinf = string.Empty;
                    if (s.TryGetProperty("info", out var info) && info.ValueKind == JsonValueKind.String) sinf = info.GetString();

                    var sense = new Sense
                    {
                        Entry = entry,
                        Pos = string.Empty,
                        Field = string.Empty,
                        Misc = string.Empty,
                        SInf = sinf ?? string.Empty,
                        Dialect = string.Empty,
                        SenseOrder = senseOrder++
                    };
                    _db.Senses.Add(sense);

                    if (s.TryGetProperty("glosses", out var glossesEl) && glossesEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var g in glossesEl.EnumerateArray())
                        {
                            if (g.ValueKind == JsonValueKind.String)
                            {
                                var txt = g.GetString();
                                if (!string.IsNullOrWhiteSpace(txt) && viLang != null)
                                {
                                    _db.Glosses.Add(new Gloss
                                    {
                                        Sense = sense,
                                        Language = viLang,
                                        Text = txt,
                                        GType = string.Empty,
                                        GGend = string.Empty,
                                        Priority = string.Empty
                                    });
                                }
                            }
                        }
                    }
                    else if (s.TryGetProperty("gloss", out var g2) && g2.ValueKind == JsonValueKind.String && viLang != null)
                    {
                        var txt = g2.GetString();
                        if (!string.IsNullOrWhiteSpace(txt))
                        {
                            _db.Glosses.Add(new Gloss
                            {
                                Sense = sense,
                                Language = viLang,
                                Text = txt,
                                GType = string.Empty,
                                GGend = string.Empty,
                                Priority = string.Empty
                            });
                        }
                    }
                }
            }
            else if (resultEl.TryGetProperty("means", out var meansEl) && meansEl.ValueKind == JsonValueKind.Array)
            {
                int senseOrder = 1;
                foreach (var m in meansEl.EnumerateArray())
                {
                    string meanText = null;
                    if (m.ValueKind == JsonValueKind.Object && m.TryGetProperty("mean", out var mm) && mm.ValueKind == JsonValueKind.String)
                        meanText = mm.GetString();
                    else if (m.ValueKind == JsonValueKind.String)
                        meanText = m.GetString();

                    if (meanText == null) continue;

                    var sense = new Sense
                    {
                        Entry = entry,
                        Pos = string.Empty,
                        Field = string.Empty,
                        Misc = string.Empty,
                        SInf = string.Empty,
                        Dialect = string.Empty,
                        SenseOrder = senseOrder++
                    };
                    _db.Senses.Add(sense);

                    if (!string.IsNullOrWhiteSpace(meanText) && viLang != null)
                    {
                        _db.Glosses.Add(new Gloss
                        {
                            Sense = sense,
                            Language = viLang,
                            Text = meanText,
                            GType = string.Empty,
                            GGend = string.Empty,
                            Priority = string.Empty
                        });
                    }
                }
            }
            else if (resultEl.TryGetProperty("mean", out var meanSingle) && meanSingle.ValueKind == JsonValueKind.String && viLang != null)
            {
                var sense = new Sense
                {
                    Entry = entry,
                    Pos = string.Empty,
                    Field = string.Empty,
                    Misc = string.Empty,
                    SInf = string.Empty,
                    Dialect = string.Empty,
                    SenseOrder = 1
                };
                _db.Senses.Add(sense);
                _db.Glosses.Add(new Gloss
                {
                    Sense = sense,
                    Language = viLang,
                    Text = meanSingle.GetString(),
                    GType = string.Empty,
                    GGend = string.Empty,
                    Priority = string.Empty
                });
            }

            // sanitize/truncate string props before saving
            foreach (var trk in _db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                foreach (var prop in trk.Properties.Where(p => p.Metadata.ClrType == typeof(string)))
                {
                    var cur = prop.CurrentValue as string;
                    if (cur == null) { prop.CurrentValue = string.Empty; continue; }
                    var maxLen = prop.Metadata.GetMaxLength();
                    if (maxLen.HasValue && cur.Length > maxLen.Value)
                        prop.CurrentValue = cur.Substring(0, maxLen.Value);
                }
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                _db.ChangeTracker.Clear();

                // persist progress AFTER DB save succeeded
                SaveProcessedLabelBuffered(label);
                if (entSeq.HasValue) _processedEntSeqs.Add(entSeq.Value);

                _logger.LogInformation("Saved Entry (word) label={lab} entSeq={es}", label, entSeq?.ToString() ?? "(null)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed SaveChanges for Entry word label={lab}", label);
                throw;
            }
        }

        /// <summary>
        /// Save raw JSON into entries as a backup (type="word") so you can inspect later and re-run/repair.
        /// This persists progress for the provided label only after saving.
        /// </summary>
        private async Task SaveRawBackupEntryAsync(string labelHint, string rawJson, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return;

            var entry = new Entry
            {
                Type = "word",
                Label = string.IsNullOrWhiteSpace(labelHint) ? "ja_vi" : labelHint,
                RawJson = rawJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Entries.Add(entry);

            // truncate strings if needed
            foreach (var trk in _db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                foreach (var prop in trk.Properties.Where(p => p.Metadata.ClrType == typeof(string)))
                {
                    var cur = prop.CurrentValue as string;
                    if (cur == null) { prop.CurrentValue = string.Empty; continue; }
                    var maxLen = prop.Metadata.GetMaxLength();
                    if (maxLen.HasValue && cur.Length > maxLen.Value)
                        prop.CurrentValue = cur.Substring(0, maxLen.Value);
                }
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                _db.ChangeTracker.Clear();

                // persist progress for this label too
                SaveProcessedLabelBuffered(labelHint);

                _logger.LogInformation("Saved raw backup Entry label={lab} (raw saved)", labelHint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save raw backup entry label={lab}", labelHint);
                throw;
            }
        }

        // ---------------- Helper: generate query variants ----------------
        public static IEnumerable<string> GenerateQueryVariants(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) yield break;
            var norm = Normalize(input);
            var set = new HashSet<string>(StringComparer.Ordinal);

            set.Add(norm);
            var trimmed = Regex.Replace(norm, @"\s+", "", RegexOptions.Compiled);
            set.Add(trimmed);

            bool hasHiragana = Regex.IsMatch(norm, @"\p{IsHiragana}");
            bool hasKatakana = Regex.IsMatch(norm, @"\p{IsKatakana}");

            if (hasHiragana)
                set.Add(HiraganaToKatakana(norm));
            if (hasKatakana)
                set.Add(KatakanaToHiragana(norm));

            var kanjiOnly = ExtractKanjiOnly(norm);
            if (!string.IsNullOrEmpty(kanjiOnly)) set.Add(kanjiOnly);

            // add reading-only if exists
            var kanaOnlyMatch = Regex.Match(norm, @"[\p{IsHiragana}\p{IsKatakana}]+");
            if (kanaOnlyMatch.Success)
            {
                var kanaOnly = kanaOnlyMatch.Value;
                set.Add(kanaOnly);
                set.Add(HiraganaToKatakana(kanaOnly));
                set.Add(KatakanaToHiragana(kanaOnly));
            }

            // escaped variations + upper/lower
            foreach (var v in set.ToList())
            {
                set.Add(ToJsonUnicodeEscape(v));
                set.Add(ToJsonUnicodeEscape(v).Replace(@"\", @"\\"));
                set.Add(v.ToLowerInvariant());
                set.Add(v.ToUpperInvariant());
            }

            // priority: kanji-only, canonical, hiragana, katakana, others
            if (!string.IsNullOrEmpty(kanjiOnly)) yield return kanjiOnly;
            yield return norm;

            foreach (var v in set.Where(x => Regex.IsMatch(x, @"^\p{IsHiragana}+$")).Distinct()) yield return v;
            foreach (var v in set.Where(x => Regex.IsMatch(x, @"^\p{IsKatakana}+$")).Distinct()) yield return v;
            foreach (var v in set.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(norm, StringComparison.Ordinal) && !x.Equals(kanjiOnly, StringComparison.Ordinal)).Distinct()) yield return v;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Normalize(System.Text.NormalizationForm.FormKC).Trim();
        }

        private static string ExtractKanjiOnly(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                if (CjkRegex.IsMatch(ch.ToString()))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string ToJsonUnicodeEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder();
            foreach (var ch in s)
                sb.Append("\\u").Append(((int)ch).ToString("X4"));
            return sb.ToString();
        }

        private static string KatakanaToHiragana(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch >= '\u30A1' && ch <= '\u30F4')
                    sb.Append((char)(ch - 0x60));
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string HiraganaToKatakana(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch >= '\u3041' && ch <= '\u3096')
                    sb.Append((char)(ch + 0x60));
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        // ---------- GetOrCreateLanguageAsync (re-used) ----------
        private async Task<Language> GetOrCreateLanguageAsync(string code, string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code));

            var tracked = _db.ChangeTracker
                             .Entries<Language>()
                             .Where(e => e.State != EntityState.Deleted)
                             .Select(e => e.Entity)
                             .FirstOrDefault(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));
            if (tracked != null) return tracked;

            var langFromDb = await _db.Languages.FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
            if (langFromDb != null) return langFromDb;

            var lang = new Language
            {
                Code = code,
                Name = name ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Languages.Add(lang);

            return lang;
        }
    }
}
