#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class User
    {
        [JsonProperty("avatar_url")]
        public Uri AvatarUrl { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }

        [JsonProperty("default_group")]
        public string DefaultGroup { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("is_bot")]
        public bool IsBot { get; set; }

        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("is_online")]
        public bool IsOnline { get; set; }

        [JsonProperty("is_supporter")]
        public bool IsSupporter { get; set; }

        [JsonProperty("last_visit", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? LastVisit { get; set; }

        [JsonProperty("pm_friends_only")]
        public bool PmFriendsOnly { get; set; }

        [JsonProperty("profile_colour", NullValueHandling = NullValueHandling.Ignore)]
        public string? ProfileColour { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("cover_url")]
        public Uri CoverUrl { get; set; }

        [JsonProperty("discord", NullValueHandling = NullValueHandling.Ignore)]
        public string? Discord { get; set; }

        [JsonProperty("has_supported")]
        public bool HasSupported { get; set; }

        [JsonProperty("interests", NullValueHandling = NullValueHandling.Ignore)]
        public string? Interests { get; set; }

        [JsonProperty("join_date")]
        public DateTimeOffset JoinDate { get; set; }

        [JsonProperty("kudosu")]
        public JObject Kudosu { get; set; }

        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public string? Location { get; set; }

        [JsonProperty("max_blocks")]
        public long MaxBlocks { get; set; }

        [JsonProperty("max_friends")]
        public long MaxFriends { get; set; }

        [JsonProperty("occupation", NullValueHandling = NullValueHandling.Ignore)]
        public string? Occupation { get; set; }

        /// <summary>
        /// 这个是指的官网用户的默认游玩模式，并非查询的成绩模式！！！
        /// 使用时请把此值强制赋值成查询的模式
        /// </summary>
        [JsonProperty("playmode")]
        [JsonConverter(typeof(JsonEnumConverter))]
        public Mode PlayMode { get; set; }

        [JsonProperty("playstyle")]
        public string[] Playstyle { get; set; }

        [JsonProperty("post_count")]
        public long PostCount { get; set; }

        [JsonProperty("profile_order")]
        public string[] ProfileOrder { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        [JsonProperty("title_url", NullValueHandling = NullValueHandling.Ignore)]
        public string? TitleUrl { get; set; }

        [JsonProperty("twitter", NullValueHandling = NullValueHandling.Ignore)]
        public string? Twitter { get; set; }

        [JsonProperty("website", NullValueHandling = NullValueHandling.Ignore)]
        public string? Website { get; set; }

        [JsonProperty("country")]
        public Country Country { get; set; }

        [JsonProperty("cover")]
        public UserCover Cover { get; set; }

        [JsonProperty("account_history")]
        public UserAccountHistory[] AccountHistory { get; set; }

        [JsonProperty("active_tournament_banners", NullValueHandling = NullValueHandling.Ignore)]
        public JArray? ActiveTournamentBanners { get; set; }

        [JsonProperty("badges")]
        public UserBadge[] Badges { get; set; }

        [JsonProperty("beatmap_playcounts_count", NullValueHandling = NullValueHandling.Ignore)]
        public long BeatmapPlaycountsCount { get; set; }

        [JsonProperty("comments_count")]
        public long CommentsCount { get; set; }

        [JsonProperty("favourite_beatmapset_count")]
        public long FavouriteBeatmapsetCount { get; set; }

        [JsonProperty("follower_count")]
        public long FollowerCount { get; set; }

        [JsonProperty("graveyard_beatmapset_count")]
        public long GraveyardBeatmapsetCount { get; set; }

        [JsonProperty("groups")]
        public JArray Groups { get; set; }

        [JsonProperty("guest_beatmapset_count")]
        public long GuestBeatmapsetCount { get; set; }

        [JsonProperty("loved_beatmapset_count")]
        public long LovedBeatmapsetCount { get; set; }

        [JsonProperty("mapping_follower_count")]
        public long MappingFollowerCount { get; set; }

        [JsonProperty("pending_beatmapset_count")]
        public long PendingBeatmapsetCount { get; set; }

        [JsonProperty("previous_usernames")]
        public string[] PreviousUsernames { get; set; }

        [JsonProperty("ranked_beatmapset_count")]
        public long RankedBeatmapsetCount { get; set; }

        [JsonProperty("scores_best_count")]
        public long ScoresBestCount { get; set; }

        [JsonProperty("scores_first_count")]
        public long ScoresFirstCount { get; set; }

        [JsonProperty("scores_pinned_count")]
        public long ScoresPinnedCount { get; set; }

        [JsonProperty("scores_recent_count")]
        public long ScoresRecentCount { get; set; }

        [JsonProperty("is_restricted", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsRestricted { get; set; }

        [JsonProperty("statistics")]
        public UserStatistics Statistics { get; set; }

        [JsonProperty("support_level")]
        public long SupportLevel { get; set; }

        [JsonProperty("ranked_and_approved_beatmapset_count")]
        public long RankedAndApprovedBeatmapsetCount { get; set; }

        [JsonProperty("unranked_beatmapset_count")]
        public long UnrankedBeatmapsetCount { get; set; }

        // 搞不懂为啥这里ppy要给两个rankhistory
        [JsonProperty("rankHistory")]
        public RankHistory? RankHistory { get; set; }
    }

    
    public class Country
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class UserCover
    {
        [JsonProperty("custom_url")]
        public Uri CustomUrl { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public JValue? Id { get; set; }
    }

    public class Count
    {
        [JsonProperty("start_date")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("count")]
        public long CountCount { get; set; }
    }

    public class UserPage
    {
        [JsonProperty("html")]
        public string Html { get; set; }

        [JsonProperty("raw")]
        public string Raw { get; set; }
    }

    public class RankHistory
    {
        [JsonProperty("mode")]
        [JsonConverter(typeof(JsonEnumConverter))]
        public Mode Mode { get; set; }

        [JsonProperty("data")]
        public long[] Data { get; set; }
    }


    public class UserStatistics
    {
        [JsonProperty("level")]
        public UserLevel Level { get; set; }

        [JsonProperty("global_rank", NullValueHandling = NullValueHandling.Ignore)]
        public long GlobalRank { get; set; }

        [JsonProperty("pp")]
        public double PP { get; set; }

        [JsonProperty("ranked_score")]
        public long RankedScore { get; set; }

        [JsonProperty("hit_accuracy")]
        public double HitAccuracy { get; set; }

        [JsonProperty("play_count")]
        public long PlayCount { get; set; }

        [JsonProperty("play_time")]
        public long PlayTime { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("total_hits")]
        public long TotalHits { get; set; }

        [JsonProperty("maximum_combo")]
        public long MaximumCombo { get; set; }

        [JsonProperty("replays_watched_by_others")]
        public long ReplaysWatchedByOthers { get; set; }

        [JsonProperty("is_ranked")]
        public bool IsRanked { get; set; }

        [JsonProperty("grade_counts")]
        public UserGradeCounts GradeCounts { get; set; }

        [JsonProperty("country_rank", NullValueHandling = NullValueHandling.Ignore)]
        public long CountryRank { get; set; }

        [JsonProperty("rank")]
        public UserRank Rank { get; set; }
    }

    public class UserGradeCounts
    {
        [JsonProperty("ss")]
        public int SS { get; set; }

        [JsonProperty("ssh")]
        public int SSH { get; set; }

        [JsonProperty("s")]
        public int S { get; set; }

        [JsonProperty("sh")]
        public int SH { get; set; }

        [JsonProperty("a")]
        public int A { get; set; }
    }

    public class UserLevel
    {
        [JsonProperty("current")]
        public int Current { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }
    }

    public class UserRank
    {
        [JsonProperty("country", NullValueHandling = NullValueHandling.Ignore)]
        public int Country { get; set; }
    }

    public class UserAchievement
    {
        [JsonProperty("achieved_at")]
        public DateTimeOffset AchievedAt { get; set; }

        [JsonProperty("achievement_id")]
        public long AchievementId { get; set; }
    }

    public class UserBadge
    {
        [JsonProperty("awarded_at")]
        public DateTimeOffset AwardedAt { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public class UserAccountHistory
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Time { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }
    }

}