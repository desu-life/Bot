#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class Beatmap {
        [JsonProperty("md5")]
        public string Md5 { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("set_id")]
        public long SetId { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("last_update")]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonProperty("total_length")]
        public uint TotalLength { get; set; }

        [JsonProperty("max_combo")]
        public long MaxCombo { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("plays")]
        public long Plays { get; set; }

        [JsonProperty("passes")]
        public long Passes { get; set; }

        [JsonProperty("mode")]
        public Mode Mode { get; set; }

        [JsonProperty("bpm")]
        public double bpm { get; set; }

        [JsonProperty("cs")]
        public double CS { get; set; }

        [JsonProperty("od")]
        public double OD { get; set; }

        [JsonProperty("ar")]
        public double AR { get; set; }

        [JsonProperty("hp")]
        public double HP { get; set; }

        [JsonProperty("diff")]
        public double Diff { get; set; }
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