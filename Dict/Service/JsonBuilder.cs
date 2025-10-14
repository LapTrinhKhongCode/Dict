using Dict.Data;
using Dict.Models;
using Dict.Models.JsonModels; // Đảm bảo bạn đã có các model JSON
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<string> RebuildJsonForWordAsync(string labelToTest)
        {
            var entryFromDb = await _db.Entries
                .AsNoTracking()
                .Include(e => e.Words)
                .Include(e => e.Senses).ThenInclude(s => s.Glosses)
                .Include(e => e.Senses).ThenInclude(s => s.Examples)
                .Include(e => e.Media.Where(m => m.MediaType == "image"))
                .Include(e => e.Senses).ThenInclude(s => s.SynsetEntries).ThenInclude(se => se.SynonymItems)
                // MỚI: Include cả các quan hệ từ
                .Include(e => e.Words).ThenInclude(w => w.Relations)
                    .ThenInclude(r => r.RelatedWord) // Tải thông tin của từ liên quan
                .FirstOrDefaultAsync(e => e.Label == labelToTest);

            if (entryFromDb == null)
            {
                return JsonConvert.SerializeObject(new { status = 404, error = "Word not found" });
            }

            var mainWordRecord = entryFromDb.Words.FirstOrDefault();
            if (mainWordRecord == null)
            {
                return JsonConvert.SerializeObject(new { status = 404, error = "Entry has no associated Word." });
            }

            // --- BẮT ĐẦU ÁNH XẠ (MAPPING) ---

            var wordObject = new Dict.Models.JsonModels.Word
            {
                IdFromSource = Guid.NewGuid().ToString("N"),
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

            // Xử lý các tầng nghĩa và ví dụ
            foreach (var senseFromDb in entryFromDb.Senses.OrderBy(s => s.SenseOrder))
            {
                var meanObject = new Dict.Models.JsonModels.Mean
                {
                    MeanText = senseFromDb.Glosses.FirstOrDefault()?.Text,
                    Kind = senseFromDb.Pos,
                    Examples = senseFromDb.Examples.Select(ex => new Dict.Models.JsonModels.Example
                    {
                        Content = ex.ContentJp,
                        Mean = ex.ContentTranslated,
                        Transcription = ex.Transcription
                    }).ToList()
                };
                wordObject.Means.Add(meanObject);
            }

            // Xử lý synsets (với giới hạn hiện tại)
            var firstSense = entryFromDb.Senses.OrderBy(s => s.SenseOrder).FirstOrDefault();
            if (firstSense != null && firstSense.SynsetEntries.Any())
            {
                var synsetObject = new Dict.Models.JsonModels.Synset
                {
                    BaseForm = firstSense.SynsetEntries.First().BaseForm,
                    Pos = firstSense.SynsetEntries.First().Pos
                };

                // Lặp qua mỗi entry group để tạo lại cấu trúc lồng nhau
                foreach (var entryGroup in firstSense.SynsetEntries)
                {
                    var synonymList = entryGroup.SynonymItems.Select(si => si.Word).ToList();
                    synsetObject.Entry.Add(new SynonymEntry
                    {
                        Synonym = synonymList,
                        DefinitionId = entryGroup.DefinitionId ?? ""
                    });
                }
                wordObject.Synsets.Add(synsetObject);
            }

            // MỚI: Xử lý opposite_word
            if (mainWordRecord.Relations != null)
            {
                wordObject.OppositeWord = mainWordRecord.Relations
                    .Where(r => r.RelationType == "antonym" || r.RelationType == "opposite") // Giả sử bạn lưu như vậy
                    .Select(r => r.RelatedWord.WordText)
                    .ToList();
            }

            // MỚI: Tái tạo pronunciation từ dữ liệu có sẵn
            if (!string.IsNullOrEmpty(mainWordRecord.Romaji) || !string.IsNullOrEmpty(mainWordRecord.Phonetic))
            {
                var pronunciationObject = new Dict.Models.JsonModels.Pronunciation
                {
                    Word = mainWordRecord.WordText,
                    Type = "regular", // Giả định là 'regular'
                    Transcriptions = new List<Transcription>
                    {
                        new Transcription { Romaji = mainWordRecord.Romaji, Kana = mainWordRecord.Phonetic }
                    }
                };
                wordObject.Pronunciation.Add(pronunciationObject);
            }

            // Đóng gói tất cả
            var rootObject = new RootObject
            {
                Status = 200,
                Data = new Dict.Models.JsonModels.Data { Words = new List<Dict.Models.JsonModels.Word> { wordObject } }
            };

            // Chuyển đổi thành chuỗi JSON
            string json = JsonConvert.SerializeObject(rootObject, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            return json;
        }
    }
}
