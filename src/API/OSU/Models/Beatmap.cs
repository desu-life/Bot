#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;
using System.ComponentModel;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class BeatmapList {

        [JsonProperty(PropertyName = "beatmaps")]
        public Beatmap[] Beatmaps { get; set; }
    }

    public class Beatmap
    {
        [JsonProperty(PropertyName = "beatmapset_id")]
        public long BeatmapsetId { get; set; }

        [JsonProperty(PropertyName = "difficulty_rating")]
        public double DifficultyRating { get; set; }

        [JsonProperty(PropertyName = "id")]
        public long BeatmapId { get; set; }

        [JsonProperty(PropertyName = "mode")]
        [JsonConverter(typeof(JsonEnumConverter))]
        public Mode Mode { get; set; }

        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(JsonEnumConverter))]
        public Status Status { get; set; }

        [JsonProperty(PropertyName = "total_length")]
        public uint TotalLength { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public long UserId { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        // Accuracy = OD

        [JsonProperty(PropertyName = "accuracy")]
        public double OD { get; set; }

        [JsonProperty(PropertyName = "ar")]
        public double AR { get; set; }

        [JsonProperty(PropertyName = "bpm", NullValueHandling = NullValueHandling.Ignore)]
        public double? BPM { get; set; }

        [JsonProperty(PropertyName = "convert")]
        public bool Convert { get; set; }

        [JsonProperty(PropertyName = "count_circles")]
        public long CountCircles { get; set; }

        [JsonProperty(PropertyName = "count_sliders")]
        public long CountSliders { get; set; }

        [JsonProperty(PropertyName = "count_spinners")]
        public long CountSpinners { get; set; }

        [JsonProperty(PropertyName = "cs")]
        public double CS { get; set; }

        [JsonProperty(PropertyName = "deleted_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? DeletedAt { get; set; }

        [JsonProperty(PropertyName = "drain")]
        public double HPDrain { get; set; }

        [JsonProperty(PropertyName = "hit_length")]
        public long HitLength { get; set; }

        [JsonProperty(PropertyName = "is_scoreable")]
        public bool IsScoreable { get; set; }

        [JsonProperty(PropertyName = "last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty(PropertyName = "mode_int")]
        public int ModeInt { get; set; }

        [JsonProperty(PropertyName = "passcount")]
        public long Passcount { get; set; }

        [JsonProperty(PropertyName = "playcount")]
        public long Playcount { get; set; }

        [JsonProperty(PropertyName = "ranked")]
        public long Ranked { get; set; }

        [JsonProperty(PropertyName = "url")]
        public Uri Url { get; set; }

        [JsonProperty(PropertyName = "checksum", NullValueHandling = NullValueHandling.Ignore)]
        public string? Checksum { get; set; }

        [JsonProperty(PropertyName = "beatmapset", NullValueHandling = NullValueHandling.Ignore)]
        public Beatmapset? Beatmapset { get; set; }

        [JsonProperty(PropertyName = "failtimes")]
        public BeatmapFailtimes Failtimes { get; set; }

        [JsonProperty(PropertyName = "max_combo", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxCombo { get; set; }
    }


    public class BeatmapFailtimes
    {
        [JsonProperty(PropertyName = "fail", NullValueHandling = NullValueHandling.Ignore)]
        public int[]? Fail { get; set; }

        [JsonProperty(PropertyName = "exit", NullValueHandling = NullValueHandling.Ignore)]
        public int[]? Exit { get; set; }
    }

    [DefaultValue(Unknown)]
    public enum Status
    {
        /// <summary>
        /// 未知，在转换错误时为此值
        /// </summary>
        [Description("")]
        Unknown,

        [Description("graveyard")]
        Graveyard,

        [Description("wip")]
        WIP,

        [Description("pending")]
        Pending,

        [Description("ranked")]
        Ranked,

        [Description("approved")]
        Approved,

        [Description("qualified")]
        Qualified,

        [Description("loved")]
        Loved
    }
}