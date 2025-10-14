using Newtonsoft.Json;
using System.Collections.Generic;

// ---
// QUAN TRỌNG: Các lớp trong file này chỉ dùng để định nghĩa cấu trúc của file JSON
// sẽ được tạo ra. Chúng tách biệt hoàn toàn với các lớp model của Entity Framework Core
// dùng để ánh xạ với database.
// ---

namespace Dict.Models.JsonModels
{
    /// <summary>
    /// Đối tượng gốc của toàn bộ file JSON.
    /// </summary>
    public class RootObject
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    /// <summary>
    /// Đối tượng chứa dữ liệu chính của từ điển.
    /// </summary>
    public class Data
    {
        // suggestWords có thể được thêm vào sau nếu cần
        // [JsonProperty("suggestWords")]
        // public List<Word> SuggestWords { get; set; } = new List<Word>();

        [JsonProperty("words")]
        public List<Word> Words { get; set; } = new List<Word>();
    }

    /// <summary>
    /// Đại diện cho một mục từ chi tiết trong mảng "words".
    /// </summary>
    public class Word
    {
        [JsonProperty("_id")]
        public string IdFromSource { get; set; }

        [JsonProperty("word")]
        public string WordText { get; set; }

        [JsonProperty("phonetic")]
        public string Phonetic { get; set; }

        [JsonProperty("weight")]
        public int? Weight { get; set; }

        [JsonProperty("short_mean")]
        public string ShortMean { get; set; }

        [JsonProperty("mobileId")]
        public int? MobileId { get; set; }

        [JsonProperty("means")]
        public List<Mean> Means { get; set; } = new List<Mean>();

        [JsonProperty("synsets")]
        public List<Synset> Synsets { get; set; } = new List<Synset>();

        [JsonProperty("opposite_word")]
        public List<string> OppositeWord { get; set; } = new List<string>();

        [JsonProperty("pronunciation")]
        public List<Pronunciation> Pronunciation { get; set; } = new List<Pronunciation>();

        [JsonProperty("images")]
        public List<string> Images { get; set; } = new List<string>();

        // Các trường khác có thể có trong JSON gốc
        [JsonProperty("type")]
        public string Type { get; set; } = "word";
    }

    /// <summary>
    /// Đại diện cho một tầng nghĩa của từ.
    /// </summary>
    public class Mean
    {
        [JsonProperty("mean")]
        public string MeanText { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("examples")]
        public List<Example> Examples { get; set; } = new List<Example>();
    }

    /// <summary>
    /// Đại diện cho một câu ví dụ.
    /// </summary>
    public class Example
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("mean")]
        public string Mean { get; set; }

        [JsonProperty("transcription")]
        public string Transcription { get; set; }
    }

    /// <summary>
    /// Đại diện cho một nhóm từ đồng nghĩa.
    /// </summary>
    public class Synset
    {
        [JsonProperty("base_form")]
        public string BaseForm { get; set; }

        [JsonProperty("pos")]
        public string Pos { get; set; }

        [JsonProperty("entry")]
        public List<SynonymEntry> Entry { get; set; } = new List<SynonymEntry>();
    }

    /// <summary>
    /// Đại diện cho một mục trong nhóm từ đồng nghĩa.
    /// </summary>
    public class SynonymEntry
    {
        [JsonProperty("synonym")]
        public List<string> Synonym { get; set; } = new List<string>();

        [JsonProperty("definition_id")]
        public string DefinitionId { get; set; } = "";
    }

    /// <summary>
    /// Đại diện cho thông tin phát âm chi tiết.
    /// </summary>
    public class Pronunciation
    {
        [JsonProperty("word")]
        public string Word { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("transcriptions")]
        public List<Transcription> Transcriptions { get; set; } = new List<Transcription>();
    }

    /// <summary>
    /// Đại diện cho cách phiên âm Romaji và Kana.
    /// </summary>
    public class Transcription
    {
        [JsonProperty("romaji")]
        public string Romaji { get; set; }

        [JsonProperty("kana")]
        public string Kana { get; set; }
    }
}