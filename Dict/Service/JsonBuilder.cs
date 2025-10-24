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
        private readonly string _sensitiveCollation = "Japanese_CS_AS_KS_WS"; // Collation phân biệt Kana

        public JsonBuilder(ApplicationDbContext db)
        {
            _db = db;
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
                .AsNoTracking()
                .Where(e => EF.Functions.Collate(e.Label, _sensitiveCollation) == term && e.Type == "word")
                .IncludeWordData() // Sử dụng extension method
                .FirstOrDefaultAsync();

            if (exactLabelMatch != null)
            {
                // TÌM THẤY 1 KẾT QUẢ (Ví dụ: tìm "食べる")
                mainEntries.Add(exactLabelMatch);
                suggestionEntries = await GetSuggestionEntriesAsync(term, 20, new List<int> { exactLabelMatch.Id });
            }
            else
            {
                // === ƯU TIÊN 2: TÌM THEO "CÁCH ĐỌC" (PHONETIC) ===
                var homophoneMatches = await _db.Entries
                    .AsNoTracking()
                    .Where(e => e.Words.Any(w => EF.Functions.Collate(w.Phonetic, _sensitiveCollation) == term) && e.Type == "word")
                    .OrderBy(e => e.Words.FirstOrDefault().Weight)
                    .IncludeWordData() // Dùng extension method
                    .ToListAsync();

                if (homophoneMatches.Any())
                {
                    // TÌM THẤY DANH SÁCH ĐỒNG ÂM (Ví dụ: tìm "とる")
                    mainEntries = homophoneMatches;
                    var homophoneIds = homophoneMatches.Select(e => e.Id).ToList();
                    suggestionEntries = await GetSuggestionEntriesAsync(term, 20, homophoneIds);
                }
                else
                {
                    // === ƯU TIÊN 3: GỢI Ý CHUNG - KHÔNG CÓ KẾT QUẢ CHÍNH ===
                    mainEntries = new List<Entry>(); // Rỗng
                    suggestionEntries = await GetSuggestionEntriesAsync(term, 20);
                }
            }

            // --- ÁNH XẠ VÀ ĐÓNG GÓI ---

            // 1. Lấy danh sách Kanji ID cần thiết từ TẤT CẢ các kết quả chính
            var allKanjiChars = mainEntries
                .SelectMany(e => e.Words.FirstOrDefault()?.WordKanji ?? new List<WordKanji>())
                .Select(wk => wk.Kanji?.Character)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            // 2. Chạy MỘT truy vấn phụ duy nhất để lấy dữ liệu cho TẤT CẢ Kanji đó
            var kanjiEntryDataMap = await GetKanjiEntryDataForMappingAsync(allKanjiChars!);

            // 3. Map kết quả chính
            data.Words = mainEntries.Select(entry => MapEntryToJsonWord(entry, kanjiEntryDataMap))
                                   .Where(w => w != null)
                                   .ToList()!;

            // 4. Map gợi ý
            data.SuggestWords = suggestionEntries.Select(MapEntryToSuggestWord)
                                                .Where(s => s != null)
                                                .ToList()!;

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
        public async Task<string> RebuildJsonForKanjiAsync(string kanjiCharacter)
        {
            if (string.IsNullOrEmpty(kanjiCharacter) || kanjiCharacter.Length != 1)
            {
                return JsonConvert.SerializeObject(new { status = 404, error = "Invalid Kanji character" });
            }

            // 1. Truy vấn Entry (lấy cách đọc, nghĩa...)
            var entryFromDb = await _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "kanji" && e.Label == kanjiCharacter)
                .Include(e => e.ReadingElements)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses)
                .FirstOrDefaultAsync();

            // 2. Truy vấn Kanji (lấy số nét, ví dụ lồng nhau...)
            var kanjiFromDb = await _db.Kanji
                .AsNoTracking()
                .Where(k => k.Character == kanjiCharacter)
                .Include(k => k.KanjiExamples.OrderBy(ex => ex.Id)) // Nạp TẤT CẢ các ví dụ liên quan
                .FirstOrDefaultAsync();

            if (entryFromDb == null || kanjiFromDb == null)
            {
                var rawJsonEntry = await _db.Entries
                    .AsNoTracking()
                    .Where(e => e.Type == "kanji" && e.Label == kanjiCharacter)
                    .Select(e => e.RawJson)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(rawJsonEntry)) return rawJsonEntry;

                return JsonConvert.SerializeObject(new { status = 404, error = "Kanji data not found in database" });
            }

            // 3. Ánh xạ (Map) sang Model JSON
            var kanjiResult = MapKanjiDataToJson(entryFromDb, kanjiFromDb);

            var rootObject = new KanjiRootObject
            {
                Results = new List<KanjiResult> { kanjiResult },
                Total = 10000 // Giả lập giá trị total như file gốc
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

            // --- BƯỚC 1: PHÂN TÍCH TỪ KHÓA ---
            // Tách riêng phần Hán tự
            string kanjiPart = ExtractKanji(term); // Ví dụ: "穫る" -> "穫"
            bool hasKanjiPart = !string.IsNullOrEmpty(kanjiPart);

            // --- BƯỚC 2: TRUY VẤN ỨNG VIÊN (DÙNG LIKE, KHÔNG DÙNG FTS) ---
            var candidatesQuery = _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "word" && (
                    e.Label.Contains(term) ||    // Dùng LIKE N'%term%'
                    e.Phonetic.Contains(term) || // Dùng LIKE N'%term%'
                                                 // SỬA LỖI: Dùng C# .Contains() (tức LIKE N'%kanjiPart%')
                    (hasKanjiPart && e.Label.Contains(kanjiPart))
                ));

            if (excludeEntryIds != null && excludeEntryIds.Any())
            {
                candidatesQuery = candidatesQuery.Where(e => !excludeEntryIds.Contains(e.Id));
            }

            // --- BƯỚC 3: CHẤM ĐIỂM VÀ SẮP XẾP ---
            var rankedSuggestions = await candidatesQuery
                .Select(e => new
                {
                    Entry = e,
                    WordInfo = e.Words.FirstOrDefault(),
                    // --- HỆ THỐNG ĐIỂM HOÀN CHỈNH (DÙNG LIKE) ---
                    Score =
                        (EF.Functions.Collate(e.Label, sensitiveCollation) == term ? 100 : 0) +
                        (e.Words.FirstOrDefault() != null && EF.Functions.Collate(e.Words.FirstOrDefault().Phonetic, sensitiveCollation) == term ? 90 : 0) +
                        (e.Label.StartsWith(term) ? 50 : 0) +
                        (e.Phonetic.StartsWith(term) ? 40 : 0) +
                        // SỬA LỖI: Dùng C# .Contains() (LIKE)
                        (hasKanjiPart && e.Label.Contains(kanjiPart) ? 20 : 0) +
                        (e.Label.Contains(term) ? 10 : 0) // Điểm "chứa" cơ bản
                })
                .Where(x => x.Score > 0 && x.WordInfo != null)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.WordInfo!.Weight)
                .ThenBy(x => x.Entry.Label.Length)
                .Take(limit)
                .Select(x => x.Entry)
                // Include dữ liệu cần thiết cho suggestWord (nhẹ)
                .Include(e => e.Words)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses)
                .AsSplitQuery()
                .ToListAsync();

            return rankedSuggestions;
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
        private Dict.Models.JsonModels.Word? MapEntryToJsonWord(Entry entryFromDb, Dictionary<string, Entry> kanjiEntryDataMap)
        {
            var mainWordRecord = entryFromDb.Words?.FirstOrDefault();
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

            // THÊM LOGIC MAPPING KANJI TỪ DỮ LIỆU ĐÃ TẢI SẴN
            if (mainWordRecord.WordKanji != null)
            {
                foreach (var wordKanjiLink in mainWordRecord.WordKanji.OrderBy(wk => wk.KanjiId))
                {
                    var kanji = wordKanjiLink.Kanji;
                    // Kiểm tra xem đã lấy được Entry của Kanji này chưa
                    if (kanji != null && kanjiEntryDataMap.TryGetValue(kanji.Character, out var kanjiEntry))
                    {
                        // Tái sử dụng helper 'MapKanjiDataToJson'
                        var kanjiResult = MapKanjiDataToJson(kanjiEntry, kanji);
                        wordObject.RelatedKanji.Add(kanjiResult);
                    }
                }
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
        /// (Helper) Tải trước dữ liệu Entry (cách đọc, nghĩa) cho các Hán tự liên quan
        /// </summary>
        private async Task<Dictionary<string, Entry>> GetKanjiEntryDataForMappingAsync(List<string> kanjiChars)
        {
            if (kanjiChars == null || !kanjiChars.Any())
            {
                return new Dictionary<string, Entry>();
            }

            // Truy vấn các Entry (loại 'kanji') có Label nằm trong danh sách
            return await _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "kanji" && kanjiChars.Contains(e.Label))
                .Include(e => e.ReadingElements) // Tải cách đọc
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses) // Tải nghĩa
                .ToDictionaryAsync(e => e.Label); // Trả về Dictionary để tra cứu nhanh
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