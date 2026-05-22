#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{
    
    public class Medal
    {
        [JsonPropertyName("id")]
        public uint Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("grouping")]
        public string grouping { get; set; }

        [JsonPropertyName("icon_url")]
        public string icon_url { get; set; }

        [JsonPropertyName("instructions")]
        public string? instructions { get; set; }

        [JsonPropertyName("mode")]
        public Mode? mode { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("ordering")]
        public uint ordering { get; set; }

        [JsonPropertyName("slug")]
        public string slug { get; set; }
    }

    public class MedalCompact {

        [JsonPropertyName("achievement_id")]
        public uint MedalId { get; set; }

        [JsonPropertyName("achieved_at")]
        public DateTimeOffset achieved_at { get; set; }
    }
}