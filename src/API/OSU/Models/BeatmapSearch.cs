#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{


    public class BeatmapSearchResult
    {
        [JsonPropertyName("beatmapsets")]
        public List<Beatmapset> Beatmapsets { get; set; }

        [JsonPropertyName("search")]
        public SearchResult Search { get; set; }

        [JsonPropertyName("recommended_difficulty")]
        public object? RecommendedDifficulty { get; set; }

        [JsonPropertyName("error")]
        public object? Error { get; set; }

        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("cursor")]
        public CursorResult Cursor { get; set; }

        [JsonPropertyName("cursor_string")]
        public string CursorString { get; set; }

        public class SearchResult
        {
            [JsonPropertyName("sort")]
            public string sort { get; set; }
        }

        public class CursorResult
        {
            [JsonPropertyName("approved_date")]
            public long approved_date { get; set; }

            [JsonPropertyName("id")]
            public int id { get; set; }
        }
    }


}