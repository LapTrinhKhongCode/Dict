using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dict.Data;
using Dict.DTO;
using Microsoft.EntityFrameworkCore;

public class KanjiService : IKanjiService
{
    private readonly ApplicationDbContext _db;

    public KanjiService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<KanjiDto?> GetKanjiInfoAsync(
        string character,
        string languageCode = "en",
        int maxWords = 200,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(character))
            throw new ArgumentException("character is required", nameof(character));

        // Query with projection to DTO to avoid loading entire entities
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
