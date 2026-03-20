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
using Dict.Data;
using Dict.Models;

namespace Dict.Service
{
    /// <summary>
    /// Import words but ONLY persist Entry objects (RawJson) — no Senses/Glosses/Words/etc.
    /// Keeps the original TSV reading behaviour but batches saves for performance.
    /// </summary>
    public class WordEntryOnlyImportService
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WordEntryOnlyImportService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Random _rng = new();

        // HTTP / retry configuration (copied from original)
        private const int MaxHttpRetries = 5;
        private const int InitialBackoffMs = 300;
        private const int MaxBackoffMs = 10_000;
        private const int RequestDelayMs = 0;

        private static readonly Regex CjkRegex = new(@"[\u4E00-\u9FFF\u3400-\u4DBF\uF900-\uFAFF]", RegexOptions.Compiled);

        // batching settings
        private const int CommitEvery = 100; // commit every N entries to DB for speed
        private readonly List<Entry> _pendingEntries = new();
        private readonly HashSet<string> _pendingLabelCache = new(StringComparer.Ordinal); // to avoid duplicate pending labels

        public WordEntryOnlyImportService(HttpClient http, ApplicationDbContext db, ILogger<WordEntryOnlyImportService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        }

        /// <summary>
        /// Same signature / reading behaviour as your original ImportWordsFromTsvAsync.
        /// This method will process each TSV row and only persist Entries (type="word") containing RawJson.
        /// </summary>
        public async Task ImportWordsFromTsvAsync(string tsvPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tsvPath)) throw new ArgumentNullException(nameof(tsvPath));
            if (!File.Exists(tsvPath)) throw new FileNotFoundException("TSV file not found", tsvPath);

            using var sr = new StreamReader(tsvPath, Encoding.UTF8);
            string header = await sr.ReadLineAsync(); // expect header present
            _logger.LogInformation("Reading TSV {file} header={hdr}", tsvPath, header);

            int lineNo = 1;

            while (!sr.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await sr.ReadLineAsync();
                lineNo++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                // parse TSV - tolerant
                var cols = line.Split('\t');
                // out.tsv header: ent_seq	headword	reading	canonical	sense_index	pos_list	glosses
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

                try
                {
                    await ProcessSingleWordAsync(entSeqStr, canonical, headword, reading, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cancellation requested - stopping import.");
                    break;

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing TSV line {ln} canonical={c}", lineNo, canonical);
                }

                // small polite delay between TSV rows
                await Task.Delay(0, cancellationToken);
            }

            // flush any remaining pending entries
            await FlushPendingAsync(cancellationToken);

            _logger.LogInformation("ImportWordsFromTsvAsync finished reading {file}", tsvPath);
        }

        private async Task ProcessSingleWordAsync(string entSeqStr, string canonical, string headword, string reading, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // generate query variants (reuse original helper)
            var variants = GenerateQueryVariants(canonical).Distinct(StringComparer.Ordinal).ToList();
            _logger.LogDebug("Processing word canonical='{c}' variants={vcount}", canonical, variants.Count);

            // deduped results keyed by stable key
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
                            {
                                items.Add(root);
                            }
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

                // optionally stop if found a complete result
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
                        await SaveRawBackupEntryOnlyAsync(canonical, lastRawResponse, cancellationToken);
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

            // Save results as Entries only (batched)
            foreach (var kv in results)
            {
                var el = kv.Value;
                try
                {
                    await SaveEntryOnlyAsync(el, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue Entry for result key={k}", kv.Key);
                }
            }
        }

        /// <summary>
        /// Post to /api/search/word and parse JSON (with retry/backoff).
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
        /// Save a single API result as Entry (queued, committed in batches).
        /// Only persists Entry (type="word"), nothing else.
        /// </summary>
        private async Task SaveEntryOnlyAsync(JsonElement resultEl, CancellationToken cancellationToken)
        {
            if (resultEl.ValueKind != JsonValueKind.Object) return;

            // extract entSeq / mobileId if present
            long? entSeq = null;
            if (resultEl.TryGetProperty("entSeq", out var ent) && ent.ValueKind == JsonValueKind.Number)
            {
                if (ent.TryGetInt64(out long v)) entSeq = v;
            }
            if (!entSeq.HasValue && resultEl.TryGetProperty("mobileId", out var mid) && mid.ValueKind == JsonValueKind.Number)
            {
                if (mid.TryGetInt64(out long mv)) entSeq = mv;
            }

            // label selection
            string label = null;
            if (resultEl.TryGetProperty("word", out var w) && w.ValueKind == JsonValueKind.String) label = w.GetString();
            if (string.IsNullOrWhiteSpace(label) && resultEl.TryGetProperty("kanji", out var k) && k.ValueKind == JsonValueKind.String) label = k.GetString();
            if (string.IsNullOrWhiteSpace(label) && resultEl.TryGetProperty("reading", out var r2) && r2.ValueKind == JsonValueKind.String) label = r2.GetString();
            label = label ?? "ja_vi";

            var rawJson = resultEl.GetRawText();

            // try find existing entry by entSeq if available, else by a tolerant LIKE on label
            Entry entryFromDb = null;
            if (entSeq.HasValue)
            {
                entryFromDb = await _db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.EntSeq == entSeq.Value, cancellationToken);
            }

            if (entryFromDb == null)
            {
                // try tolerant match: rawjson contains label
                entryFromDb = await _db.Entries.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Type == "word" && e.RawJson != null && EF.Functions.Like(e.RawJson, $"%{label}%"), cancellationToken);
            }

            if (entryFromDb != null)
            {
                // update existing entry: we'll create a new Entry object with same Id to be tracked as Modified
                var updated = new Entry
                {
                    Id = entryFromDb.Id,
                    Type = "word",
                    Label = label,
                    RawJson = rawJson,
                    CreatedAt = entryFromDb.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                };
                if (entSeq.HasValue) updated.EntSeq = entSeq.Value;

                // add to pending as "update" — using _db.Update when flushing
                _pendingEntries.Add(updated);
            }
            else
            {
                // avoid duplicate pending label to reduce duplicates in pending batch
                if (_pendingLabelCache.Contains(label) && !entSeq.HasValue)
                {
                    // already queued a similar label in this batch; skip to avoid duplicates
                    _logger.LogDebug("Skipping duplicate pending label enqueue: {lab}", label);
                    return;
                }

                var newEntry = new Entry
                {
                    Type = "word",
                    Label = label,
                    RawJson = rawJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                if (entSeq.HasValue) newEntry.EntSeq = entSeq.Value;

                _pendingEntries.Add(newEntry);
                _pendingLabelCache.Add(label);
            }

            // flush if we reached threshold
            if (_pendingEntries.Count >= CommitEvery)
            {
                await FlushPendingAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Persist pending entries to DB in a single SaveChanges, with AutoDetectChanges disabled for speed.
        /// For entries that have Id set (updates), call Update; else Add.
        /// </summary>
        private async Task FlushPendingAsync(CancellationToken cancellationToken)
        {
            if (_pendingEntries.Count == 0) return;

            // disable AutoDetectChanges for bulk add for performance
            var prevAutoDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                foreach (var e in _pendingEntries)
                {
                    if (e.Id != 0)
                    {
                        // update: attach as modified without loading
                        _db.Entry(e).State = EntityState.Modified;
                        // Only update the fields we care about
                        _db.Entry(e).Property(p => p.Type).IsModified = true;
                        _db.Entry(e).Property(p => p.Label).IsModified = true;
                        _db.Entry(e).Property(p => p.RawJson).IsModified = true;
                        _db.Entry(e).Property(p => p.UpdatedAt).IsModified = true;
                        if (e.EntSeq != 0) _db.Entry(e).Property(p => p.EntSeq).IsModified = true;
                    }
                    else
                    {
                        _db.Entries.Add(e);
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Flushed {count} pending Entry records (committed)", _pendingEntries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed saving pending Entries — clearing pending list to avoid re-trying duplicates. Consider logging pending entries separately.");
                // On failure: clear caches to avoid repeated failing attempts; you might prefer to write to disk for later analysis.
            }
            finally
            {
                _pendingEntries.Clear();
                _pendingLabelCache.Clear();
                _db.ChangeTracker.AutoDetectChangesEnabled = prevAutoDetect;
            }
        }

        /// <summary>
        /// Save raw JSON into entries as backup (Entry only).
        /// Immediate save (not batched) because it's rare — used when no structured results.
        /// </summary>
        private async Task SaveRawBackupEntryOnlyAsync(string labelHint, string rawJson, CancellationToken cancellationToken)
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

            // sanitize/truncate string props if needed (basic)
            foreach (var prop in entry.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var val = prop.GetValue(entry) as string;
                if (val == null) prop.SetValue(entry, string.Empty);
            }

            _db.Entries.Add(entry);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved raw backup Entry label={lab} (raw saved)", labelHint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save raw backup entry label={lab}", labelHint);
                throw;
            }
        }

        // ----------------- Helpers (same as original) -----------------

        private static string DedupKeyFromResult(JsonElement el)
        {
            try
            {
                if (el.ValueKind != JsonValueKind.Object) return null;

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
                if (string.IsNullOrEmpty(word) && el.TryGetProperty("w", out var w2) && w2.ValueKind == JsonValueKind.String) word = w2.GetString();
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

        // Variant generator (same behaviour as original code)
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

            if (hasHiragana) set.Add(HiraganaToKatakana(norm));
            if (hasKatakana) set.Add(KatakanaToHiragana(norm));

            var kanjiOnly = ExtractKanjiOnly(norm);
            if (!string.IsNullOrEmpty(kanjiOnly)) set.Add(kanjiOnly);

            var kanaOnlyMatch = Regex.Match(norm, @"[\p{IsHiragana}\p{IsKatakana}]+");
            if (kanaOnlyMatch.Success)
            {
                var kanaOnly = kanaOnlyMatch.Value;
                set.Add(kanaOnly);
                set.Add(HiraganaToKatakana(kanaOnly));
                set.Add(KatakanaToHiragana(kanaOnly));
            }

            foreach (var v in set.ToList())
            {
                set.Add(ToJsonUnicodeEscape(v));
                set.Add(ToJsonUnicodeEscape(v).Replace(@"\", @"\\"));
                set.Add(v.ToLowerInvariant());
                set.Add(v.ToUpperInvariant());
            }

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
    }
}
