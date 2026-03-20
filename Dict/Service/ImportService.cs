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
    public class KanjiImportService
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<KanjiImportService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Random _rng = new(); // reuse Random to avoid identical seeds

        // configuration - tweak as needed
        private const int MaxHttpRetries = 4;
        private const int InitialHttpBackoffMs = 500;
        private const int MaxHttpBackoffMs = 10_000;
        private const int DelayBetweenRequestsMs = 200;

        // compiled regex for CJK ranges used by parsing fallbacks
        private static readonly Regex CjkRegex = new(@"[\u4E00-\u9FFF\u3400-\u4DBF\uF900-\uFAFF]", RegexOptions.Compiled);

        public KanjiImportService(HttpClient httpClient, ApplicationDbContext db, ILogger<KanjiImportService> logger)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // safe defaults for HttpClient - can be adjusted upstream if needed
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("KanjiImportService/1.0 (+https://example)");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        // ---------- existing import methods (unchanged) ----------
        public async Task ImportKanjiFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {file}", filePath);
                return;
            }

            var processedFile = filePath + ".processed";
            var processed = new HashSet<string>(StringComparer.Ordinal);

            if (File.Exists(processedFile))
            {
                foreach (var ln in await File.ReadAllLinesAsync(processedFile, cancellationToken))
                {
                    var s = (ln ?? string.Empty).Trim();
                    if (s.Length > 0) processed.Add(s);
                }
                _logger.LogInformation("Loaded {count} already-processed kanji from {file}", processed.Count, processedFile);
            }

            // read using UTF-8 explicitly to avoid encoding surprises. If your file is SHIFT-JIS/EUC-JP, convert externally.
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            var kanjis = lines.Select(l => (l ?? string.Empty).Trim()).Where(s => s.Length > 0).ToList();
            _logger.LogInformation("Found {n} kanji in source file (will skip {m} already processed)", kanjis.Count, processed.Count);

            foreach (var kanji in kanjis)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (processed.Contains(kanji))
                {
                    _logger.LogDebug("Skipping already processed kanji: {k}", kanji);
                    continue;
                }

                try
                {
                    var ok = await ProcessSingleKanjiAsync(kanji, cancellationToken);
                    if (ok)
                    {
                        await File.AppendAllTextAsync(processedFile, kanji + Environment.NewLine, cancellationToken);
                        processed.Add(kanji);
                        _logger.LogInformation("Processed and saved kanji: {k}", kanji);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to fully process kanji (skipped after retries): {k}", kanji);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cancellation requested - stopping import.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception processing kanji {k} - continuing with next", kanji);
                }

                await Task.Delay(DelayBetweenRequestsMs, cancellationToken);
            }

            _logger.LogInformation("ImportKanjiFromFileAsync finished.");
        }

        private async Task<bool> ProcessSingleKanjiAsync(string kanjiChar, CancellationToken cancellationToken)
        {
            var payload = new
            {
                dict = "javi",
                type = "kanji",
                query = kanjiChar,
                page = 1
            };

            string responseContent = null;
            bool httpSuccess = false;
            var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
            for (int attempt = 0; attempt < MaxHttpRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                    using var resp = await _http.PostAsync("https://mazii.net/api/search/kanji", content, cancellationToken);

                    if (resp.IsSuccessStatusCode)
                    {
                        responseContent = await resp.Content.ReadAsStringAsync(cancellationToken);
                        httpSuccess = true;
                        break;
                    }

                    _logger.LogWarning("HTTP {code} for kanji {k} (attempt {a}/{max})", (int)resp.StatusCode, kanjiChar, attempt + 1, MaxHttpRetries);
                }
                catch (HttpRequestException hre)
                {
                    _logger.LogWarning(hre, "HttpRequestException for kanji {k} attempt {a}/{max}", kanjiChar, attempt + 1, MaxHttpRetries);
                }

                var backoff = Math.Min(InitialHttpBackoffMs * (1 << attempt), MaxHttpBackoffMs);
                var jitter = (int)(_rng.NextDouble() * 200);
                await Task.Delay(backoff + jitter, cancellationToken);
            }

            if (!httpSuccess)
            {
                _logger.LogWarning("Giving up HTTP for kanji {k} after {max} attempts", kanjiChar, MaxHttpRetries);
                return false;
            }

            // parse and save (use Normalize to handle many formats)
            List<KanjiResult> results = NormalizeRawJsonToKanjiResults(responseContent, out var parseLog);
            _logger.LogDebug("ProcessSingleKanjiAsync parseLog={log}", parseLog);

            if (results == null || results.Count == 0)
            {
                _logger.LogInformation("No usable results returned for kanji {k}. parseLog={log} Sample={s}",
                    kanjiChar, parseLog,
                    (responseContent ?? string.Empty).Length > 1000 ? (responseContent ?? string.Empty).Substring(0, 1000) + "..." : responseContent);
                return false;
            }

            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

                    foreach (var res in results)
                    {
                        await SaveResultToDatabaseInternalAsync(res, cancellationToken);
                    }

                    // sanitize string properties & truncate to column max
                    foreach (var entry in _db.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
                    {
                        foreach (var prop in entry.Properties.Where(p => p.Metadata.ClrType == typeof(string)))
                        {
                            var cur = prop.CurrentValue as string;
                            if (cur == null)
                            {
                                prop.CurrentValue = string.Empty;
                                continue;
                            }

                            var maxLen = prop.Metadata.GetMaxLength();
                            if (maxLen.HasValue && cur.Length > maxLen.Value)
                            {
                                _logger.LogWarning("Truncating property {Entity}.{Prop} from {Len} to {Max}",
                                    entry.Entity.GetType().Name, prop.Metadata.Name, cur.Length, maxLen.Value);
                                prop.CurrentValue = cur.Substring(0, maxLen.Value);
                            }
                        }
                    }

                    try
                    {
                        // dump pending changes for debugging if no inserts detected (dev)
                        var pending = _db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList();
                        _logger.LogDebug("Pending changes before SaveChanges: count={cnt} types={list}",
                            pending.Count, string.Join(", ", pending.Select(e => $"{e.Entity.GetType().Name}({e.State})")));

                        await _db.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "DbUpdateException saving results for kanji {k}. Entries: {entries}. Inner: {inner}",
                            results.FirstOrDefault()?.Kanji ?? kanjiChar,
                            string.Join(", ", dbEx.Entries.Select(e => e.Entity.GetType().Name)),
                            dbEx.GetBaseException()?.Message);
                        throw;
                    }

                    await tx.CommitAsync(cancellationToken);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error saving kanji {k}", kanjiChar);
                return false;
            }

            return true;
        }

        // ---------- Rehydrate with robust parsing & verbose logging ----------
        public async Task RehydrateExamplesFromRawJsonAsync(
            DateTime? sinceUtc = null,
            IEnumerable<string> specificKanji = null,
            int batchLimit = 100,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("RehydrateExamplesFromRawJsonAsync started. sinceUtc={sinceUtc} specificCount={count} batchLimit={limit}",
                sinceUtc?.ToString("o") ?? "null", specificKanji == null ? 0 : specificKanji.Count(), batchLimit);

            bool WordExistsTracked(string wordText, out Word trackedWord)
            {
                trackedWord = _db.ChangeTracker.Entries<Word>()
                                 .Where(x => x.State != EntityState.Deleted)
                                 .Select(x => x.Entity)
                                 .FirstOrDefault(w => string.Equals(w.WordText ?? string.Empty, wordText ?? string.Empty, StringComparison.Ordinal));
                return trackedWord != null;
            }

            bool WordToEntryExistsTracked(Word w, Entry e)
            {
                return _db.ChangeTracker.Entries<WordToEntry>()
                          .Where(x => x.State != EntityState.Deleted)
                          .Select(x => x.Entity)
                          .Any(x => ReferenceEquals(x.Word, w) && ReferenceEquals(x.Entry, e));
            }

            bool WordKanjiExistsTracked(Word w, Kanji k)
            {
                return _db.ChangeTracker.Entries<WordKanji>()
                          .Where(x => x.State != EntityState.Deleted)
                          .Select(x => x.Entity)
                          .Any(x => ReferenceEquals(x.Word, w) && ReferenceEquals(x.Kanji, k));
            }

            // base query - server-side filter to limit work
            var baseQ = _db.Entries.AsQueryable().Where(e => e.Type == "kanji" && e.RawJson != null);
            if (sinceUtc.HasValue)
                baseQ = baseQ.Where(e => e.CreatedAt >= sinceUtc.Value || e.UpdatedAt >= sinceUtc.Value);

            // exclude entries that already have examples (any sense->example)
            var entriesWithExamples = _db.Senses
                .Where(s => _db.Examples.Any(ex => ex.SenseId == s.Id))
                .Select(s => s.EntryId)
                .Distinct();

            baseQ = baseQ.Where(e => !entriesWithExamples.Contains(e.Id));

            // fetch limited candidates (client-side filtering will refine further)
            var candidates = await baseQ.OrderByDescending(e => e.UpdatedAt).Take(batchLimit).ToListAsync(cancellationToken);
            _logger.LogInformation("Fetched {count} candidates from DB (before client-side filter). IDs: {ids}", candidates.Count,
                string.Join(',', candidates.Select(c => c.Id)));

            foreach (var c in candidates.Take(5))
            {
                var sample = (c.RawJson ?? string.Empty);
                var truncated = sample.Length > 800 ? sample.Substring(0, 800) + "..." : sample;
                _logger.LogDebug("Candidate Id={id} UpdatedAt={u} RawJsonSample={s}", c.Id, c.UpdatedAt, truncated);
            }

            // client-side filtering if specificKanji provided (because server-side LIKE patterns can miss escaped forms)
            var filterList = specificKanji?.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            if (filterList != null && filterList.Count > 0)
            {
                var normalizedWanted = new HashSet<string>(filterList.Select(s => s.Normalize(System.Text.NormalizationForm.FormC)), StringComparer.Ordinal);
                var filtered = new List<Entry>();

                foreach (var e in candidates)
                {
                    try
                    {
                        var raw = e.RawJson ?? string.Empty;
                        var parsed = NormalizeRawJsonToKanjiResults(raw, out var parseLog);

                        bool matched = false;
                        if (parsed != null)
                        {
                            foreach (var r in parsed)
                            {
                                if (string.IsNullOrEmpty(r?.Kanji)) continue;
                                var k = r.Kanji.Normalize(System.Text.NormalizationForm.FormC);
                                if (normalizedWanted.Contains(k))
                                {
                                    matched = true;
                                    break;
                                }
                            }
                        }

                        // final fallback: check raw contains wanted literal or \uXXXX escaped form
                        if (!matched)
                        {
                            foreach (var w in normalizedWanted)
                            {
                                if (!string.IsNullOrEmpty(raw) && raw.Contains(w, StringComparison.Ordinal))
                                {
                                    matched = true; break;
                                }
                                var esc = ToJsonUnicodeEscape(w);
                                if (!string.IsNullOrEmpty(raw) && (raw.Contains(esc, StringComparison.Ordinal) || raw.Contains(esc.ToLowerInvariant(), StringComparison.Ordinal)))
                                {
                                    matched = true; break;
                                }
                            }
                        }

                        _logger.LogDebug("ClientFilter Entry.Id={id} matched={m} parseLog={log}", e.Id, matched, parseLog);
                        if (matched) filtered.Add(e);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while client-filtering entry id {id}", e.Id);
                    }
                }

                candidates = filtered;
                _logger.LogInformation("After client-side specificKanji filter, {count} candidates remain. IDs: {ids}",
                    candidates.Count, string.Join(',', candidates.Select(c => c.Id)));
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            int processed = 0, succeeded = 0, failed = 0;

            foreach (var entry in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            var results = NormalizeRawJsonToKanjiResults(entry.RawJson, out var parseLog);
                            _logger.LogDebug("Entry.Id={id} normalizeParseLog={log} resultsCount={c}", entry.Id, parseLog, results?.Count ?? 0);

                            if (results == null || results.Count == 0)
                            {
                                _logger.LogWarning("Entry.Id={id} parsing produced no results - skipping. parseLog={log} RawJsonSample={sample}",
                                    entry.Id, parseLog,
                                    (entry.RawJson ?? string.Empty).Length > 800 ? (entry.RawJson ?? string.Empty).Substring(0, 800) + "..." : entry.RawJson);
                                await tx.CommitAsync(cancellationToken);
                                return;
                            }

                            var viLang = await GetOrCreateLanguageAsync("vi", "Vietnamese", cancellationToken);

                            foreach (var res in results)
                            {
                                if (res == null || string.IsNullOrWhiteSpace(res.Kanji) || !IsLikelyKanji(res.Kanji))
                                {
                                    _logger.LogWarning("Entry.Id={id} skipping invalid Kanji token: '{k}'", entry.Id, res?.Kanji);
                                    continue;
                                }

                                var kanjiEntity = await _db.Kanji.FirstOrDefaultAsync(k => k.Character == res.Kanji, cancellationToken);
                                if (kanjiEntity == null)
                                {
                                    kanjiEntity = new Kanji
                                    {
                                        Character = res.Kanji,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow,
                                        JlptLevel = string.Empty,
                                        Meaning = string.Empty
                                    };
                                    _db.Kanji.Add(kanjiEntity);
                                    _logger.LogDebug("Created Kanji entity for {ch} when rehydrating Entry.Id={id}", res.Kanji, entry.Id);
                                }

                                // keep RawJson as-is if it's already present; but ensure we have a canonical RawJson stored when creating from API
                                if (string.IsNullOrWhiteSpace(entry.RawJson))
                                    entry.RawJson = JsonSerializer.Serialize(res, _jsonOptions);

                                entry.UpdatedAt = DateTime.UtcNow;

                                var sense = await _db.Senses.FirstOrDefaultAsync(s => s.EntryId == entry.Id, cancellationToken);
                                if (sense == null)
                                {
                                    sense = new Sense
                                    {
                                        Entry = entry,
                                        Pos = string.Empty,
                                        Field = string.Empty,
                                        Misc = string.Empty,
                                        SInf = res.Detail ?? string.Empty,
                                        Dialect = string.Empty,
                                        SenseOrder = 1
                                    };
                                    _db.Senses.Add(sense);
                                }

                                // helper to add examples & words
                                async Task AddExampleAndWordLinksFromResAsync(ExampleItem ex, string sourceTag)
                                {
                                    if (ex == null) return;
                                    if (string.IsNullOrWhiteSpace(ex.W) && string.IsNullOrWhiteSpace(ex.M)) return;

                                    var jp = string.IsNullOrWhiteSpace(ex.W) ? string.Empty : ex.W.Trim();
                                    var tr = string.IsNullOrWhiteSpace(ex.M) ? string.Empty : ex.M.Trim();
                                    var trsc = string.IsNullOrWhiteSpace(ex.P) ? string.Empty : ex.P.Trim();

                                    var exampleExists = await _db.Examples.AnyAsync(x =>
                                        x.SenseId == sense.Id && x.ContentJp == jp && x.ContentTranslated == tr, cancellationToken);

                                    if (!exampleExists)
                                    {
                                        var example = new Example
                                        {
                                            Sense = sense,
                                            ContentJp = jp,
                                            ContentTranslated = tr,
                                            Transcription = trsc,
                                            SourceRef = "mazii:" + sourceTag
                                        };
                                        _db.Examples.Add(example);
                                        _logger.LogDebug("Queued Example (Entry.Id={id}) JP='{jp}'", entry.Id, jp);
                                    }

                                    if (string.IsNullOrWhiteSpace(jp)) return;

                                    Word trackedWord = null;
                                    if (!WordExistsTracked(jp, out trackedWord))
                                    {
                                        var wordFromDb = await _db.Words.FirstOrDefaultAsync(w => w.WordText == jp, cancellationToken);
                                        if (wordFromDb == null)
                                        {
                                            wordFromDb = new Word
                                            {
                                                WordText = jp,
                                                Phonetic = trsc,
                                                ShortMean = tr,
                                                Romaji = string.Empty,
                                                CreatedAt = DateTime.UtcNow,
                                                UpdatedAt = DateTime.UtcNow
                                            };
                                            _db.Words.Add(wordFromDb);
                                            trackedWord = wordFromDb;
                                            _logger.LogDebug("Queued new Word for insert: {word}", jp);
                                        }
                                        else
                                        {
                                            trackedWord = wordFromDb;
                                            _logger.LogDebug("Found existing Word in DB: {word}", jp);
                                        }
                                    }

                                    if (trackedWord != null)
                                    {
                                        trackedWord.WordText = trackedWord.WordText ?? string.Empty;
                                        trackedWord.Phonetic = trackedWord.Phonetic ?? string.Empty;
                                        trackedWord.ShortMean = trackedWord.ShortMean ?? string.Empty;
                                        trackedWord.Romaji = trackedWord.Romaji ?? string.Empty;
                                        trackedWord.UpdatedAt = DateTime.UtcNow;

                                        var createWte = true;
                                        if (trackedWord.Id != 0)
                                        {
                                            var existsMapDb = await _db.WordToEntries.AnyAsync(wte => wte.WordId == trackedWord.Id && wte.EntryId == entry.Id, cancellationToken);
                                            if (existsMapDb) createWte = false;
                                        }
                                        else
                                        {
                                            if (WordToEntryExistsTracked(trackedWord, entry)) createWte = false;
                                        }

                                        if (createWte)
                                        {
                                            _db.WordToEntries.Add(new WordToEntry
                                            {
                                                Word = trackedWord,
                                                Entry = entry,
                                                MappingType = "example"
                                            });
                                        }

                                        var createWk = true;
                                        if (trackedWord.Id != 0)
                                        {
                                            var existsWkDb = await _db.WordKanji.AnyAsync(wk => wk.WordId == trackedWord.Id && wk.KanjiId == kanjiEntity.Id, cancellationToken);
                                            if (existsWkDb) createWk = false;
                                        }
                                        else
                                        {
                                            if (WordKanjiExistsTracked(trackedWord, kanjiEntity)) createWk = false;
                                        }

                                        if (createWk)
                                        {
                                            _db.WordKanji.Add(new WordKanji
                                            {
                                                Word = trackedWord,
                                                Kanji = kanjiEntity
                                            });

                                            _logger.LogDebug("Queued WordKanji for word {word} -> kanji {kanji}", trackedWord.WordText, kanjiEntity.Character);
                                        }
                                    }
                                }

                                if (res.Examples != null)
                                {
                                    foreach (var ex in res.Examples) await AddExampleAndWordLinksFromResAsync(ex, "examples");
                                }
                                if (res.ExampleOn != null)
                                {
                                    foreach (var kv in res.ExampleOn)
                                        foreach (var ex in kv.Value ?? Enumerable.Empty<ExampleItem>())
                                            await AddExampleAndWordLinksFromResAsync(ex, "example_on:" + (kv.Key ?? "on"));
                                }
                                if (res.ExampleKun != null)
                                {
                                    foreach (var kv in res.ExampleKun)
                                        foreach (var ex in kv.Value ?? Enumerable.Empty<ExampleItem>())
                                            await AddExampleAndWordLinksFromResAsync(ex, "example_kun:" + (kv.Key ?? "kun"));
                                }
                            } // foreach result

                            // sanitize strings & truncate
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

                            await _db.SaveChangesAsync(cancellationToken);
                            await tx.CommitAsync(cancellationToken);
                            _logger.LogInformation("Rehydrated Entry.Id={id} (committed)", entry.Id);
                        }
                        catch (Exception exInner)
                        {
                            _logger.LogError(exInner, "Failed to rehydrate Entry.Id={id} - rolling back", entry.Id);
                            await tx.RollbackAsync(cancellationToken);
                            throw;
                        }
                    });

                    succeeded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error rehydrating Entry.Id={id}", entry.Id);
                    failed++;
                }
            } // foreach candidate

            _logger.LogInformation("RehydrateExamplesFromRawJsonAsync finished. processed={p} succeeded={s} failed={f}",
                processed, succeeded, failed);
        }


        // ---------- SaveResultToDatabaseInternalAsync (kept similar to before) ----------
        private async Task SaveResultToDatabaseInternalAsync(KanjiResult res, CancellationToken cancellationToken)
        {
            if (res == null || string.IsNullOrWhiteSpace(res.Kanji) || !IsLikelyKanji(res.Kanji)) return;

            var viLang = await GetOrCreateLanguageAsync("vi", "Vietnamese", cancellationToken);

            var kanjiEntity = await _db.Kanji.FirstOrDefaultAsync(k => k.Character == res.Kanji, cancellationToken);
            if (kanjiEntity == null)
            {
                kanjiEntity = new Kanji
                {
                    Character = res.Kanji,
                    CreatedAt = DateTime.UtcNow,
                    JlptLevel = string.Empty,
                    Meaning = string.Empty
                };
                _db.Kanji.Add(kanjiEntity);
            }

            if (int.TryParse(res.StrokeCount, out var stroke))
                kanjiEntity.StrokeCount = stroke;

            if (res.Freq.HasValue) kanjiEntity.Freq = res.Freq.Value;

            if (!string.IsNullOrWhiteSpace(res.Mean))
                kanjiEntity.Meaning = res.Mean.Trim();
            else
                kanjiEntity.Meaning = kanjiEntity.Meaning ?? string.Empty;

            kanjiEntity.JlptLevel = (res.Level != null && res.Level.Any())
                ? string.Join(",", res.Level.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()))
                : (kanjiEntity.JlptLevel ?? string.Empty);

            kanjiEntity.UpdatedAt = DateTime.UtcNow;

            Entry entry = null;
            if (res.MobileId.HasValue)
            {
                entry = await _db.Entries.FirstOrDefaultAsync(e => e.EntSeq == res.MobileId.Value, cancellationToken);
            }

            if (entry == null)
            {
                entry = await _db.Entries
                    .FirstOrDefaultAsync(e => e.Type == "kanji" && e.RawJson != null && EF.Functions.Like(e.RawJson, $"%\"kanji\"%{res.Kanji}%"), cancellationToken);
            }

            if (entry == null)
            {
                entry = new Entry
                {
                    Type = "kanji",
                    Label = !string.IsNullOrWhiteSpace(res.Label) ? res.Label.Trim() : "ja_vi",
                    RawJson = JsonSerializer.Serialize(res, _jsonOptions),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                if (res.MobileId.HasValue) entry.EntSeq = res.MobileId.Value;
                _db.Entries.Add(entry);
            }
            else
            {
                entry.RawJson = JsonSerializer.Serialize(res, _jsonOptions);
                entry.Label = !string.IsNullOrWhiteSpace(res.Label) ? res.Label.Trim() : (entry.Label ?? "ja_vi");
                entry.UpdatedAt = DateTime.UtcNow;
                if (res.MobileId.HasValue) entry.EntSeq = res.MobileId.Value;
            }

            // create sense
            var sense = new Sense
            {
                Entry = entry,
                Pos = string.Empty,
                Field = string.Empty,
                Misc = string.Empty,
                SInf = res.Detail ?? string.Empty,
                Dialect = string.Empty,
                SenseOrder = 1
            };
            _db.Senses.Add(sense);

            if (!string.IsNullOrWhiteSpace(res.Mean))
            {
                _db.Glosses.Add(new Gloss
                {
                    Sense = sense,
                    Language = viLang,
                    Text = res.Mean.Trim(),
                    GType = string.Empty,
                    GGend = string.Empty,
                    Priority = string.Empty
                });
            }

            // add examples if present (mirror rehydrate logic)
            async Task AddExampleAndWordLinksAsync(ExampleItem ex, string sourceTag)
            {
                if (ex == null) return;
                if (string.IsNullOrWhiteSpace(ex.W) && string.IsNullOrWhiteSpace(ex.M)) return;

                var example = new Example
                {
                    Sense = sense,
                    ContentJp = string.IsNullOrWhiteSpace(ex.W) ? string.Empty : ex.W.Trim(),
                    ContentTranslated = string.IsNullOrWhiteSpace(ex.M) ? string.Empty : ex.M.Trim(),
                    Transcription = string.IsNullOrWhiteSpace(ex.P) ? string.Empty : ex.P.Trim(),
                    SourceRef = "mazii:" + sourceTag
                };
                _db.Examples.Add(example);

                if (!string.IsNullOrWhiteSpace(ex.W))
                {
                    var wt = ex.W.Trim();
                    // Word insert / mapping logic same as before
                    var word = await _db.Words.FirstOrDefaultAsync(w => w.WordText == wt, cancellationToken);
                    if (word == null)
                    {
                        word = new Word
                        {
                            WordText = wt,
                            Phonetic = string.IsNullOrWhiteSpace(ex.P) ? string.Empty : ex.P.Trim(),
                            ShortMean = string.IsNullOrWhiteSpace(ex.M) ? string.Empty : ex.M.Trim(),
                            Romaji = string.Empty,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Words.Add(word);
                    }

                    // WordToEntry & WordKanji
                    _db.WordToEntries.Add(new WordToEntry
                    {
                        Word = word,
                        Entry = entry,
                        MappingType = "example"
                    });
                    _db.WordKanji.Add(new WordKanji
                    {
                        Word = word,
                        Kanji = kanjiEntity
                    });
                }
            }

            if (res.Examples != null)
            {
                foreach (var ex in res.Examples)
                    await AddExampleAndWordLinksAsync(ex, "examples");
            }
            if (res.ExampleOn != null)
            {
                foreach (var kv in res.ExampleOn)
                    foreach (var ex in kv.Value ?? Enumerable.Empty<ExampleItem>())
                        await AddExampleAndWordLinksAsync(ex, "example_on:" + kv.Key);
            }
            if (res.ExampleKun != null)
            {
                foreach (var kv in res.ExampleKun)
                    foreach (var ex in kv.Value ?? Enumerable.Empty<ExampleItem>())
                        await AddExampleAndWordLinksAsync(ex, "example_kun:" + kv.Key);
            }
        }

        // ---------- DTOs ----------
        public class MaziiKanjiResponse
        {
            [JsonPropertyName("status")]
            public int Status { get; set; }
            [JsonPropertyName("results")]
            public List<KanjiResult> Results { get; set; }
            [JsonPropertyName("total")]
            public int Total { get; set; }
        }

        public class KanjiResult
        {
            [JsonPropertyName("label")] public string Label { get; set; }
            [JsonPropertyName("kanji")] public string Kanji { get; set; }
            [JsonPropertyName("on")] public string On { get; set; }
            [JsonPropertyName("freq")] public int? Freq { get; set; }
            [JsonPropertyName("example_kun")] public Dictionary<string, List<ExampleItem>> ExampleKun { get; set; }
            [JsonPropertyName("example_on")] public Dictionary<string, List<ExampleItem>> ExampleOn { get; set; }
            [JsonPropertyName("image")] public string Image { get; set; }
            [JsonPropertyName("stroke_count")] public string StrokeCount { get; set; }
            [JsonPropertyName("mobileId")] public int? MobileId { get; set; }
            [JsonPropertyName("compDetail")] public List<CompDetailItem> CompDetail { get; set; }
            [JsonPropertyName("tips")] public Dictionary<string, string> Tips { get; set; }
            [JsonPropertyName("mean")] public string Mean { get; set; }
            [JsonPropertyName("kun")] public string Kun { get; set; }
            [JsonPropertyName("writing")] public string Writing { get; set; }
            [JsonPropertyName("detail")] public string Detail { get; set; }
            [JsonPropertyName("level")] public List<string> Level { get; set; }
            [JsonPropertyName("examples")] public List<ExampleItem> Examples { get; set; }
        }

        public class ExampleItem
        {
            [JsonPropertyName("w")] public string W { get; set; }
            [JsonPropertyName("m")] public string M { get; set; }
            [JsonPropertyName("p")] public string P { get; set; }
            [JsonPropertyName("h")] public string H { get; set; }
        }

        public class CompDetailItem
        {
            [JsonPropertyName("w")] public string W { get; set; }
            [JsonPropertyName("h")] public string H { get; set; }
        }

        // ---------- Helpers for robust parsing ----------
        private static List<KanjiResult> RobustParseResults(string rawJson, JsonSerializerOptions opts, out string log)
        {
            log = string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson)) { log = "rawJson empty"; return null; }

            // try direct deserialize single object
            try
            {
                var single = JsonSerializer.Deserialize<KanjiResult>(rawJson, opts);
                if (single != null && !string.IsNullOrWhiteSpace(single.Kanji))
                {
                    log = "parsed KanjiResult directly";
                    return new List<KanjiResult> { single };
                }
            }
            catch (Exception ex)
            {
                log += "direct single failed; ";
            }

            // try deserialize list of KanjiResult (array)
            try
            {
                var list = JsonSerializer.Deserialize<List<KanjiResult>>(rawJson, opts);
                if (list != null && list.Count > 0 && list.Any(x => !string.IsNullOrWhiteSpace(x.Kanji)))
                {
                    log += "parsed List<KanjiResult> directly; ";
                    return list.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList();
                }
            }
            catch
            {
                log += "list deserialize failed; ";
            }

            // try wrapper object MaziiKanjiResponse
            try
            {
                var wrap = JsonSerializer.Deserialize<MaziiKanjiResponse>(rawJson, opts);
                if (wrap != null && wrap.Results != null && wrap.Results.Count > 0)
                {
                    log += "parsed wrapper directly";
                    return wrap.Results.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList();
                }
            }
            catch
            {
                log += "direct wrapper failed; ";
            }

            // try unescape once
            try
            {
                var once = Regex.Unescape(rawJson);
                var single = JsonSerializer.Deserialize<KanjiResult>(once, opts);
                if (single != null && !string.IsNullOrWhiteSpace(single.Kanji))
                {
                    log += "parsed single after 1 unescape";
                    return new List<KanjiResult> { single };
                }

                var wrap = JsonSerializer.Deserialize<MaziiKanjiResponse>(once, opts);
                if (wrap != null && wrap.Results != null && wrap.Results.Count > 0)
                {
                    log += "parsed wrapper after 1 unescape";
                    return wrap.Results.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList();
                }

                var list = JsonSerializer.Deserialize<List<KanjiResult>>(once, opts);
                if (list != null && list.Count > 0)
                {
                    log += "parsed list after 1 unescape";
                    return list.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList();
                }
            }
            catch
            {
                log += "unescape1 failed; ";
            }

            // try unescape twice
            try
            {
                var twice = Regex.Unescape(Regex.Unescape(rawJson));
                var single = JsonSerializer.Deserialize<KanjiResult>(twice, opts);
                if (single != null && !string.IsNullOrWhiteSpace(single.Kanji))
                {
                    log += "parsed single after 2 unescape";
                    return new List<KanjiResult> { single };
                }

                var wrap = JsonSerializer.Deserialize<MaziiKanjiResponse>(twice, opts);
                if (wrap != null && wrap.Results != null && wrap.Results.Count > 0)
                {
                    log += "parsed wrapper after 2 unescape";
                    return wrap.Results.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList();
                }

                var list = JsonSerializer.Deserialize<List<KanjiResult>>(twice, opts);
                if (list != null && list.Count > 0)
                {
                    log += "parsed list after 2 unescape";
                    return list.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList();
                }
            }
            catch
            {
                log += "unescape2 failed;";
            }

            // final heuristic: try to locate a JSON substring that looks like {"label":...,"kanji":...} or any { ... "kanji":"X" ... }
            try
            {
                var m = Regex.Match(rawJson, @"\{[^}]*?""kanji""\s*:\s*""(?<k>[^""]+)""[^}]*\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var token = m.Value;
                    try
                    {
                        var single = JsonSerializer.Deserialize<KanjiResult>(token, opts);
                        if (single != null && !string.IsNullOrWhiteSpace(single.Kanji))
                        {
                            log += "heuristic parsed token to KanjiResult";
                            return new List<KanjiResult> { single };
                        }
                    }
                    catch
                    {
                        var kanji = m.Groups["k"].Value;
                        var single = new KanjiResult { Kanji = kanji, Label = "ja_vi" };
                        log += "heuristic extracted kanji token";
                        return new List<KanjiResult> { single };
                    }
                }
            }
            catch
            {
                // ignore
            }

            log += "all parse attempts failed";
            return null;
        }

        /// <summary>
        /// Robust kanji extraction:
        /// - handles quoted JSON strings (double-serialized)
        /// - tries parse, unescape once and twice
        /// - recursively inspects JSON objects/arrays for "kanji" properties and any string containing CJK characters
        /// - fallback: scans for literal CJK or \uXXXX sequences and decodes them
        /// </summary>
        private static bool TryExtractKanjiFromJson(string raw, out List<string> kanjis)
        {
            kanjis = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // safe parse helper
            JsonDocument TryParseDoc(string s)
            {
                try { return JsonDocument.Parse(s); }
                catch { return null; }
            }

            // collect CJK from a plain string (including attempts to unescape)
            void DecodeUnicodeEscapesAndCollect(string s, HashSet<string> dst)
            {
                if (string.IsNullOrEmpty(s)) return;

                // 1) if the string already has literal CJK, collect them
                var m = CjkRegex.Matches(s);
                foreach (Match mm in m) dst.Add(mm.Value);

                // 2) if none found, try Regex.Unescape (handles \\uXXXX -> \uXXXX -> actual char)
                if (dst.Count == 0)
                {
                    try
                    {
                        var un = Regex.Unescape(s);
                        var m2 = CjkRegex.Matches(un);
                        foreach (Match mm in m2) dst.Add(mm.Value);

                        // 3) if still none, look for explicit \uXXXX sequences and decode
                        if (dst.Count == 0)
                        {
                            foreach (Match esc in Regex.Matches(un, @"\\u(?<h>[0-9A-Fa-f]{4})"))
                            {
                                var hex = esc.Groups["h"].Value;
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code))
                                {
                                    dst.Add(char.ConvertFromUtf32(code));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignore unescape errors
                    }
                }
            }

            // recursive traversal of JsonElement to find kanji props and strings with CJK
            void ExtractFromElement(JsonElement el, HashSet<string> dst)
            {
                switch (el.ValueKind)
                {
                    case JsonValueKind.Object:
                        foreach (var prop in el.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, "kanji", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
                            {
                                var s = prop.Value.GetString();
                                DecodeUnicodeEscapesAndCollect(s, dst);
                            }
                            else
                            {
                                ExtractFromElement(prop.Value, dst);
                            }
                        }
                        break;

                    case JsonValueKind.Array:
                        foreach (var item in el.EnumerateArray()) ExtractFromElement(item, dst);
                        break;

                    case JsonValueKind.String:
                        {
                            var str = el.GetString();
                            DecodeUnicodeEscapesAndCollect(str, dst);
                        }
                        break;

                    default:
                        break;
                }
            }

            // 1) If raw is quoted JSON string, unquote once
            string attempt = raw;
            if (attempt.Length > 1 && attempt[0] == '"' && attempt[attempt.Length - 1] == '"')
            {
                try
                {
                    attempt = JsonSerializer.Deserialize<string>(attempt) ?? attempt;
                }
                catch
                {
                    // ignore - keep original
                }
            }

            // 2) try parse attempt, then unescape once and twice
            JsonDocument doc = TryParseDoc(attempt)
                               ?? TryParseDoc(Regex.Unescape(attempt))
                               ?? TryParseDoc(Regex.Unescape(Regex.Unescape(attempt)));

            var found = new HashSet<string>(StringComparer.Ordinal);
            if (doc != null)
            {
                try
                {
                    ExtractFromElement(doc.RootElement, found);
                    kanjis = found.Distinct().ToList();
                    return true; // parsed successfully (maybe empty)
                }
                finally
                {
                    doc.Dispose();
                }
            }

            // 3) fallback: scan raw for CJK or \uXXXX sequences
            try
            {
                DecodeUnicodeEscapesAndCollect(raw, found);
                if (found.Count > 0)
                {
                    kanjis = found.Distinct().ToList();
                    return true;
                }
            }
            catch { /* ignore */ }

            return false;
        }

        private static bool TryExtractKanjiListFromRawJson(string rawJson, out List<string> kanjis)
        {
            return TryExtractKanjiFromJson(rawJson, out kanjis);
        }

        // ---------- New: high-level normalizer that tries many strategies ----------
        private List<KanjiResult> NormalizeRawJsonToKanjiResults(string rawJson, out string parseLog)
        {
            parseLog = string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson)) { parseLog = "raw empty"; return null; }

            // 1) Pre-extract JSON inside HTML/script if present
            try
            {
                var extracted = ExtractJsonFromHtmlIfPresent(rawJson);
                if (!string.Equals(extracted, rawJson, StringComparison.Ordinal))
                {
                    rawJson = extracted;
                    parseLog += "htmlStripped;";
                }
            }
            catch { /* ignore */ }

            // 2) Try RobustParseResults (handles single/wrapper/list & unescape attempts)
            try
            {
                var r = RobustParseResults(rawJson, _jsonOptions, out var rl);
                parseLog += "robust:" + rl + ";";
                if (r != null && r.Count > 0) return r;
            }
            catch (Exception ex)
            {
                parseLog += "robustEx:" + ex.GetBaseException().Message + ";";
            }

            // 3) Try explicit List<KanjiResult> deserialize again with unescape attempts
            try
            {
                var once = Regex.Unescape(rawJson);
                var list = JsonSerializer.Deserialize<List<KanjiResult>>(once, _jsonOptions);
                if (list != null && list.Count > 0) { parseLog += "listAfterUnescape;"; return list.Where(x => !string.IsNullOrWhiteSpace(x.Kanji)).ToList(); }
            }
            catch { parseLog += "listAfterUnescapeFailed;"; }

            // 4) Try to extract simple Kanji chars (TryExtractKanjiFromJson)
            try
            {
                if (TryExtractKanjiFromJson(rawJson, out var chars) && chars != null && chars.Count > 0)
                {
                    parseLog += "extractedChars:" + string.Join(',', chars.Take(10)) + ";";
                    return chars.Distinct().Select(ch => new KanjiResult { Kanji = ch, Label = "ja_vi" }).ToList();
                }
                parseLog += "extractedChars:none;";
            }
            catch (Exception ex)
            {
                parseLog += "extractCharsEx:" + ex.GetBaseException().Message + ";";
            }

            // 5) Try decode \uXXXX sequences directly
            try
            {
                var escMatches = Regex.Matches(rawJson, @"\\u(?<h>[0-9A-Fa-f]{4})");
                if (escMatches.Count > 0)
                {
                    var list = new List<string>();
                    foreach (Match mm in escMatches)
                    {
                        if (int.TryParse(mm.Groups["h"].Value, System.Globalization.NumberStyles.HexNumber, null, out var code))
                        {
                            list.Add(char.ConvertFromUtf32(code));
                        }
                    }
                    if (list.Count > 0)
                    {
                        parseLog += "decodedEscapes:" + string.Join(',', list.Take(10)) + ";";
                        return list.Distinct().Select(ch => new KanjiResult { Kanji = ch, Label = "ja_vi" }).ToList();
                    }
                }
                parseLog += "decodedEscapes:none;";
            }
            catch (Exception ex)
            {
                parseLog += "decodedEscapesEx:" + ex.GetBaseException().Message + ";";
            }

            // 6) Final fallback: scan raw for literal CJK characters and return them
            try
            {
                var m2 = CjkRegex.Matches(rawJson);
                if (m2.Count > 0)
                {
                    var chars = m2.Cast<Match>().Select(mm => mm.Value).Distinct().ToList();
                    parseLog += "cjkScan:" + string.Join(',', chars.Take(10)) + ";";
                    return chars.Select(c => new KanjiResult { Kanji = c, Label = "ja_vi" }).ToList();
                }
                parseLog += "cjkScan:none;";
            }
            catch (Exception ex)
            {
                parseLog += "cjkScanEx:" + ex.GetBaseException().Message + ";";
            }

            parseLog += "allAttemptsFailed";
            return null;
        }

        // extract JSON that might be wrapped in HTML/script tags or mixed with text
        private static string ExtractJsonFromHtmlIfPresent(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            // try to locate <script> blocks that contain JSON-like content
            var scriptMatch = Regex.Match(raw, @"<script\b[^>]*>(?<c>.*?\{.*?\}.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (scriptMatch.Success)
            {
                return scriptMatch.Groups["c"].Value;
            }

            // try to find first balanced { ... } or [ ... ] substring (naive but useful)
            var braceMatch = Regex.Match(raw, @"(\{[\s\S]*\}|\[[\s\S]*\])", RegexOptions.Singleline);
            if (braceMatch.Success)
                return braceMatch.Value;

            return raw;
        }

        // helper: convert kanji char(s) to \uXXXX sequence(s) (uppercase hex)
        private static string ToJsonUnicodeEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder();
            foreach (var ch in s)
                sb.Append("\\u").Append(((int)ch).ToString("X4")); // uppercase hex
            return sb.ToString();
        }

        // validate token looks like kanji (has at least one CJK char)
        private static bool IsLikelyKanji(string s)
        {
            return !string.IsNullOrWhiteSpace(s) && CjkRegex.IsMatch(s);
        }

        // ---------- GetOrCreateLanguageAsync (same as before) ----------
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
