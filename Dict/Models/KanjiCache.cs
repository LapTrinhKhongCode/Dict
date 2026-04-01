namespace Dict.Models
{
    public class KanjiCache
    {
        // Key: Ký tự Kanji, Value: Nội dung RawJson
        public Dictionary<char, string> Data { get; set; } = new Dictionary<char, string>();
        public bool IsLoaded { get; set; } = false;
    }

}
