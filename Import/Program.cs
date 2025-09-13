using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

#region POCOs
public class RootResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("data")]
    public DataNode Data { get; set; } = new DataNode();
}

public class DataNode
{
    [JsonProperty("suggestWords")]
    public List<SuggestWord> SuggestWords { get; set; } = new();

    [JsonProperty("words")]
    public List<WordItem> Words { get; set; } = new();
}

public class SuggestWord
{
    [JsonProperty("word")]
    public string Word { get; set; }

    [JsonProperty("means")]
    public List<MeanItem> Means { get; set; } = new();

    [JsonProperty("phonetic")]
    public string Phonetic { get; set; }

    [JsonProperty("mobileId")]
    public long MobileId { get; set; }

    [JsonProperty("_id")]
    public string Id { get; set; }

    [JsonProperty("short_mean")]
    public string ShortMean { get; set; }
}

public class WordItem
{
    [JsonProperty("_id")]
    public string Id { get; set; }

    [JsonProperty("_rev")]
    public string Rev { get; set; }

    [JsonProperty("word")]
    public string Word { get; set; }

    [JsonProperty("phonetic")]
    public string Phonetic { get; set; }

    [JsonProperty("weight")]
    public int? Weight { get; set; }

    [JsonProperty("short_mean")]
    public string ShortMean { get; set; }

    [JsonProperty("mobileId")]
    public long MobileId { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("lang")]
    public string Lang { get; set; }

    [JsonProperty("means")]
    public List<MeanItem> Means { get; set; } = new();

    [JsonProperty("synsets")]
    public object Synsets { get; set; } // keep raw if not needed now

    [JsonProperty("pronunciation")]
    public object Pronunciation { get; set; }

    [JsonProperty("images")]
    public List<string> Images { get; set; } = new();
}

public class MeanItem
{
    [JsonProperty("mean")]
    public string Mean { get; set; }

    [JsonProperty("kind")]
    public string Kind { get; set; }

    [JsonProperty("examples")]
    public List<ExampleItem> Examples { get; set; } = new();
}

public class ExampleItem
{
    [JsonProperty("content")]
    public string Content { get; set; }

    [JsonProperty("mean")]
    public string Mean { get; set; }

    [JsonProperty("transcription")]
    public string Transcription { get; set; }
}
#endregion

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        // (Bạn có thể load từ file, web response, v.v. - ở đây để trực tiếp cho demo)
        string json = @"{
                ""status"": 200,
                ""data"": {
                    ""suggestWords"": [
                        {
                            ""word"": ""えねるぎーじょうほうきょく"",
                            ""means"": [
                                {
                                    ""mean"": ""Cơ quan Thông tin Năng lượng; Cục Thông tin Năng lượng"",
                                    ""kind"": null,
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""エネルギー情報局"",
                            ""mobileId"": 49702,
                            ""_id"": ""14f45d45c3bdd1e233529a52d266abd3"",
                            ""short_mean"": ""cơ quan thông tin năng lượng; cục thông tin năng lượng""
                        },
                        {
                            ""word"": ""こくぼうじょうほうせんたー"",
                            ""means"": [
                                {
                                    ""mean"": ""Trung tâm Thông tin Quốc phòng."",
                                    ""kind"": null,
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""国防情報センター"",
                            ""mobileId"": 135328,
                            ""_id"": ""ecb526903230c57ed702753f96fb1b18"",
                            ""short_mean"": ""trung tâm thông tin quốc phòng""
                        },
                        {
                            ""word"": ""でんしぷらいばしーじょうほうせんたー"",
                            ""means"": [
                                {
                                    ""mean"": ""Trung tâm Thông tin Mật Điện tử."",
                                    ""kind"": null,
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""電子プライバシー情報センター"",
                            ""mobileId"": 67501,
                            ""_id"": ""548c53dedaebde2e14d98d544e74f229"",
                            ""short_mean"": ""trung tâm thông tin mật điện tử""
                        },
                        {
                            ""word"": ""さんぎょうじょうほうかすいしんせんたー"",
                            ""means"": [
                                {
                                    ""mean"": ""Trung tâm Tin học hóa Công nghiệp."",
                                    ""kind"": null,
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""産業情報化推進センター"",
                            ""mobileId"": 137274,
                            ""_id"": ""1dc3c2df7631d92cd5956649baa0b402"",
                            ""short_mean"": ""trung tâm tin học hóa công nghiệp""
                        },
                        {
                            ""word"": ""旧情報"",
                            ""means"": [
                                {
                                    ""mean"": ""thông tin cũ"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""きゅうじょうほう"",
                            ""mobileId"": 260112,
                            ""_id"": ""ec29be730f6d2c3515564eb47c2778aa"",
                            ""short_mean"": ""thông tin cũ""
                        },
                        {
                            ""word"": ""情報トラック"",
                            ""means"": [
                                {
                                    ""mean"": ""rãnh thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうトラック"",
                            ""mobileId"": 243924,
                            ""_id"": ""a2f7b853350a7a0a56d790c26fe69a64"",
                            ""short_mean"": ""rãnh thông tin""
                        },
                        {
                            ""word"": ""情報ベース"",
                            ""means"": [
                                {
                                    ""mean"": ""cơ sở thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうベース"",
                            ""mobileId"": 243928,
                            ""_id"": ""c2a37eb7698c8a30e23b496643373f85"",
                            ""short_mean"": ""cơ sở thông tin""
                        },
                        {
                            ""word"": ""ユーザー情報"",
                            ""means"": [
                                {
                                    ""mean"": ""thông tin người dùng"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                },
                                {
                                    ""mean"": ""thông tin người sử dụng"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""ユーザーじょうほう"",
                            ""mobileId"": 240975,
                            ""_id"": ""d0dd1116856c936536c0b6664e932376"",
                            ""short_mean"": ""thông tin người dùng; thông tin người sử dụng""
                        },
                        {
                            ""word"": ""情報館"",
                            ""means"": [
                                {
                                    ""mean"": ""cơ quan thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうかん"",
                            ""mobileId"": 263876,
                            ""_id"": ""6af69d2817ab32b0f7bf0a2c478f6618"",
                            ""short_mean"": ""cơ quan thông tin""
                        },
                        {
                            ""word"": ""対情報"",
                            ""means"": [
                                {
                                    ""mean"": ""intelligence) /'kautərin, telidʤəns/, công tác phản gián"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""たいじょうほう"",
                            ""mobileId"": 63832,
                            ""_id"": ""9107bea1306f1c29aa877fff4ef2f082"",
                            ""short_mean"": ""intelligence) /'kautərin; telidʤəns/; công tác phản gián""
                        },
                        {
                            ""word"": ""リリース情報"",
                            ""means"": [
                                {
                                    ""mean"": ""thông tin phiên bản"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""リリースじょうほう"",
                            ""mobileId"": 241214,
                            ""_id"": ""a8fd5341bab1de18d14416b5a22b17ee"",
                            ""short_mean"": ""thông tin phiên bản""
                        },
                        {
                            ""word"": ""情報ブロック"",
                            ""means"": [
                                {
                                    ""mean"": ""khối thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうブロック"",
                            ""mobileId"": 243927,
                            ""_id"": ""c1c54e18ffd554d3d7de591aceb8c972"",
                            ""short_mean"": ""khối thông tin""
                        },
                        {
                            ""word"": ""情報誌"",
                            ""means"": [
                                {
                                    ""mean"": ""tạp chí thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうし"",
                            ""mobileId"": 57808,
                            ""_id"": ""cb423b4594c092cb2b9f0f5f734bb1bc"",
                            ""short_mean"": ""tạp chí thông tin""
                        },
                        {
                            ""word"": ""情報学"",
                            ""means"": [
                                {
                                    ""mean"": ""thông tin học"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうがく"",
                            ""mobileId"": 57799,
                            ""_id"": ""8efd5c2f3b522dd3f6ffcdb8e6be91de"",
                            ""short_mean"": ""thông tin học""
                        },
                        {
                            ""word"": ""情報部"",
                            ""means"": [
                                {
                                    ""mean"": ""bộ thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうぶ"",
                            ""mobileId"": 57814,
                            ""_id"": ""4333030b5b230a820adcb664d998cb49"",
                            ""short_mean"": ""bộ thông tin""
                        },
                        {
                            ""word"": ""アクセス情報"",
                            ""means"": [
                                {
                                    ""mean"": ""thông tin về truy cập"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""アクセスじょうほう"",
                            ""mobileId"": 235651,
                            ""_id"": ""4e951c1ac3b208f3faa5078b03111ce8"",
                            ""short_mean"": ""thông tin về truy cập""
                        },
                        {
                            ""word"": ""情報量"",
                            ""means"": [
                                {
                                    ""mean"": ""lượng thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうりょう"",
                            ""mobileId"": 57816,
                            ""_id"": ""2b202c7d637beb7eef3f33afe6604dd1"",
                            ""short_mean"": ""lượng thông tin""
                        },
                        {
                            ""word"": ""情報システム"",
                            ""means"": [
                                {
                                    ""mean"": ""hệ thống thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                },
                                {
                                    ""mean"": ""hệ thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""じょうほうシステム"",
                            ""mobileId"": 231901,
                            ""_id"": ""8edfee09314f2e7454fc49c6d813d750"",
                            ""short_mean"": ""hệ thống thông tin; hệ thông tin""
                        },
                        {
                            ""word"": ""各情報"",
                            ""means"": [
                                {
                                    ""mean"": ""tất cả thông tin"",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                }
                            ],
                            ""phonetic"": ""かくじょうほう"",
                            ""mobileId"": 20402,
                            ""_id"": ""a06429bbaa39b2263eb5431fa305aa87"",
                            ""short_mean"": ""tất cả thông tin""
                        }
                    ],
                    ""words"": [
                        {
                            ""_id"": ""978e3c9d14e9058fea1319cdff8dba41"",
                            ""_rev"": """",
                            ""word"": ""情報"",
                            ""phonetic"": ""じょうほう"",
                            ""weight"": -40700,
                            ""short_mean"": ""thông tin; tin tức; tình báo; thông tin"",
                            ""mobileId"": 98786,
                            ""label"": ""ja_vi"",
                            ""lang"": """",
                            ""means"": [
                                {
                                    ""mean"": ""thông tin; tin tức"",
                                    ""kind"": ""n"",
                                    ""examples"": [
                                        {
                                            ""content"": ""〜についてのもっと適切な情報"",
                                            ""mean"": ""thông tin chính xác hơn về..."",
                                            ""transcription"": ""〜についてのもっとてきせつなじょうほう""
                                        }
                                    ]
                                },
                                {
                                    ""mean"": ""tình báo."",
                                    ""kind"": ""n"",
                                    ""examples"": []
                                },
                                {
                                    ""mean"": ""thông tin"",
                                    ""examples"": [
                                        {
                                            ""content"": ""情報が患者を狼狽させるかもしれないとき、それは患者には知らされない。"",
                                            ""mean"": ""Thông tin đôi khi bị giữ lại từ bệnh nhân khi người ta cho rằng nó có thểlàm họ buồn."",
                                            ""transcription"": ""じょうほうがかんじゃをろうばいさせるかもしれないとき、それはかんじゃにはしらされない。""
                                        },
                                        {
                                            ""content"": ""情報を仕入れるために新聞を二部ほど読む"",
                                            ""mean"": ""Đọc hai bộ báo để lấy thông tin"",
                                            ""transcription"": ""じょうほうをしいれるためにしんぶんをにぶほどよむ""
                                        },
                                        {
                                            ""content"": ""情報技術の基礎知識を審査する"",
                                            ""mean"": ""kiểm tra kiến thức cơ bản về công nghệ thông tin"",
                                            ""transcription"": ""じょうほうぎじゅつのきそちしきをしんさする""
                                        }
                                    ]
                                }
                            ],
                            ""synsets"": [
                                {
                                    ""base_form"": ""情報"",
                                    ""pos"": ""noun"",
                                    ""entry"": [
                                        {
                                            ""synonym"": [
                                                ""知識"",
                                                ""見聞"",
                                                ""データ"",
                                                ""知見""
                                            ],
                                            ""definition_id"": """"
                                        },
                                        {
                                            ""synonym"": [
                                                ""音沙汰"",
                                                ""便り"",
                                                ""知らせ"",
                                                ""ニュース"",
                                                ""報"",
                                                ""消息"",
                                                ""沙汰"",
                                                ""音信"",
                                                ""報道""
                                            ],
                                            ""definition_id"": """"
                                        },
                                        {
                                            ""synonym"": [
                                                ""廣報"",
                                                ""弘報"",
                                                ""報告"",
                                                ""報知"",
                                                ""便り"",
                                                ""知らせ"",
                                                ""通信"",
                                                ""案内書"",
                                                ""ニュース"",
                                                ""報"",
                                                ""一報"",
                                                ""案内"",
                                                ""通知"",
                                                ""伝言"",
                                                ""消息"",
                                                ""広報"",
                                                ""通報"",
                                                ""報道""
                                            ],
                                            ""definition_id"": """"
                                        }
                                    ]
                                }
                            ],
                            ""type"": ""word"",
                            ""opposite_word"": null,
                            ""pronunciation"": [
                                {
                                    ""word"": ""情報"",
                                    ""type"": ""regular"",
                                    ""transcriptions"": [
                                        {
                                            ""romaji"": ""jo／–ho–‾"",
                                            ""kana"": ""じょ／ーほー‾""
                                        }
                                    ]
                                }
                            ],
                            ""images"": [
                                ""https://data.wingarc.com/wp-content/uploads/2019/01/dateandinfo.jpg"",
                                ""https://ggo.ismcdn.jp/mwimgs/4/e/-/img_4e93761d8e2d6ff3db0a6918b1afd589903377.jpg"",
                                ""https://www.sbbit.jp/article/image/35108/660_bit201807111336571424.jpg"",
                                ""https://data.wingarc.com/wp-content/uploads/2019/01/GettyImages-503784056-1024x899.jpg"",
                                ""https://data.wingarc.com/wp-content/uploads/2019/01/GettyImages-952679588-1024x678.jpg"",
                                ""https://nkpos.nikkei.co.jp/wp-content/themes/nkpos/common/images/mainvisual_sp.jpg"",
                                ""https://nkbb.nikkei.co.jp/km/assets-new/images/entries/column-information-gathering.png"",
                                ""https://www.innovation.co.jp/urumo/images/2018/04/information-gathering-top.png"",
                                ""https://www.pasonatech.co.jp/workstyle/column/media/2297_mv.png"",
                                ""https://ideasforgood.jp/_wu/2019/06/shutterstock_744456904.jpg"",
                                ""https://tspace-prod.s3.amazonaws.com/articles/2897ccf8d695b919041d4d9cbbda4a8e.jpg"",
                                ""https://d1qhpq68wlei2m.cloudfront.net/images/6173.jpg"",
                                ""https://p.e-words.jp/img/Information.png"",
                                ""https://www.mss.co.jp/business/info-security/img/main_img_sp.jpg"",
                                ""https://www.itc.u-tokyo.ac.jp/images/content/pict_main-image-01.jpg"",
                                ""https://www.e.kaiyodai.ac.jp/MT/images/lie/facility_img2.jpg"",
                                ""https://hnavi.co.jp/wp-content/uploads/2018/05/ICT2-min-640x320.jpg"",
                                ""https://www.jbsvc.co.jp/files/9262-00029-1.jpg"",
                                ""https://www.z-holdings.co.jp/ja/ir/main/0/teaserItems2/0/binaryNodeName/f743ef9bee6205ca10b1f7acfa1967dc.jpg"",
                                ""https://www.ttc.or.jp/application/files/2815/5121/6835/slide_01.jpg"",
                                ""https://sugiyama-kougyou.jp/wp-content/uploads/2016/11/13549847_s.jpg"",
                                ""https://acaric.jp/articles/wp-content/uploads/2021/06/business-5475661_1920.jpg"",
                                ""https://www.motex.co.jp/nomore/wp-content/uploads/2017/03/shutterstock_551729191.jpg"",
                                ""https://yochiyochi-digital.com/wp-content/uploads/2020/12/4dcfd0f236acb2f82d46cc411dd1d362.png"",
                                ""https://tspace-prod.s3.amazonaws.com/articles/81787c97c806e57cf2666b9192d2a75e.jpg""
                            ]
                        }
                    ]
                }
            }"; // <-- thay bằng JSON dài của bạn hoặc đọc từ file

        // Nếu JSON dài, tốt hơn đọc từ file:
        // string json = System.IO.File.ReadAllText("response.json");

        RootResponse root;
        try
        {
            root = JsonConvert.DeserializeObject<RootResponse>(json)
                   ?? throw new Exception("Không thể deserialize JSON thành RootResponse.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi parse JSON: {ex.Message}");
            return;
        }

        Console.WriteLine($"Status: {root.Status}");
        Console.WriteLine($"SuggestWords: {root.Data.SuggestWords.Count}");
        Console.WriteLine($"Words: {root.Data.Words.Count}");
        Console.WriteLine(new string('-', 40));

        Console.WriteLine("=== SuggestWords (top 5) ===");
        for (int i = 0; i < Math.Min(5, root.Data.SuggestWords.Count); i++)
        {
            var s = root.Data.SuggestWords[i];
            Console.WriteLine($"Word: {s.Word} | Phonetic: {s.Phonetic} | MobileId: {s.MobileId}");
            foreach (var m in s.Means)
            {
                Console.WriteLine($"  - Mean: {m.Mean} (Kind: {m.Kind ?? "null"}) Examples: {m.Examples.Count}");
            }
        }

        Console.WriteLine(new string('-', 40));
        Console.WriteLine("=== Words (detailed, first 3) ===");

        for (int i = 0; i < Math.Min(3, root.Data.Words.Count); i++)
        {
            var w = root.Data.Words[i];
            Console.WriteLine($"Word: {w.Word} | Phonetic: {w.Phonetic} | MobileId: {w.MobileId} | ShortMean: {w.ShortMean}");
            Console.WriteLine($"Images: {(w.Images?.Count ?? 0)}");
            foreach (var mean in w.Means)
            {
                Console.WriteLine($"  Mean: {mean.Mean} (Kind: {mean.Kind ?? "null"}) Examples: {mean.Examples.Count}");
                foreach (var ex in mean.Examples)
                {
                    Console.WriteLine($"    - Example content: {ex.Content}");
                    Console.WriteLine($"      Mean: {ex.Mean}");
                    Console.WriteLine($"      Transcription: {ex.Transcription}");
                }
            }
            Console.WriteLine();
        }
    }
}
