#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KanonBot.Serializer;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class ScoreResponseV2 : ApiResponseV2
    {
        [JsonPropertyName("data")]
        public Score[] Data { get; set; }
    }

    public class PlayerScoreResponse : ApiResponse
    {
        [JsonPropertyName("scores")]
        public Score[] Scores { get; set; }

        [JsonPropertyName("player")]
        public ScoreUser Player { get; set; }
    }

    public class ScoreResponse : ApiResponse
    {
        [JsonPropertyName("score")]
        public Score Score { get; set; }
    }

    public class Score
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("score")]
        public uint Scores { get; set; }

        [JsonPropertyName("pp")]
        public double PP { get; set; }

        [JsonPropertyName("acc")]
        public double Acc { get; set; }

        [JsonPropertyName("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonPropertyName("mods")]
        public uint Mods { get; set; }

        [JsonPropertyName("n300")]
        public uint Count300 { get; set; }

        [JsonPropertyName("n100")]
        public uint Count100 { get; set; }

        [JsonPropertyName("n50")]
        public uint Count50 { get; set; }

        [JsonPropertyName("nmiss")]
        public uint CountMiss { get; set; }

        [JsonPropertyName("ngeki")]
        public uint CountGeki { get; set; }

        [JsonPropertyName("nkatu")]
        public uint CountKatu { get; set; }

        [JsonPropertyName("grade")]
        public string Rank { get; set; }

        [JsonPropertyName("status")]
        public uint Status { get; set; }

        [JsonPropertyName("mode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("play_time")]
        public DateTimeOffset PlayTime { get; set; }

        [JsonPropertyName("time_elapsed")]
        public int TimeElapsed { get; set; }

        [JsonPropertyName("perfect")]
        [JsonConverter(typeof(AnyBoolConverter))]
        public bool Perfect { get; set; }

        [JsonPropertyName("beatmap")]
        public Beatmap Beatmap { get; set; }
    }

    public class ScoreUser
    {
        [JsonPropertyName("id")]
        public uint Id { get; set; }

        [JsonPropertyName("name")]
        public string Username { get; set; }

        [JsonPropertyName("clan")]
        public object? Clan { get; set; }
    }
}

// {
//     "status": "success",
//     "scores": [
//         {
//             "id": 2324,
//             "score": 10134396,
//             "pp": 111.765,
//             "acc": 84.706,
//             "max_combo": 795,
//             "mods": 8,
//             "n300": 459,
//             "n100": 134,
//             "n50": 2,
//             "nmiss": 0,
//             "ngeki": 26,
//             "nkatu": 48,
//             "grade": "B",
//             "status": 2,
//             "mode": 0,
//             "play_time": "2020-02-18T17:40:57",
//             "time_elapsed": 0,
//             "perfect": 0,
//             "beatmap": {
//                 "md5": "4e392566be350059b31f029ad4d889cf",
//                 "id": 81136,
//                 "set_id": 23754,
//                 "artist": "USBduck",
//                 "title": "Keyboard Cat ZONE",
//                 "version": "Party Time",
//                 "creator": "Kurosanyan",
//                 "last_update": "2012-05-19T09:38:11",
//                 "total_length": 105,
//                 "max_combo": 798,
//                 "status": 2,
//                 "plays": 40,
//                 "passes": 10,
//                 "mode": 0,
//                 "bpm": 165,
//                 "cs": 4,
//                 "od": 7,
//                 "ar": 9,
//                 "hp": 7,
//                 "diff": 5.447
//             }
//         }
//     ],
//     "player": {
//         "id": 1104,
//         "name": "水瓶",
//         "clan": null
//     }
// }
