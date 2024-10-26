#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{


    public class BeatmapSearchResult
    {
        [JsonProperty("beatmapsets")]
        public List<Beatmapset> Beatmapsets { get; set; }

        [JsonProperty("search")]
        public SearchResult Search { get; set; }

        [JsonProperty("recommended_difficulty")]
        public object? RecommendedDifficulty { get; set; }

        [JsonProperty("error")]
        public object? Error { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("cursor")]
        public CursorResult Cursor { get; set; }

        [JsonProperty("cursor_string")]
        public string CursorString { get; set; }

        public class SearchResult
        {
            [JsonProperty("sort")]
            public string sort { get; set; }
        }

        public class CursorResult
        {
            [JsonProperty("approved_date")]
            public long approved_date { get; set; }

            [JsonProperty("id")]
            public int id { get; set; }
        }
    }


}