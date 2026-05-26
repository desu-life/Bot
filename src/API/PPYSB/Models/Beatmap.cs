#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class MapResponseV2 : ApiResponseV2 {
        [JsonPropertyName("data")]
        public Beatmap Data { get; set; }
    }

    public class Beatmap {
        [JsonPropertyName("md5")]
        public string Md5 { get; set; }

        [JsonPropertyName("id")]
        public long BeatmapId { get; set; }

        [JsonPropertyName("set_id")]
        public long BeatmapsetId { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("creator")]
        public string Creator { get; set; }

        [JsonPropertyName("last_update")]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonPropertyName("total_length")]
        public uint TotalLength { get; set; }

        [JsonPropertyName("max_combo")]
        public long MaxCombo { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("plays")]
        public long Plays { get; set; }

        [JsonPropertyName("passes")]
        public long Passes { get; set; }

        [JsonPropertyName("mode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("bpm")]
        public double BPM { get; set; }

        [JsonPropertyName("cs")]
        public double CS { get; set; }

        [JsonPropertyName("od")]
        public double OD { get; set; }

        [JsonPropertyName("ar")]
        public double AR { get; set; }

        [JsonPropertyName("hp")]
        public double HP { get; set; }

        [JsonPropertyName("diff")]
        public double DifficultyRating { get; set; }
    }

    public enum Status
    {
        NotSubmitted = -1,
        Pending = 0,
        UpdateAvailable = 1,
        Ranked = 2,
        Approved = 3,
        Qualified = 4,
        Loved = 5
    }
}

// {
//     "md5": "4e392566be350059b31f029ad4d889cf",
//     "id": 81136,
//     "set_id": 23754,
//     "artist": "USBduck",
//     "title": "Keyboard Cat ZONE",
//     "version": "Party Time",
//     "creator": "Kurosanyan",
//     "last_update": "2012-05-19T09:38:11",
//     "total_length": 105,
//     "max_combo": 798,
//     "status": 2,
//     "plays": 40,
//     "passes": 10,
//     "mode": 0,
//     "bpm": 165,
//     "cs": 4,
//     "od": 7,
//     "ar": 9,
//     "hp": 7,
//     "diff": 5.447
// }