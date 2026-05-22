#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class UserResponse : ApiResponse {
        [JsonPropertyName("player")]
        public User Player { get; set; }
    }

    public class User {

        [JsonPropertyName("info")]
        public UserInfo Info { get; set; }

        [JsonPropertyName("stats")]
        public UserStats Stats { get; set; }

        [JsonIgnore]
        public OSU.Models.UserExtended? _LazerUser { get; set; } = null;
    }

    public class UserInfo {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("safe_name")]
        public string SafeName { get; set; }

        [JsonPropertyName("priv")]
        public Privileges Privileges { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("silence_end")]
        public uint SilenceEnd { get; set; }

        [JsonPropertyName("donor_end")]
        public uint DonorEnd { get; set; }

        [JsonPropertyName("creation_time")]
        [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
        public DateTimeOffset CreationTime { get; set; }

        [JsonPropertyName("latest_activity")]
        [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
        public DateTimeOffset LatestActivity { get; set; }

        [JsonPropertyName("clan_id")]
        public uint ClanId { get; set; }

        [JsonPropertyName("clan_priv")]
        public uint ClanPrivileges { get; set; }

        [JsonPropertyName("preferred_mode")]
        public Mode PreferredMode { get; set; }

        [JsonPropertyName("play_style")]
        public uint PlayStyle { get; set; }

        [JsonPropertyName("custom_badge_name")]
        public string? CustomBadgeName { get; set; }

        [JsonPropertyName("custom_badge_icon")]
        public string? CustomBadgeIcon { get; set; }

        [JsonPropertyName("userpage_content")]
        public string? UserPageContent { get; set; }
    }

    public class UserStats {
        [JsonPropertyName("0")]
        public UserStat? StatOsu { get; set; }

        [JsonPropertyName("1")]
        public UserStat? StatTaiko { get; set; }

        [JsonPropertyName("2")]
        public UserStat? StatFruits { get; set; }

        [JsonPropertyName("3")]
        public UserStat? StatMania { get; set; }

        [JsonPropertyName("4")]
        public UserStat? StatRelaxOsu { get; set; }

        [JsonPropertyName("5")]
        public UserStat? StatRelaxTaiko { get; set; }

        [JsonPropertyName("6")]
        public UserStat? StatRelaxFruits { get; set; }

        [JsonPropertyName("8")]
        public UserStat? StatAutoPilotOsu { get; set; }
    }
    public class UserStat {

        [JsonPropertyName("id")]
        public uint Id { get; set; }

        [JsonPropertyName("mode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("tscore")]
        public long TotalScore { get; set; }

        [JsonPropertyName("rscore")]
        public long RankedScore { get; set; }

        [JsonPropertyName("pp")]
        public uint PP { get; set; }

        [JsonPropertyName("plays")]
        public uint Plays { get; set; }

        [JsonPropertyName("playtime")]
        public uint PlayTime { get; set; }

        [JsonPropertyName("acc")]
        public double Accuracy { get; set; }

        [JsonPropertyName("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonPropertyName("total_hits")]
        public long TotalHits { get; set; }

        [JsonPropertyName("replay_views")]
        public uint ReplayViews { get; set; }

        [JsonPropertyName("xh_count")]
        public int XhCount { get; set; }

        [JsonPropertyName("x_count")]
        public int XCount { get; set; }

        [JsonPropertyName("sh_count")]
        public int ShCount { get; set; }

        [JsonPropertyName("s_count")]
        public int SCount { get; set; }

        [JsonPropertyName("a_count")]
        public int ACount { get; set; }

        [JsonPropertyName("rank")]
        public long Rank { get; set; }

        [JsonPropertyName("country_rank")]
        public long CountryRank { get; set; }
    }
}

// {
//     "status": "success",
//     "player": {
//         "info": {
//             "id": 1104,
//             "name": "水瓶",
//             "safe_name": "水瓶",
//             "priv": 3,
//             "country": "cn",
//             "silence_end": 0,
//             "donor_end": 0,
//             "creation_time": 1581848730,
//             "latest_activity": 1588664676,
//             "clan_id": 0,
//             "clan_priv": 0,
//             "preferred_mode": 0,
//             "play_style": 0,
//             "custom_badge_name": null,
//             "custom_badge_icon": null,
//             "userpage_content": null
//         },
//         "stats": {
//             "0": {
//                 "id": 1104,
//                 "mode": 0,
//                 "tscore": 235885842,
//                 "rscore": 174523148,
//                 "pp": 1132,
//                 "plays": 86,
//                 "playtime": 0,
//                 "acc": 88.042,
//                 "max_combo": 1020,
//                 "total_hits": 53027,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 1,
//                 "s_count": 0,
//                 "a_count": 9,
//                 "rank": 635,
//                 "country_rank": 0
//             },
//             "1": {
//                 "id": 1104,
//                 "mode": 1,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             },
//             "2": {
//                 "id": 1104,
//                 "mode": 2,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             },
//             "3": {
//                 "id": 1104,
//                 "mode": 3,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             },
//             "4": {
//                 "id": 1104,
//                 "mode": 4,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             },
//             "5": {
//                 "id": 1104,
//                 "mode": 5,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             },
//             "6": {
//                 "id": 1104,
//                 "mode": 6,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             },
//             "8": {
//                 "id": 1104,
//                 "mode": 8,
//                 "tscore": 0,
//                 "rscore": 0,
//                 "pp": 0,
//                 "plays": 0,
//                 "playtime": 0,
//                 "acc": 100,
//                 "max_combo": 0,
//                 "total_hits": 0,
//                 "replay_views": 0,
//                 "xh_count": 0,
//                 "x_count": 0,
//                 "sh_count": 0,
//                 "s_count": 0,
//                 "a_count": 0,
//                 "rank": 0,
//                 "country_rank": 0
//             }
//         }
//     }
// }