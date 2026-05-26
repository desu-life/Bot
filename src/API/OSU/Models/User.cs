#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class UserExtended : User {
        [JsonPropertyName("discord")]
        public string? Discord { get; set; }

        [JsonPropertyName("has_supported")]
        public bool HasSupported { get; set; }

        [JsonPropertyName("interests")]
        public string? Interests { get; set; }

        [JsonPropertyName("join_date")]
        public DateTimeOffset JoinDate { get; set; }

        [JsonPropertyName("kudosu")]
        public JsonObject Kudosu { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("max_blocks")]
        public long MaxBlocks { get; set; }

        [JsonPropertyName("max_friends")]
        public long MaxFriends { get; set; }

        [JsonPropertyName("occupation")]
        public string? Occupation { get; set; }

        /// <summary>
        /// 这个是指的官网用户的默认游玩模式，并非查询的成绩模式！！！
        /// 使用时请把此值强制赋值成查询的模式
        /// </summary>
        [JsonPropertyName("playmode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("playstyle")]
        public string[] Playstyle { get; set; }

        [JsonPropertyName("post_count")]
        public long PostCount { get; set; }

        [JsonPropertyName("profile_hue")]
        public long? ProfileHue { get; set; }

        [JsonPropertyName("profile_order")]
        public string[] ProfileOrder { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("title_url")]
        public string? TitleUrl { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("comments_count")]
        public long CommentsCount { get; set; }

        [JsonPropertyName("mapping_follower_count")]
        public long? MappingFollowerCount { get; set; }

        [JsonIgnore]
        public new UserStatistics Statistics => StatisticsCurrent ?? Mode switch
        {
            Mode.OSU => StatisticsModes.Osu,
            Mode.Taiko => StatisticsModes.Taiko,
            Mode.Fruits => StatisticsModes.Catch,
            Mode.Mania => StatisticsModes.Mania,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public class User
    {
        [JsonPropertyName("avatar_url")]
        public Uri AvatarUrl { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("default_group")]
        public string? DefaultGroup { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("is_bot")]
        public bool IsBot { get; set; }

        [JsonPropertyName("is_deleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("is_supporter")]
        public bool IsSupporter { get; set; }

        [JsonPropertyName("last_visit")]
        public DateTimeOffset? LastVisit { get; set; }

        [JsonPropertyName("pm_friends_only")]
        public bool PmFriendsOnly { get; set; }

        [JsonPropertyName("profile_colour")]
        public string? ProfileColor { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        // UserJsonAvailableIncludes

        [JsonPropertyName("account_history")]
        public UserAccountHistory[]? AccountHistory { get; set; }

        [JsonPropertyName("badges")]
        public UserBadge[]? Badges { get; set; }

        [JsonPropertyName("beatmap_playcounts_count")]
        public long? BeatmapPlaycountsCount { get; set; }

        [JsonPropertyName("country")]
        public Country? Country { get; set; }

        [JsonPropertyName("cover")]
        public UserCover? Cover { get; set; }

        [JsonPropertyName("favourite_beatmapset_count")]
        public long? FavouriteBeatmapsetCount { get; set; }

        [JsonPropertyName("follower_count")]
        public long? FollowerCount { get; set; }

        [JsonPropertyName("graveyard_beatmapset_count")]
        public long? GraveyardBeatmapsetCount { get; set; }

        [JsonPropertyName("guest_beatmapset_count")]
        public long? GuestBeatmapsetCount { get; set; }

        [JsonPropertyName("loved_beatmapset_count")]
        public long? LovedBeatmapsetCount { get; set; }

        [JsonPropertyName("pending_beatmapset_count")]
        public long? PendingBeatmapsetCount { get; set; }

        [JsonPropertyName("ranked_beatmapset_count")]
        public long? RankedBeatmapsetCount { get; set; }

        [JsonPropertyName("groups")]
        public UserGroup[]? Groups { get; set; }

        [JsonPropertyName("rank_highest")]
        public UserHighestRank? HighestRank { get; set; }

        [JsonPropertyName("is_admin")]
        public bool? is_admin { get; set; }

        [JsonPropertyName("is_bng")]
        public bool? is_bng { get; set; }

        [JsonPropertyName("is_full_bn")]
        public bool? is_full_bn { get; set; }

        [JsonPropertyName("is_gmt")]
        public bool? is_gmt { get; set; }

        [JsonPropertyName("is_limited_bn")]
        public bool? is_limited_bn { get; set; }

        [JsonPropertyName("is_moderator")]
        public bool? is_moderator { get; set; }

        [JsonPropertyName("is_nat")]
        public bool? is_nat { get; set; }

        [JsonPropertyName("is_restricted")]
        public bool? IsRestricted { get; set; }

        [JsonPropertyName("is_silenced")]
        public bool? is_silenced { get; set; }
        
        [JsonPropertyName("medals")]
        public MedalCompact[]? medals { get; set; }

        [JsonPropertyName("monthly_playcounts")]
        public MonthlyCount[]? monthly_playcounts { get; set; }

        [JsonPropertyName("page")]
        public UserPage? page { get; set; }
        
        [JsonPropertyName("previous_usernames")]
        public string[]? PreviousUsernames { get; set; }

        // 搞不懂为啥这里ppy要给两个rankhistory
        [JsonPropertyName("rank_history")]
        public RankHistory? RankHistory { get; set; }

        [JsonPropertyName("replays_watched_counts")]
        public MonthlyCount[]? replays_watched_counts { get; set; }

        [JsonPropertyName("scores_best_count")]
        public long? ScoresBestCount { get; set; }

        [JsonPropertyName("scores_first_count")]
        public long? ScoresFirstCount { get; set; }

        [JsonPropertyName("scores_recent_count")]
        public long? ScoresRecentCount { get; set; }

        [JsonPropertyName("scores_pinned_count")]
        public long? ScoresPinnedCount { get; set; }

        [JsonPropertyName("statistics")]
        public UserStatistics? StatisticsCurrent { get; set; }
        
        [JsonPropertyName("statistics_rulesets")]
        public UserStatisticsModes StatisticsModes { get; set; }

        [JsonPropertyName("support_level")]
        public long? SupportLevel { get; set; }

        [JsonPropertyName("active_tournament_banners")]
        public JsonArray? ActiveTournamentBanners { get; set; }

        [JsonPropertyName("active_tournament_banner")]
        public JsonObject ActiveTournamentBanner { get; set; }

        [JsonIgnore]
        public UserStatistics Statistics => StatisticsCurrent ?? StatisticsModes.Osu;
    }

    
    public class Country
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("display")]
        public string? Display { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class UserCover
    {
        [JsonPropertyName("custom_url")]
        public Uri? CustomUrl { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class Count
    {
        [JsonPropertyName("start_date")]
        public DateTimeOffset StartDate { get; set; }

        [JsonPropertyName("count")]
        public long CountCount { get; set; }
    }

    public class UserPage
    {
        [JsonPropertyName("html")]
        public string Html { get; set; }

        [JsonPropertyName("raw")]
        public string Raw { get; set; }
    }

    public class UserHighestRank
    {
        [JsonPropertyName("rank")]
        public uint Rank { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class RankHistory
    {
        [JsonPropertyName("mode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("data")]
        public long[] Data { get; set; }
    }


    public class UserStatisticsModes {
        [JsonPropertyName("osu")]
        public UserStatistics Osu { get; set; }

        [JsonPropertyName("taiko")]
        public UserStatistics Taiko { get; set; }

        [JsonPropertyName("fruits")]
        public UserStatistics Catch { get; set; }

        [JsonPropertyName("mania")]
        public UserStatistics Mania { get; set; }
    }

    public class UserStatistics
    {
        [JsonPropertyName("level")]
        public UserLevel Level { get; set; }

        [JsonPropertyName("global_rank")]
        public long? GlobalRank { get; set; }

        [JsonPropertyName("pp")]
        public double PP { get; set; }

        [JsonPropertyName("ranked_score")]
        public long RankedScore { get; set; }

        [JsonPropertyName("hit_accuracy")]
        public double HitAccuracy { get; set; }

        [JsonPropertyName("play_count")]
        public long PlayCount { get; set; }

        [JsonPropertyName("play_time")]
        public long PlayTime { get; set; }

        [JsonPropertyName("total_score")]
        public long TotalScore { get; set; }

        [JsonPropertyName("total_hits")]
        public long TotalHits { get; set; }

        [JsonPropertyName("maximum_combo")]
        public long MaximumCombo { get; set; }

        [JsonPropertyName("replays_watched_by_others")]
        public long ReplaysWatchedByOthers { get; set; }

        [JsonPropertyName("is_ranked")]
        public bool IsRanked { get; set; }

        [JsonPropertyName("grade_counts")]
        public UserGradeCounts GradeCounts { get; set; }

        [JsonPropertyName("country_rank")]
        public long? CountryRank { get; set; }

        [JsonPropertyName("rank")]
        public UserRank Rank { get; set; }
    }

    public class UserGradeCounts
    {
        [JsonPropertyName("ss")]
        public int SS { get; set; }

        [JsonPropertyName("ssh")]
        public int SSH { get; set; }

        [JsonPropertyName("s")]
        public int S { get; set; }

        [JsonPropertyName("sh")]
        public int SH { get; set; }

        [JsonPropertyName("a")]
        public int A { get; set; }
    }

    public class UserGroup
    {
        [JsonPropertyName("colour")]
        public string Color { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("has_playmodes")]
        public bool HasModes { get; set; }

        [JsonPropertyName("id")]
        public uint Id { get; set; }

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("is_probationary")]
        public bool IsProbationary { get; set; }

        [JsonPropertyName("playmodes")]
        public Mode[]? Modes { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }
    }
    public class UserLevel
    {
        [JsonPropertyName("current")]
        public int Current { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; }
    }

    public class MonthlyCount
    {
        [JsonPropertyName("start_date")]
        public string start_date { get; set; }

        [JsonPropertyName("count")]
        public int count { get; set; }
    }

    public class UserRank
    {
        [JsonPropertyName("country")]
        public long? Country { get; set; }
    }

    public class UserAchievement
    {
        [JsonPropertyName("achieved_at")]
        public DateTimeOffset AchievedAt { get; set; }

        [JsonPropertyName("achievement_id")]
        public long AchievementId { get; set; }
    }

    public class UserBadge
    {
        [JsonPropertyName("awarded_at")]
        public DateTimeOffset AwardedAt { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }

    public class UserAccountHistory
    {
        [JsonPropertyName("id")]
        public uint? Id { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Time { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public HistoryType HistoryType { get; set; }

        [JsonPropertyName("length")]
        public uint Seconds { get; set; }

        [JsonPropertyName("permanent")]
        public bool Permanent { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter<HistoryType>))]
    public enum HistoryType
    {
        [JsonStringEnumMemberName("note")]
        Note,

        [JsonStringEnumMemberName("restriction")]
        Restriction,

        [JsonStringEnumMemberName("tournament_ban")]
        TournamentBan,

        [JsonStringEnumMemberName("silence")]
        Silence,
    }
}