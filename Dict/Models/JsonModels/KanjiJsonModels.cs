using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dict.Models.JsonModels
{
    // Cấu trúc cho một item trong mảng "example_on/kun"
    public class KanjiExampleJson
    {
        [JsonProperty("w")]
        public string W { get; set; } // Word
        [JsonProperty("m")]
        public string M { get; set; } // Meaning
        [JsonProperty("p")]
        public string P { get; set; } // Phonetic
    }

    // Cấu trúc cho một item trong mảng "examples"
    public class KanjiExampleGeneralJson : KanjiExampleJson
    {
        [JsonProperty("h")]
        public string? H { get; set; } // HanViet
    }

    // Cấu trúc cho đối tượng "tips"
    public class KanjiTipsJson
    {
        [JsonProperty("vi")]
        public string? Vi { get; set; }
    }

    // Cấu trúc chính của một kết quả Kanji
    public class KanjiResult
    {
        [JsonProperty("label")]
        public string Label { get; set; } = "ja_vi";
        [JsonProperty("kanji")]
        public string Kanji { get; set; }
        [JsonProperty("on")]
        public string On { get; set; }
        [JsonProperty("freq")]
        public int? Freq { get; set; }

        // Dùng Dictionary để map "た.つ" -> [ {...}, {...} ]
        [JsonProperty("example_kun")]
        public Dictionary<string, List<KanjiExampleJson>> Example_kun { get; set; } = new Dictionary<string, List<KanjiExampleJson>>();

        [JsonProperty("image")]
        public string? Image { get; set; } // "stand"
        [JsonProperty("stroke_count")]
        public int? Stroke_count { get; set; }
        [JsonProperty("mobileId")]
        public int? MobileId { get; set; }

        [JsonProperty("compDetail")]
        public List<object> CompDetail { get; set; } = new List<object>(); // Luôn là mảng rỗng

        // Dùng Dictionary để map "リツ" -> [ {...}, {...} ]
        [JsonProperty("example_on")]
        public Dictionary<string, List<KanjiExampleJson>> Example_on { get; set; } = new Dictionary<string, List<KanjiExampleJson>>();

        [JsonProperty("tips")]
        public KanjiTipsJson Tips { get; set; }

        [JsonProperty("mean")]
        public string Mean { get; set; } // Nghĩa Hán Việt
        [JsonProperty("kun")]
        public string Kun { get; set; }

        [JsonProperty("writing")]
        public object Writing { get; set; } = null; // Luôn là null

        [JsonProperty("detail")]
        public string Detail { get; set; }
        [JsonProperty("level")]
        public List<string> Level { get; set; } = new List<string>();

        [JsonProperty("examples")]
        public List<KanjiExampleGeneralJson> Examples { get; set; } = new List<KanjiExampleGeneralJson>();
    }

    // Đối tượng gốc của file JSON Kanji
    public class KanjiRootObject
    {
        [JsonProperty("status")]
        public int Status { get; set; } = 200;
        [JsonProperty("results")]
        public List<KanjiResult> Results { get; set; } = new List<KanjiResult>();
        [JsonProperty("total")]
        public int Total { get; set; } = 10000; // Giá trị giả
    }
}