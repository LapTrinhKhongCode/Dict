using Dict.DTO;
using System.Threading;
using System.Threading.Tasks;

public interface IKanjiService
{
    Task<KanjiDto?> GetKanjiInfoAsync(
        string character,
        string languageCode = "en",
        int maxWords = 200,
        CancellationToken cancellationToken = default);

    Task<string?> GetKanjiJson(string label);
}
