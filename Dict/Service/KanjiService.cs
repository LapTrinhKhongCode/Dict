using Dict.Data;
using Dict.DTO;
using Dict.Service.IService;
using EllipticCurve.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class KanjiService : IKanjiService
{
    private readonly ApplicationDbContext _db;
    private readonly IJsonBuilderService _jsonBuilderService;

    public KanjiService(ApplicationDbContext db, IJsonBuilderService jsonBuilderService)
    {
        _db = db;
        _jsonBuilderService = jsonBuilderService;
    }

    public async Task<string?> GetKanjiJson(string label)
    {
        string kanjiChars = new string(label.Where(c => c >= 0x4E00 && c <= 0x9FFF).ToArray());

        if (string.IsNullOrEmpty(kanjiChars))
        {
            var phoneticMatch = await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "kanji" && k.Phonetic == label)
                .Select(k => k.RawJson)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(phoneticMatch)) return phoneticMatch;

            return JsonConvert.SerializeObject(new { status = 404, error = "No Kanji found in search term" });
        }

        if (kanjiChars.Length == 1)
        {
            var rawJson = await _db.Entries
                .AsNoTracking()
                .Where(k => k.Type == "kanji" && k.Label == kanjiChars)
                .Select(k => k.RawJson)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(rawJson) && rawJson != "{}")
            {
                return rawJson;
            }
        }
        return await _jsonBuilderService.RebuildJsonForKanjiAsync(kanjiChars);
    }

    public async Task<KanjiDto?> GetKanjiInfoAsync(
        string character,
        string languageCode = "en",
        int maxWords = 200,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(character))
            throw new ArgumentException("character is required", nameof(character));

        var query = _db.Kanji
            .AsNoTracking()
            .Where(k => k.Character == character)
            .Select(k => new KanjiDto
            {
                Character = k.Character,
                StrokeCount = k.StrokeCount,
                Grade = k.Grade,
                JlptLevel = k.JlptLevel,
                Meaning = k.Meaning,
                Freq = k.Freq,
                Words = k.WordKanji
                    .Select(wk => wk.Word)
                    .Distinct()
                    // limit number of words to avoid huge payloads
                    .OrderBy(w => w.WordText)
                    .Take(maxWords)
                    .Select(w => new WordSummaryDto
                    {
                        WordId = w.Id,
                        WordText = w.WordText,
                        Phonetic = w.Phonetic,
                        Romaji = w.Romaji,
                        ShortMean = w.ShortMean,
                        KanjiInWord = w.WordToEntries
                            .SelectMany(wte => wte.Entry.KanjiElements)
                            .Select(ke => ke.Keb)
                            .Where(keb => keb != null)
                            .Distinct()
                            .ToList(),
                        Entries = w.WordToEntries
                            .Select(wte => wte.Entry)
                            .Distinct()
                            .Select(e => new EntryDto
                            {
                                EntSeq = (long)e.EntSeq,
                                Type = e.Type,
                                KanjiForms = e.KanjiElements.Select(ke => ke.Keb).Where(x => x != null).ToList(),
                                Readings = e.ReadingElements.Select(re => re.Reb).Where(x => x != null).ToList(),
                                Senses = e.Senses
                                    .OrderBy(s => s.SenseOrder)
                                    .Select(s => new SenseDto
                                    {
                                        Pos = s.Pos,
                                        Field = s.Field,
                                        Misc = s.Misc,
                                        SInf = s.SInf,
                                        SenseOrder = (int)s.SenseOrder,
                                        Glosses = s.Glosses
                                                    .Where(g => g.Language != null ? g.Language.Code == languageCode : true)
                                                    .Select(g => g.Text)
                                                    .ToList(),
                                        Examples = s.Examples.Select(ex => new ExampleDto
                                        {
                                            ContentJp = ex.ContentJp,
                                            ContentTranslated = ex.ContentTranslated,
                                            Transcription = ex.Transcription
                                        }).ToList()
                                    }).ToList()
                            }).ToList(),
                        Translations = w.Translations
                            .Where(t => t.Language != null ? t.Language.Code == languageCode : true)
                            .Select(t => new TranslationDto
                            {
                                Id = t.Id,
                                Definition = t.Definition,
                                Kind = t.Kind,
                                LanguageCode = t.Language != null ? t.Language.Code : null
                            }).ToList(),
                        Media = w.Media
                            .Select(m => new MediaDto
                            {
                                Id = m.Id,
                                Url = m.Url,
                                Caption = m.Caption,
                                MediaType = m.MediaType
                            }).ToList()
                    })
                    .ToList()
            });

        var kanjiDto = await query.FirstOrDefaultAsync(cancellationToken);
        return kanjiDto;
    }
}
