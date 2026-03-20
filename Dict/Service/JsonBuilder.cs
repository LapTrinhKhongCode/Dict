using Dict.Data;
using Dict.Models;
using Dict.Models.JsonModels; // Đảm bảo bạn có namespace này
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // Thêm using này
using System.Threading.Tasks;

namespace Dict.Service
{
    public class JsonBuilder : IJsonBuilderService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<JsonBuilder> _logger;
        private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS"; // Collation phân biệt Kana

        public JsonBuilder(ApplicationDbContext db, ILogger<JsonBuilder> logger)
        {
            _db = db;
            _logger = logger;
        }

        #region --- 1. XỬ LÝ TÌM KIẾM TỪ VỰNG (Word) ---

        /// <summary>
        /// "Bộ não" tìm kiếm chính cho Từ vựng (Word), triển khai thuật toán 3 ưu tiên.
        /// </summary>
        public async Task<string> RebuildJsonForWordAsync(string term)
        {
            var data = new Dict.Models.JsonModels.Data(); // Model JSON
            List<Entry> mainEntries = new List<Entry>();
            List<Entry> suggestionEntries;

            // === ƯU TIÊN 1: TÌM "TRÙNG KHỚP TUYỆT ĐỐI" (LABEL) ===
            var exactLabelMatch = await _db.Entries
                .AsNoTrackingWithIdentityResolution()
                .Where(e => EF.Functions.Collate(e.Label, _sensitiveCollation) == term && e.Type == "word")
                .IncludeWordData()
                .FirstOrDefaultAsync();

            if (exactLabelMatch != null)
            {
                // Nếu tìm thấy exact label, dùng luôn bản đó (đã include đầy đủ)
                mainEntries.Add(exactLabelMatch);
                suggestionEntries = await GetSuggestionEntriesAsync(term, 20, new List<int> { exactLabelMatch.Id });

                // dedupe suggestions by surface form (WordText+Phonetic)
                suggestionEntries = suggestionEntries
                    .GroupBy(e => (WordText: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.WordText ?? e.Label ?? "",
                                  Phonetic: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.Phonetic ?? ""))
                    .Select(g => SelectPreferredEntry(g))
                    .ToList();
            }
            else
            {
                // === ƯU TIÊN 2: TÌM THEO "CÁCH ĐỌC" (PHONETIC) ===
                var homophoneMatches = await _db.Entries
                    .AsNoTrackingWithIdentityResolution()
                    .Where(e => e.Words.Any(w => EF.Functions.Collate(w.Phonetic, _sensitiveCollation) == term) && e.Type == "word")
                    .OrderBy(e => e.Words.OrderBy(w => w.Id).Select(w => w.Weight).FirstOrDefault())
                    .IncludeWordData()
                    .ToListAsync();

                LogEntryChildrenCounts(homophoneMatches, "homophoneMatches after load");

                if (homophoneMatches.Any())
                {
                    // Dedupe theo surface form: WordText + Phonetic
                    var deduped = homophoneMatches
                        .GroupBy(e => (WordText: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.WordText ?? e.Label ?? "",
                                      Phonetic: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.Phonetic ?? ""))
                        .Select(g => SelectPreferredEntry(g))
                        .ToList();

                    mainEntries = deduped;
                    var homophoneIds = mainEntries.Select(e => e.Id).ToList();
                    suggestionEntries = await GetSuggestionEntriesAsync(term, 20, homophoneIds);

                    // dedupe suggestions as well
                    suggestionEntries = suggestionEntries
                        .GroupBy(e => (WordText: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.WordText ?? e.Label ?? "",
                                      Phonetic: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.Phonetic ?? ""))
                        .Select(g => SelectPreferredEntry(g))
                        .ToList();
                }
                else
                {
                    // === ƯU TIÊN 3: GỢI Ý CHUNG - KHÔNG CÓ KẾT QUẢ CHÍNH ===
                    mainEntries = new List<Entry>(); // Rỗng
                    suggestionEntries = await GetSuggestionEntriesAsync(term, 20);

                    // dedupe suggestions
                    suggestionEntries = suggestionEntries
                        .GroupBy(e => (WordText: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.WordText ?? e.Label ?? "",
                                      Phonetic: e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.Phonetic ?? ""))
                        .Select(g => SelectPreferredEntry(g))
                        .ToList();
                }
            }

            // --- ÁNH XẠ VÀ ĐÓNG GÓI ---

            // 1. Lấy danh sách Kanji ID cần thiết từ TẤT CẢ các kết quả chính
            var allKanjiChars = mainEntries
                .SelectMany(e => e.Words.OrderBy(w => w.Id).FirstOrDefault()?.WordKanji ?? new List<WordKanji>())
                .Select(wk => wk.Kanji?.Character)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            // (không cần group by Id nữa vì đã dedupe theo surface form)
            // 3. Map kết quả chính (sử dụng entry đã được chọn)
            var mappedList = mainEntries.Select(entry => MapEntryToJsonWord(entry))
                                       .Where(w => w != null)
                                       .Cast<Dict.Models.JsonModels.Word>()
                                       .ToList();

            // Debug: kiểm tra duplicate theo WordText+Phonetic (nên không còn)
            var duplicateGroups = mappedList
                .GroupBy(w => (w.WordText ?? "", w.Phonetic ?? ""))
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateGroups.Any())
            {
                foreach (var g in duplicateGroups)
                {
                    _logger.LogInformation("Duplicate form detected AFTER dedupe: WordText='{WordText}', Phonetic='{Phonetic}', Count={Count}, EntryIds={Ids}",
                        g.Key.Item1, g.Key.Item2, g.Count(), string.Join(',', g.SelectMany(x => mainEntries.Where(e =>
                            (e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.WordText ?? e.Label ?? "") == g.Key.Item1 &&
                            (e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.Phonetic ?? "") == g.Key.Item2).Select(e => e.Id))));
                }
            }
            else
            {
                _logger.LogInformation("No duplicate WordText+Phonetic groups found after dedupe+mapping.");
            }

            // 4. Map gợi ý
            data.SuggestWords = suggestionEntries.Select(MapEntryToSuggestWord)
                                                .Where(s => s != null)
                                                .ToList()!;

            // Set data.Words từ mappedList
            data.Words = mappedList;

            // Sau lấy mainEntries:
            var ids = mainEntries.Select(e => e.Id).ToList();
            _logger.LogInformation("mainEntries count={Count}, distinct={Distinct}", ids.Count, ids.Distinct().Count());

            // Sau lấy suggestionEntries:
            var sids = suggestionEntries.Select(e => e.Id).ToList();
            _logger.LogInformation("suggestEntries count={Count}, distinct={Distinct}", sids.Count, sids.Distinct().Count());

            var rootObject = new RootObject { Status = 200, Data = data };

            return JsonConvert.SerializeObject(rootObject, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }


        #endregion

        // -----------------------------------------------------------------

        #region --- 2. XỬ LÝ BUILD JSON KANJI ---

        /// <summary>
        /// Truy vấn và xây dựng chuỗi JSON hoàn chỉnh cho một Hán tự.
        /// </summary>
        public async Task<string> RebuildJsonForKanjiAsync(string kanjiTerm)
        {
            var kanjiChars = ExtractKanji(kanjiTerm);

            if (!kanjiChars.Any())
            {
                return JsonConvert.SerializeObject(new { status = 404, error = "No valid Kanji characters found in term" });
            }

            // Bước 2: Chạy các truy vấn tuần tự (ĐÃ SỬA LỖI ĐA LUỒNG)

            // Truy vấn 1: Lấy TẤT CẢ các Entry (Kanji) liên quan
            var entryFromDbList = await _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "kanji" && kanjiChars.Contains(e.Label)) // Dùng Contains
                .Include(e => e.ReadingElements)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses)
                .AsSplitQuery()
                .ToListAsync(); // Dùng ToListAsync

            // Truy vấn 2: Lấy TẤT CẢ các Kanji liên quan
            var kanjiFromDbList = await _db.Kanji
                .AsNoTracking()
                .Where(k => kanjiChars.Contains(k.Character))
                .Include(k => k.KanjiExamples.OrderBy(ex => ex.Id))
                .ToListAsync(); // Dùng ToListAsync

            // Bước 3: Kiểm tra kết quả
            if (!entryFromDbList.Any() || !kanjiFromDbList.Any())
            {
                // (Chốt chặn cuối cùng: thử tìm RawJson nếu chỉ có 1 ký tự)
                if (kanjiChars.Length == 1)
                {
                    var rawJsonEntry = await _db.Entries
                        .AsNoTracking()
                        .Where(e => e.Type == "kanji" && e.Label == kanjiChars[0].ToString())
                        .Select(e => e.RawJson)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrEmpty(rawJsonEntry)) return rawJsonEntry;
                }
                return JsonConvert.SerializeObject(new { status = 404, error = "Kanji data not found in database" });
            }

            // Bước 4: Ánh xạ (Map) từng Hán tự sang Model JSON
            var resultsList = new List<KanjiResult>();
            foreach (var charToFind in kanjiChars)
            {
                var entry = entryFromDbList.FirstOrDefault(e => e.Label == charToFind.ToString());
                var kanji = kanjiFromDbList.FirstOrDefault(k => k.Character == charToFind.ToString());

                if (entry != null && kanji != null)
                {
                    resultsList.Add(MapKanjiDataToJson(entry, kanji)); // Gọi hàm helper
                }
            }

            var rootObject = new KanjiRootObject
            {
                Results = resultsList, // Gán danh sách kết quả
                Total = 10000
            };

            return JsonConvert.SerializeObject(rootObject, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }

        #endregion

        // -----------------------------------------------------------------

        #region --- CÁC PHƯƠNG THỨC HELPER (ÁNH XẠ VÀ GỢI Ý) ---

        /// <summary>
        /// (Helper cho WORD) Tìm các từ gợi ý
        /// </summary>
        private async Task<List<Entry>> GetSuggestionEntriesAsync(string term, int limit, List<int>? excludeEntryIds = null)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new List<Entry>();

            var sensitiveCollation = "Japanese_CS_AS_KS_WS";
            string kanjiPart = ExtractKanji(term);
            bool hasKanjiPart = !string.IsNullOrEmpty(kanjiPart);

            // Candidates base (no include, chỉ select Id + computed Score): STEP 1
            var baseQuery = _db.Entries
                .AsNoTracking() // chỉ để lấy id + score, no tracking is ok here
                .Where(e => e.Type == "word" && (
                    e.Label.Contains(term) ||
                    e.Phonetic.Contains(term) ||
                    (hasKanjiPart && e.Label.Contains(kanjiPart))
                ));

            if (excludeEntryIds != null && excludeEntryIds.Any())
                baseQuery = baseQuery.Where(e => !excludeEntryIds.Contains(e.Id));

            // compute score in SQL projection
            var scoredIds = await baseQuery
                .Select(e => new
                {
                    e.Id,
                    Score =
                        (EF.Functions.Collate(e.Label, sensitiveCollation) == term ? 100 : 0) +
                        (e.Words.Any() && EF.Functions.Collate(e.Words.OrderBy(w => w.Id).Select(w => w.Phonetic).FirstOrDefault(), sensitiveCollation) == term ? 90 : 0) +
                        (e.Label.StartsWith(term) ? 50 : 0) +
                        (e.Phonetic.StartsWith(term) ? 40 : 0) +
                        (hasKanjiPart && e.Label.Contains(kanjiPart) ? 20 : 0) +
                        (e.Label.Contains(term) ? 10 : 0)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Id) // tie-breaker, giữ đơn giản
                .Take(limit)
                .Select(x => x.Id)
                .ToListAsync();

            if (!scoredIds.Any())
                return new List<Entry>();

            // STEP 2: load full entries + includes for those ids, with identity resolution (no tracking but resolve identity)
            var entries = await _db.Entries
                .AsNoTrackingWithIdentityResolution()
                .Where(e => scoredIds.Contains(e.Id))
                .Include(e => e.Words)
                    .ThenInclude(w => w.Relations)
                        .ThenInclude(r => r.RelatedWord)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Examples)
                .Include(e => e.Media.Where(m => m.MediaType == "image"))
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.SynsetEntries).ThenInclude(se => se.SynonymItems)
                .Include(e => e.Words).ThenInclude(w => w.WordKanji).ThenInclude(wk => wk.Kanji).ThenInclude(k => k.KanjiExamples)
                .AsSplitQuery()
                .ToListAsync();
            entries = entries.GroupBy(e => e.Id).Select(g => g.First()).ToList();
            LogEntryChildrenCounts(entries, "SuggestionEntries after load");
            // preserve original ranking order
            var ordered = scoredIds
                .Select(id => entries.FirstOrDefault(e => e.Id == id))
                .Where(e => e != null)
                .ToList()!;

            return ordered;
        }

        // (Hàm ExtractKanji giữ nguyên)
        private string ExtractKanji(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            // Dải Unicode U+4E00 đến U+9FAF là dải Kanji phổ biến (CJK Unified Ideographs)
            return Regex.Replace(text, @"[^\u4E00-\u9FAF]", "");
        }

        /// <summary>
        /// (Helper cho WORD) Ánh xạ Entry sang JSON Word đầy đủ
        /// </summary>
        private Dict.Models.JsonModels.Word? MapEntryToJsonWord(Entry entryFromDb)
        {
            var mainWordRecord = entryFromDb.Words?.OrderBy(w => w.Id).FirstOrDefault();
            if (mainWordRecord == null) return null;

            var wordObject = new Dict.Models.JsonModels.Word
            {
                IdFromSource = entryFromDb.MobileId?.ToString() ?? Guid.NewGuid().ToString("N"),
                WordText = mainWordRecord.WordText,
                Phonetic = mainWordRecord.Phonetic,
                ShortMean = mainWordRecord.ShortMean,
                Weight = mainWordRecord.Weight,
                MobileId = mainWordRecord.MobileId,
                Images = entryFromDb.Media?.Select(m => m.Url).ToList() ?? new List<string>(),
                OppositeWord = mainWordRecord.Relations?.Where(r => r.RelationType == "opposite" && r.RelatedWord != null).Select(r => r.RelatedWord.WordText ?? "").ToList() ?? new List<string>()
            };

            if (entryFromDb.Senses != null)
            {
                foreach (var senseFromDb in entryFromDb.Senses.OrderBy(s => s.SenseOrder))
                {
                    wordObject.Means.Add(new Dict.Models.JsonModels.Mean
                    {
                        MeanText = senseFromDb.Glosses?.FirstOrDefault()?.Text ?? "",
                        Kind = senseFromDb.Pos ?? "",
                        Examples = senseFromDb.Examples?.Select(ex => new Dict.Models.JsonModels.Example
                        {
                            Content = ex.ContentJp ?? "",
                            Mean = ex.ContentTranslated ?? "",
                            Transcription = ex.Transcription ?? ""
                        }).ToList() ?? new List<Dict.Models.JsonModels.Example>()
                    });
                }

                var firstSense = entryFromDb.Senses.OrderBy(s => s.SenseOrder).FirstOrDefault();
                if (firstSense != null && firstSense.SynsetEntries?.Any() == true)
                {
                    var synsetObject = new Dict.Models.JsonModels.Synset
                    {
                        BaseForm = firstSense.SynsetEntries.First().BaseForm ?? "",
                        Pos = firstSense.SynsetEntries.First().Pos ?? ""
                    };
                    foreach (var entryGroup in firstSense.SynsetEntries)
                    {
                        synsetObject.Entry.Add(new SynonymEntry
                        {
                            Synonym = entryGroup.SynonymItems?.Select(si => si.Word ?? "").ToList() ?? new List<string>(),
                            DefinitionId = entryGroup.DefinitionId ?? ""
                        });
                    }
                    wordObject.Synsets.Add(synsetObject);
                }
            }

            if (!string.IsNullOrEmpty(mainWordRecord.Romaji) || !string.IsNullOrEmpty(mainWordRecord.Phonetic))
            {
                wordObject.Pronunciation.Add(new Dict.Models.JsonModels.Pronunciation
                {
                    Word = mainWordRecord.WordText ?? "",
                    Type = "regular",
                    Transcriptions = new List<Transcription>
                     {
                         new Transcription { Romaji = mainWordRecord.Romaji ?? "", Kana = mainWordRecord.Phonetic ?? "" }
                     }
                });
            }

            return wordObject;
        }

        /// <summary>
        /// (Helper cho WORD) Ánh xạ Entry sang JSON SuggestWord nhẹ
        /// </summary>
        private SuggestWord? MapEntryToSuggestWord(Entry entry)
        {
            var wordRecord = entry.Words?.FirstOrDefault();
            if (wordRecord == null) return null;
            return new SuggestWord
            {
                IdFromSource = entry.MobileId?.ToString() ?? entry.Id.ToString(),
                WordText = wordRecord.WordText ?? "",
                Phonetic = wordRecord.Phonetic ?? "",
                ShortMean = wordRecord.ShortMean ?? "",
                MobileId = wordRecord.MobileId,
                Means = entry.Senses?
                            .OrderBy(s => s.SenseOrder)
                            .Select(s => new Dict.Models.JsonModels.Mean
                            {
                                MeanText = s.Glosses?.FirstOrDefault()?.Text ?? "",
                                Kind = s.Pos ?? "",
                                Examples = new List<Dict.Models.JsonModels.Example>()
                            }).ToList() ?? new List<Dict.Models.JsonModels.Mean>()
            };
        }


        /// <summary>
        /// (Helper cho KANJI) Ánh xạ Entry và Kanji sang JSON Kanji đầy đủ
        /// </summary>
        private KanjiResult MapKanjiDataToJson(Entry? entry, Kanji kanji)
        {
            var kanjiResult = new KanjiResult
            {
                Kanji = kanji.Character,
                Stroke_count = kanji.StrokeCount,
                Freq = kanji.Freq,
                //MobileId = kanji.MobileId,
                Level = new List<string> { kanji.JlptLevel ?? "" }
            };

            // Chỉ lấy cách đọc và nghĩa NẾU Entry (Kanji) được cung cấp
            if (entry != null)
            {
                var onReadings = new List<string>();
                var kunReadings = new List<string>();

                if (entry.ReadingElements != null)
                {
                    foreach (var reading in entry.ReadingElements)
                    {
                        var splitReadings = reading.Reb.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var r in splitReadings)
                        {
                            // Thay thế Regex bằng .Replace đơn giản để tăng tốc
                            var cleanReading = r.Replace(".", "").Replace("-", "");
                            if (!string.IsNullOrEmpty(cleanReading) && IsKatakana(cleanReading))
                            {
                                if (!onReadings.Contains(r)) onReadings.Add(r);
                            }
                            else
                            {
                                if (!kunReadings.Contains(r)) kunReadings.Add(r);
                            }
                        }
                    }
                }
                kanjiResult.On = string.Join(" ", onReadings);
                kanjiResult.Kun = string.Join(" ", kunReadings);

                if (entry.Senses != null)
                {
                    foreach (var sense in entry.Senses.OrderBy(s => s.SenseOrder))
                    {
                        var glossText = sense.Glosses.FirstOrDefault()?.Text ?? "";
                        switch (sense.Pos) // Dùng Pos (HanViet, Detail, Tips)
                        {
                            case "HanViet": kanjiResult.Mean = glossText; break;
                            case "Detail": kanjiResult.Detail = glossText; break;
                            case "Tips": kanjiResult.Tips = new KanjiTipsJson { Vi = glossText }; break;
                        }
                    }
                }
            }

            // Lấy ví dụ từ đối tượng Kanji (đã được Include)
            if (kanji.KanjiExamples != null)
            {
                kanjiResult.Example_kun = kanji.KanjiExamples
                    .Where(ex => ex.ExampleType == "kun" && ex.ReadingGroup != null)
                    .GroupBy(ex => ex.ReadingGroup!)
                    .ToDictionary(g => g.Key, g => g.Select(item => new KanjiExampleJson { W = item.Word, M = item.Meaning, P = item.Reading }).ToList());

                kanjiResult.Example_on = kanji.KanjiExamples
                    .Where(ex => ex.ExampleType == "on" && ex.ReadingGroup != null)
                    .GroupBy(ex => ex.ReadingGroup!)
                    .ToDictionary(g => g.Key, g => g.Select(item => new KanjiExampleJson { W = item.Word, M = item.Meaning, P = item.Reading }).ToList());

                kanjiResult.Examples = kanji.KanjiExamples
                    .Where(ex => ex.ExampleType == "general")
                    .Select(item => new KanjiExampleGeneralJson { W = item.Word, M = item.Meaning, P = item.Reading, H = item.HanViet })
                    .ToList();
            }

            return kanjiResult;
        }
        // Debug helper - chép vào class JsonBuilder
        private void LogEntryChildrenCounts(IEnumerable<Entry> entries, string tag)
        {
            foreach (var e in entries)
            {
                var wordIds = e.Words?.Select(w => w.Id).ToList() ?? new List<int>();
                var distinctWordIds = wordIds.Distinct().ToList();

                var senseIds = e.Senses?.Select(s => s.Id).ToList() ?? new List<int>();
                var distinctSenseIds = senseIds.Distinct().ToList();

                // count nested
                int synsetEntriesCount = e.Senses?.SelectMany(s => s.SynsetEntries ?? new List<SynsetEntry>()).Select(se => se.Id).Count() ?? 0;
                int distinctSynsetEntriesCount = e.Senses?.SelectMany(s => s.SynsetEntries ?? new List<SynsetEntry>()).Select(se => se.Id).Distinct().Count() ?? 0;

                int synonymItemsCount = e.Senses?.SelectMany(s => s.SynsetEntries ?? new List<SynsetEntry>())
                                               .SelectMany(se => se.SynonymItems ?? new List<SynonymItem>())
                                               .Select(si => si.Id).Count() ?? 0;
                int distinctSynonymItemsCount = e.Senses?.SelectMany(s => s.SynsetEntries ?? new List<SynsetEntry>())
                                               .SelectMany(se => se.SynonymItems ?? new List<SynonymItem>())
                                               .Select(si => si.Id).Distinct().Count() ?? 0;

                int kanjiExamplesCount = e.Words?.SelectMany(w => w.WordKanji ?? new List<WordKanji>())
                                                 .SelectMany(wk => wk.Kanji?.KanjiExamples ?? new List<KanjiExample>())
                                                 .Select(kex => kex.Id).Count() ?? 0;
                int distinctKanjiExamplesCount = e.Words?.SelectMany(w => w.WordKanji ?? new List<WordKanji>())
                                                 .SelectMany(wk => wk.Kanji?.KanjiExamples ?? new List<KanjiExample>())
                                                 .Select(kex => kex.Id).Distinct().Count() ?? 0;

                _logger.LogInformation("{Tag}: EntryId={EntryId} words={Words}/{DistinctWords} senses={Senses}/{DistinctSenses} synsetEntries={SE}/{DSE} synItems={SI}/{DSI} kanjiExamples={KE}/{DKE}",
                    tag, e.Id, wordIds.Count, distinctWordIds.Count, senseIds.Count, distinctSenseIds.Count,
                    synsetEntriesCount, distinctSynsetEntriesCount, synonymItemsCount, distinctSynonymItemsCount, kanjiExamplesCount, distinctKanjiExamplesCount);
            }
        }
        // Chọn bản "chuẩn" trong 1 nhóm entries có cùng WordText+Phonetic
        private Entry SelectPreferredEntry(IEnumerable<Entry> group)
        {
            // Tiêu chí (theo thứ tự ưu tiên):
            // 1) nhiều Senses hơn (bản đầy đủ hơn)
            // 2) highest main word weight (nếu có)
            // 3) lowest Id (deterministic tie-breaker)
            return group
                .OrderByDescending(e => e.Senses?.Count ?? 0)
                .ThenByDescending(e => e.Words?.OrderBy(w => w.Id).FirstOrDefault()?.Weight ?? 0)
                .ThenBy(e => e.Id)
                .First();
        }

        // Helper để kiểm tra Katakana (cho việc phân loại On/Kun)
        private bool IsKatakana(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            // Dải Unicode cho Katakana (U+30A0 đến U+30FF)
            return text.All(c => c >= 0x30A0 && c <= 0x30FF);
        }

        #endregion
    }

    // Extension Method để gom các .Include()
    public static class DbContextExtensions
    {
        // Extension method MỚI, CHUẨN XÁC
        public static IQueryable<Entry> IncludeWordData(this IQueryable<Entry> query)
        {
            return query
                .Include(e => e.Words).ThenInclude(w => w.Relations).ThenInclude(r => r.RelatedWord)
                .Include(e => e.Senses).ThenInclude(s => s.Glosses)
                .Include(e => e.Senses).ThenInclude(s => s.Examples)
                .Include(e => e.Media.Where(m => m.MediaType == "image"))
                .Include(e => e.Senses).ThenInclude(s => s.SynsetEntries).ThenInclude(se => se.SynonymItems)
                // Chỉ Include đến Kanji, không đi sâu hơn vào Entry của Kanji
                .Include(e => e.Words).ThenInclude(w => w.WordKanji).ThenInclude(wk => wk.Kanji).ThenInclude(k => k.KanjiExamples)
                .AsSplitQuery();
        }
    }
}