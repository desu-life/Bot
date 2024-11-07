#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class UserResponse : ApiResponse {
        [JsonProperty("player")]
        public User Player { get; set; }
    }

    public class User {

        [JsonProperty("info")]
        public UserInfo Info { get; set; }

        [JsonProperty("stats")]
        public UserStats Stats { get; set; }
    }

    public class UserInfo {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("safe_name")]
        public string SafeName { get; set; }

        [JsonProperty("priv")]
        public Privileges Privileges { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("silence_end")]
        public uint SilenceEnd { get; set; }

        [JsonProperty("donor_end")]
        public uint DonorEnd { get; set; }

        [JsonProperty("creation_time")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset CreationTime { get; set; }

        [JsonProperty("latest_activity")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset LatestActivity { get; set; }

        [JsonProperty("clan_id")]
        public uint ClanId { get; set; }

        [JsonProperty("clan_priv")]
        public uint ClanPrivileges { get; set; }

        [JsonProperty("preferred_mode")]
        public Mode PreferredMode { get; set; }

        [JsonProperty("play_style")]
        public uint PlayStyle { get; set; }

        [JsonProperty("custom_badge_name")]
        public string? CustomBadgeName { get; set; }

        [JsonProperty("custom_badge_icon")]
        public string? CustomBadgeIcon { get; set; }

        [JsonProperty("userpage_content")]
        public string? UserPageContent { get; set; }
    }

    public class UserStats {
        [JsonProperty("0")]
        public UserStat? StatOsu { get; set; }

        [JsonProperty("1")]
        public UserStat? StatTaiko { get; set; }

        [JsonProperty("2")]
        public UserStat? StatFruits { get; set; }

        [JsonProperty("3")]
        public UserStat? StatMania { get; set; }

        [JsonProperty("4")]
        public UserStat? StatRelaxOsu { get; set; }

        [JsonProperty("5")]
        public UserStat? StatRelaxTaiko { get; set; }

        [JsonProperty("6")]
        public UserStat? StatRelaxFruits { get; set; }

        [JsonProperty("8")]
        public UserStat? StatAutoPilotOsu { get; set; }
    }
    public class UserStat {

        [JsonProperty("id")]
        public uint Id { get; set; }

        [JsonProperty("mode")]
        public Mode Mode { get; set; }

        [JsonProperty("tscore")]
        public long TotalScore { get; set; }

        [JsonProperty("rscore")]
        public long RankedScore { get; set; }

        [JsonProperty("pp")]
        public uint PP { get; set; }

        [JsonProperty("plays")]
        public uint Plays { get; set; }

        [JsonProperty("playtime")]
        public uint PlayTime { get; set; }

        [JsonProperty("acc")]
        public double Accuracy { get; set; }

        [JsonProperty("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonProperty("total_hits")]
        public long TotalHits { get; set; }

        [JsonProperty("replay_views")]
        public uint ReplayViews { get; set; }

        [JsonProperty("xh_count")]
        public int XhCount { get; set; }

        [JsonProperty("x_count")]
        public int XCount { get; set; }

        [JsonProperty("sh_count")]
        public int ShCount { get; set; }

        [JsonProperty("s_count")]
        public int SCount { get; set; }

        [JsonProperty("a_count")]
        public int ACount { get; set; }

        [JsonProperty("rank")]
        public long Rank { get; set; }

        [JsonProperty("country_rank")]
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