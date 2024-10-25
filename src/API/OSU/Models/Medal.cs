#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    
    public class Medal
    {
        [JsonProperty("id")]
        public uint Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("grouping")]
        public string grouping { get; set; }

        [JsonProperty("icon_url")]
        public string icon_url { get; set; }

        [JsonProperty("instructions", NullValueHandling = NullValueHandling.Ignore)]
        public string? instructions { get; set; }

        [JsonProperty("mode", NullValueHandling = NullValueHandling.Ignore)]
        public Mode? mode { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("ordering")]
        public uint ordering { get; set; }

        [JsonProperty("slug")]
        public string slug { get; set; }
    }

    public class MedalCompact {

        [JsonProperty("achievement_id")]
        public uint MedalId { get; set; }

        [JsonProperty("achieved_at")]
        public DateTimeOffset achieved_at { get; set; }
    }
}