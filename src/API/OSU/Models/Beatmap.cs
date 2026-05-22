#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.ComponentModel;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class BeatmapList {

        [JsonPropertyName("beatmaps")]
        public Beatmap[] Beatmaps { get; set; }
    }

    public class Beatmap
    {
        [JsonPropertyName("beatmapset_id")]
        public long BeatmapsetId { get; set; }

        [JsonPropertyName("difficulty_rating")]
        public double DifficultyRating { get; set; }

        [JsonPropertyName("id")]
        public long BeatmapId { get; set; }

        [JsonPropertyName("mode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("total_length")]
        public uint TotalLength { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        // Accuracy = OD

        [JsonPropertyName("accuracy")]
        public double OD { get; set; }

        [JsonPropertyName("ar")]
        public double AR { get; set; }

        [JsonPropertyName("bpm")]
        public double? BPM { get; set; }

        [JsonPropertyName("convert")]
        public bool Convert { get; set; }

        [JsonPropertyName("count_circles")]
        public long CountCircles { get; set; }

        [JsonPropertyName("count_sliders")]
        public long CountSliders { get; set; }

        [JsonPropertyName("count_spinners")]
        public long CountSpinners { get; set; }

        [JsonPropertyName("cs")]
        public double CS { get; set; }

        [JsonPropertyName("deleted_at")]
        public DateTimeOffset? DeletedAt { get; set; }

        [JsonPropertyName("drain")]
        public double HPDrain { get; set; }

        [JsonPropertyName("hit_length")]
        public long HitLength { get; set; }

        [JsonPropertyName("is_scoreable")]
        public bool IsScoreable { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonPropertyName("mode_int")]
        public int ModeInt { get; set; }

        [JsonPropertyName("passcount")]
        public long Passcount { get; set; }

        [JsonPropertyName("playcount")]
        public long Playcount { get; set; }

        [JsonPropertyName("ranked")]
        public long Ranked { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        [JsonPropertyName("beatmapset")]
        public Beatmapset? Beatmapset { get; set; }

        [JsonPropertyName("failtimes")]
        public BeatmapFailtimes Failtimes { get; set; }

        [JsonPropertyName("max_combo")]
        public long? MaxCombo { get; set; }
    }


    public class BeatmapFailtimes
    {
        [JsonPropertyName("fail")]
        public int[]? Fail { get; set; }

        [JsonPropertyName("exit")]
        public int[]? Exit { get; set; }
    }

    [DefaultValue(Unknown)]
    [JsonConverter(typeof(JsonStringEnumConverter<Status>))]
    public enum Status
    {
        /// <summary>
        /// 未知，在转换错误时为此值
        /// </summary>
        Unknown,

        [JsonStringEnumMemberName("graveyard")]
        Graveyard,

        [JsonStringEnumMemberName("wip")]
        WIP,

        [JsonStringEnumMemberName("pending")]
        Pending,

        [JsonStringEnumMemberName("ranked")]
        Ranked,

        [JsonStringEnumMemberName("approved")]
        Approved,

        [JsonStringEnumMemberName("qualified")]
        Qualified,

        [JsonStringEnumMemberName("loved")]
        Loved
    }
}