using Dict.Data;
using Dict.Models;
using Dict.Models.JsonModels; // Đảm bảo bạn có namespace này
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dict.Service
{
    public class JsonBuilder : IJsonBuilderService
    {
        private readonly ApplicationDbContext _db;

        public JsonBuilder(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Truy vấn và xây dựng chuỗi JSON hoàn chỉnh cho một Hán tự.
        /// </summary>
        public async Task<string> RebuildJsonForKanjiAsync(string kanjiCharacter)
        {
            if (string.IsNullOrEmpty(kanjiCharacter) || kanjiCharacter.Length != 1)
            {
                return JsonConvert.SerializeObject(new { status = 404, error = "Invalid Kanji character" });
            }

            // SỬA LỖI: Chạy các truy vấn một cách tuần tự (sequentially)

            // 1. Truy vấn Entry (lấy cách đọc, nghĩa...) VÀ ĐỢI (await)
            var entryFromDb = await _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "kanji" && e.Label == kanjiCharacter)
                .Include(e => e.ReadingElements)
                .Include(e => e.Senses.OrderBy(s => s.SenseOrder)).ThenInclude(s => s.Glosses)
                .FirstOrDefaultAsync();

            // 2. Truy vấn Kanji (lấy số nét, ví dụ lồng nhau...) VÀ ĐỢI (await)
            var kanjiFromDb = await _db.Kanji
                .AsNoTracking()
                .Where(k => k.Character == kanjiCharacter)
                .Include(k => k.KanjiExamples) // Nạp TẤT CẢ các ví dụ liên quan
                .FirstOrDefaultAsync();

            // 3. Bây giờ mới kiểm tra kết quả
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

            // 4. Ánh xạ (Map) sang Model JSON
            var kanjiResult = MapKanjiDataToJson(entryFromDb, kanjiFromDb); // Gọi hàm helper

            var rootObject = new KanjiRootObject
            {
                Results = new List<KanjiResult> { kanjiResult }
            };

            return JsonConvert.SerializeObject(rootObject, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }


        // (Helper cho KANJI)
        private KanjiResult MapKanjiDataToJson(Entry entry, Kanji kanji)
        {
            var kanjiResult = new KanjiResult
            {
                Kanji = kanji.Character,
                Stroke_count = kanji.StrokeCount,
                Freq = kanji.Freq,
                //MobileId = kanji.MobileId, // Lấy MobileId từ bảng Kanji
                Level = new List<string> { kanji.JlptLevel ?? "" }
            };

            // Phân biệt On/Kun Reading
            var onReadings = entry.ReadingElements
                                  .Select(r => r.Reb)
                                  .Where(r => !string.IsNullOrEmpty(r) && IsKatakana(Regex.Replace(r, @"[\.|\-]", "")))
                                  .ToList();
            var kunReadings = entry.ReadingElements
                                   .Select(r => r.Reb)
                                   .Where(r => !onReadings.Contains(r))
                                   .ToList();

            kanjiResult.On = string.Join(" ", onReadings);
            kanjiResult.Kun = string.Join(" ", kunReadings);

            // Điền các nghĩa từ Senses/Glosses
            foreach (var sense in entry.Senses.OrderBy(s => s.SenseOrder))
            {
                var glossText = sense.Glosses.FirstOrDefault()?.Text ?? "";
                switch (sense.Pos) // Dùng Pos (mà chúng ta đã lưu là "HanViet", "Detail", "Tips")
                {
                    case "HanViet": kanjiResult.Mean = glossText; break;
                    case "Detail": kanjiResult.Detail = glossText; break;
                    case "Tips": kanjiResult.Tips = new KanjiTipsJson { Vi = glossText }; break;
                }
            }

            // Tái tạo "example_kun"
            kanjiResult.Example_kun = kanji.KanjiExamples
                .Where(ex => ex.ExampleType == "kun" && ex.ReadingGroup != null)
                .GroupBy(ex => ex.ReadingGroup!)
                .ToDictionary(g => g.Key, g => g.Select(item => new KanjiExampleJson { W = item.Word, M = item.Meaning, P = item.Reading }).ToList());

            // Tái tạo "example_on"
            kanjiResult.Example_on = kanji.KanjiExamples
                .Where(ex => ex.ExampleType == "on" && ex.ReadingGroup != null)
                .GroupBy(ex => ex.ReadingGroup!)
                .ToDictionary(g => g.Key, g => g.Select(item => new KanjiExampleJson { W = item.Word, M = item.Meaning, P = item.Reading }).ToList());

            // Tái tạo "examples"
            kanjiResult.Examples = kanji.KanjiExamples
                .Where(ex => ex.ExampleType == "general")
                .Select(item => new KanjiExampleGeneralJson { W = item.Word, M = item.Meaning, P = item.Reading, H = item.HanViet })
                .ToList();

            return kanjiResult;
        }

        // Helper để kiểm tra Katakana (cho việc phân loại On/Kun)
        private bool IsKatakana(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            // Dải Unicode cho Katakana (U+30A0 đến U+30FF)
            return text.All(c => c >= 0x30A0 && c <= 0x30FF);
        }

        public async Task<string> RebuildJsonForWordAsync(string labelToTest)
        {
            // --- TRUY VẤN KẾT QUẢ CHÍNH (words[0]) ---
            var entryFromDb = await _db.Entries
                .AsNoTracking()
                .Include(e => e.Words).ThenInclude(w => w.Relations).ThenInclude(r => r.RelatedWord)
                .Include(e => e.Senses).ThenInclude(s => s.Glosses)
                .Include(e => e.Senses).ThenInclude(s => s.Examples)
                .Include(e => e.Media.Where(m => m.MediaType == "image"))
                .Include(e => e.Senses).ThenInclude(s => s.SynsetEntries).ThenInclude(se => se.SynonymItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Label == labelToTest);

            List<Dict.Models.JsonModels.SuggestWord> suggestWords;
            Dict.Models.JsonModels.Word mainWordObject = null;

            if (entryFromDb != null)
            {
                // Nếu tìm thấy kết quả chính, lấy 19 gợi ý (loại trừ chính nó)
                suggestWords = await GetSuggestWordsAsync(labelToTest, 19, entryFromDb.Id);

                var mainWordRecord = entryFromDb.Words.FirstOrDefault();
                if (mainWordRecord != null)
                {
                    // --- ÁNH XẠ KẾT QUẢ CHÍNH ---
                    mainWordObject = new Dict.Models.JsonModels.Word
                    {
                        IdFromSource = entryFromDb.MobileId?.ToString() ?? Guid.NewGuid().ToString("N"), // Ưu tiên mobileId
                        WordText = mainWordRecord.WordText,
                        Phonetic = mainWordRecord.Phonetic,
                        ShortMean = mainWordRecord.ShortMean,
                        Weight = mainWordRecord.Weight,
                        MobileId = mainWordRecord.MobileId,
                        Images = entryFromDb.Media.Select(m => m.Url).ToList(),
                        OppositeWord = mainWordRecord.Relations
                            .Where(r => r.RelationType == "opposite")
                            .Select(r => r.RelatedWord.WordText)
                            .ToList()
                    };

                    // Ánh xạ Means và Examples
                    foreach (var senseFromDb in entryFromDb.Senses.OrderBy(s => s.SenseOrder))
                    {
                        mainWordObject.Means.Add(new Dict.Models.JsonModels.Mean
                        {
                            MeanText = senseFromDb.Glosses.FirstOrDefault()?.Text ?? "",
                            Kind = senseFromDb.Pos ?? "",
                            Examples = senseFromDb.Examples.Select(ex => new Dict.Models.JsonModels.Example
                            {
                                Content = ex.ContentJp,
                                Mean = ex.ContentTranslated,
                                Transcription = ex.Transcription
                            }).ToList()
                        });

                    }

                    // Ánh xạ Synsets
                    var firstSense = entryFromDb.Senses.OrderBy(s => s.SenseOrder).FirstOrDefault();
                    if (firstSense != null && firstSense.SynsetEntries.Any())
                    {
                        var synsetObject = new Dict.Models.JsonModels.Synset
                        {
                            BaseForm = firstSense.SynsetEntries.First().BaseForm,
                            Pos = firstSense.SynsetEntries.First().Pos
                        };
                        foreach (var entryGroup in firstSense.SynsetEntries)
                        {
                            synsetObject.Entry.Add(new SynonymEntry
                            {
                                Synonym = entryGroup.SynonymItems.Select(si => si.Word).ToList(),
                                DefinitionId = entryGroup.DefinitionId ?? ""
                            });
                        }
                        mainWordObject.Synsets.Add(synsetObject);
                    }

                    // Ánh xạ Pronunciation
                    if (!string.IsNullOrEmpty(mainWordRecord.Romaji) || !string.IsNullOrEmpty(mainWordRecord.Phonetic))
                    {
                        mainWordObject.Pronunciation.Add(new Dict.Models.JsonModels.Pronunciation
                        {
                            Word = mainWordRecord.WordText,
                            Type = "regular",
                            Transcriptions = new List<Transcription> { new Transcription { Romaji = mainWordRecord.Romaji, Kana = mainWordRecord.Phonetic } }
                        });
                    }
                }
            }
            else
            {
                // Nếu KHÔNG tìm thấy kết quả chính, chỉ tìm 20 gợi ý
                suggestWords = await GetSuggestWordsAsync(labelToTest, 20);
            }

            // --- ĐÓNG GÓI TẤT CẢ VÀO ĐỐI TƯỢNG GỐC ---
            var rootObject = new RootObject
            {
                Status = 200,
                Data = new Dict.Models.JsonModels.Data
                {
                    Words = mainWordObject != null ? new List<Dict.Models.JsonModels.Word> { mainWordObject } : new List<Dict.Models.JsonModels.Word>(),
                    SuggestWords = suggestWords
                }
            };

            // Chuyển đổi thành chuỗi JSON
            string json = JsonConvert.SerializeObject(rootObject, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            return json;
        }

        /// <summary>
        /// Phương thức chuyên tìm kiếm và xây dựng danh sách từ gợi ý theo "Hệ thống Đa tầng".
        /// </summary>
        private async Task<List<SuggestWord>> GetSuggestWordsAsync(string searchTerm, int limit, int? excludeEntryId = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SuggestWord>();

            var candidatesQuery = _db.Entries
                .AsNoTracking()
                .Where(e => e.Type == "word" && (
                    e.Label.Contains(searchTerm) ||
                    e.Phonetic.Contains(searchTerm)
                ));

            if (excludeEntryId.HasValue)
            {
                candidatesQuery = candidatesQuery.Where(e => e.Id != excludeEntryId.Value);
            }

            var rankedSuggestions = await candidatesQuery
                .Select(e => new
                {
                    Entry = e,
                    WordInfo = e.Words.FirstOrDefault(),
                    Score = (e.Label == searchTerm ? 100 : 0) +
                            (e.Label.StartsWith(searchTerm) ? 50 : 0) +
                            (e.Phonetic.StartsWith(searchTerm) ? 40 : 0) +
                            (e.Label.Contains(searchTerm) ? 10 : 0)
                })
                .Where(x => x.Score > 0 && x.WordInfo != null)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.WordInfo.Weight)
                .ThenBy(x => x.Entry.Label.Length)
                .Take(limit)
                .Select(x => x.Entry)
                .Include(e => e.Words)
                .Include(e => e.Senses).ThenInclude(s => s.Glosses)
                .AsSplitQuery()
                .ToListAsync();

            var result = new List<SuggestWord>();
            foreach (var entry in rankedSuggestions)
            {
                var wordRecord = entry.Words.FirstOrDefault();
                if (wordRecord == null) continue;

                var suggestionObject = new SuggestWord // <--- Tạo đối tượng mới
                {
                    IdFromSource = entry.MobileId?.ToString() ?? entry.Id.ToString(),
                    WordText = wordRecord.WordText,
                    Phonetic = wordRecord.Phonetic,
                    ShortMean = wordRecord.ShortMean,
                    MobileId = wordRecord.MobileId,
                    Means = entry.Senses
                                .OrderBy(s => s.SenseOrder)
                                .Select(s => new Dict.Models.JsonModels.Mean
                                {
                                    MeanText = s.Glosses.FirstOrDefault()?.Text ?? "",
                                    Kind = s.Pos ?? "",
                                    // Luôn trả về mảng rỗng cho nhẹ
                                    Examples = new List<Dict.Models.JsonModels.Example>()
                                }).ToList()
                };
                result.Add(suggestionObject);
            }

            return result;
        }
    }
}