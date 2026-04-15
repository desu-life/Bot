using KanonBot.Serializer;
using Newtonsoft.Json;

namespace KanonBot.API.Kagami;

public class KanonBotProfile
{
    public string UserId { get; set; } = "";
    public KanonBotSettings? KanonBot { get; set; }
    public List<InstalledBadge> InstalledBadges { get; set; } = [];
    public int BadgeLimit { get; set; } = 5;
}

public class KanonBotSettings
{
    public string infoPanelDefaultVersion { get; set; } = "v2";
    public string InfoPanelV2ColorMode { get; set; } = "light";
    public string PreferredGameMode { get; set; } = "osu";
    public string PpySbPreferredGameMode { get; set; } = "osu";
}

public class InstalledBadge
{
    public string UserBadgeId { get; set; } = "";
    public string BadgeId { get; set; } = "";
    public string NameZh { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Summary { get; set; } = "";
    public int WearSortOrder { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset GrantedAt { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class InfoPanelV2CustomTheme
{
    // Colors
    public string? UsernameColor { get; set; }
    public string? ModeIconColor { get; set; }
    public string? RankColor { get; set; }
    public string? CountryRankColor { get; set; }
    public string? CountryRankDiffColor { get; set; }
    public string? CountryRankDiffIconColor { get; set; }
    public string? RankLineChartColor { get; set; }
    public string? RankLineChartTextColor { get; set; }
    public string? RankLineChartDotColor { get; set; }
    public string? RankLineChartDotStrokeColor { get; set; }
    public string? RankLineChartDashColor { get; set; }
    public string? RankLineChartDateTextColor { get; set; }
    public string? ppMainColor { get; set; }
    public string? ppDiffColor { get; set; }
    public string? ppDiffIconColor { get; set; }
    public string? ppProgressBarColorTextColor { get; set; }
    public string? ppProgressBarColor { get; set; }
    public string? ppProgressBarBackgroundColor { get; set; }
    public string? accMainColor { get; set; }
    public string? accDiffColor { get; set; }
    public string? accDiffIconColor { get; set; }
    public string? accProgressBarColorTextColor { get; set; }
    public string? accProgressBarColor { get; set; }
    public string? accProgressBarBackgroundColor { get; set; }
    public string? GradeStatisticsColor_XH { get; set; }
    public string? GradeStatisticsColor_X { get; set; }
    public string? GradeStatisticsColor_SH { get; set; }
    public string? GradeStatisticsColor_S { get; set; }
    public string? GradeStatisticsColor_A { get; set; }
    public string? Details_PlayTimeColor { get; set; }
    public string? Details_TotalHitsColor { get; set; }
    public string? Details_PlayCountColor { get; set; }
    public string? Details_RankedScoreColor { get; set; }
    public string? DetailsDiff_PlayTimeColor { get; set; }
    public string? DetailsDiff_TotalHitsColor { get; set; }
    public string? DetailsDiff_PlayCountColor { get; set; }
    public string? DetailsDiff_RankedScoreColor { get; set; }
    public string? DetailsDiff_PlayTimeIconColor { get; set; }
    public string? DetailsDiff_TotalHitsIconColor { get; set; }
    public string? DetailsDiff_PlayCountIconColor { get; set; }
    public string? DetailsDiff_RankedScoreIconColor { get; set; }
    public string? LevelTitleColor { get; set; }
    public string? LevelProgressBarColor { get; set; }
    public string? LevelProgressBarBackgroundColor { get; set; }
    public string? MainBPTitleColor { get; set; }
    public string? MainBPArtistColor { get; set; }
    public string? MainBPMapperColor { get; set; }
    public string? MainBPBIDColor { get; set; }
    public string? MainBPStarsColor { get; set; }
    public string? MainBPAccColor { get; set; }
    public string? MainBPRankColor { get; set; }
    public string? MainBPppMainColor { get; set; }
    public string? MainBPppTitleColor { get; set; }
    public string? SubBp2ndModeColor { get; set; }
    public string? SubBp2ndBPTitleColor { get; set; }
    public string? SubBp2ndBPVersionColor { get; set; }
    public string? SubBp2ndBPBIDColor { get; set; }
    public string? SubBp2ndBPStarsColor { get; set; }
    public string? SubBp2ndBPAccColor { get; set; }
    public string? SubBp2ndBPRankColor { get; set; }
    public string? SubBp2ndBPppMainColor { get; set; }
    public string? SubBp3rdModeColor { get; set; }
    public string? SubBp3rdBPTitleColor { get; set; }
    public string? SubBp3rdBPVersionColor { get; set; }
    public string? SubBp3rdBPBIDColor { get; set; }
    public string? SubBp3rdBPStarsColor { get; set; }
    public string? SubBp3rdBPAccColor { get; set; }
    public string? SubBp3rdBPRankColor { get; set; }
    public string? SubBp3rdBPppMainColor { get; set; }
    public string? SubBp4thModeColor { get; set; }
    public string? SubBp4thBPTitleColor { get; set; }
    public string? SubBp4thBPVersionColor { get; set; }
    public string? SubBp4thBPBIDColor { get; set; }
    public string? SubBp4thBPStarsColor { get; set; }
    public string? SubBp4thBPAccColor { get; set; }
    public string? SubBp4thBPRankColor { get; set; }
    public string? SubBp4thBPppMainColor { get; set; }
    public string? SubBp5thModeColor { get; set; }
    public string? SubBp5thBPTitleColor { get; set; }
    public string? SubBp5thBPVersionColor { get; set; }
    public string? SubBp5thBPBIDColor { get; set; }
    public string? SubBp5thBPStarsColor { get; set; }
    public string? SubBp5thBPAccColor { get; set; }
    public string? SubBp5thBPRankColor { get; set; }
    public string? SubBp5thBPppMainColor { get; set; }
    public string? SubBpInfoSplitColor { get; set; }
    public string? footerColor { get; set; }
    
    // Float values (stored as strings for JSON compatibility)
    public string? SideImgBrightness { get; set; }
    public string? AvatarBrightness { get; set; }
    public string? BadgeBrightness { get; set; }
    public string? MainBPImgBrightness { get; set; }
    public string? CountryFlagBrightness { get; set; }
    public string? ModeCaptionBrightness { get; set; }
    public string? ModIconBrightness { get; set; }
    public string? ScoreModeIconBrightness { get; set; }
    public string? OsuSupporterIconBrightness { get; set; }
    public string? CountryFlagAlpha { get; set; }
    public string? OsuSupporterIconAlpha { get; set; }
    public string? BadgeAlpha { get; set; }
    public string? AvatarAlpha { get; set; }
    public string? ModIconAlpha { get; set; }
    
    // Bool values (stored as strings for JSON compatibility)
    public string? FixedScoreModeIconColor { get; set; }
    public string? DisplaySupporterStatus { get; set; }
    public string? ModeIconAlpha { get; set; }
    public string? Score1ModeIconAlpha { get; set; }
    public string? Score2ModeIconAlpha { get; set; }
    public string? Score3ModeIconAlpha { get; set; }
    public string? Score4ModeIconAlpha { get; set; }
    public string? DetailsPlaytimeIconAlpha { get; set; }
    public string? DetailsTotalHitIconAlpha { get; set; }
    public string? DetailsPlayCountIconAlpha { get; set; }
    public string? DetailsRankedScoreIconAlpha { get; set; }
    public string? ppDiffIconColorAlpha { get; set; }
    public string? accDiffIconColorAlpha { get; set; }
    public string? CountryRankDiffIconColorAlpha { get; set; }
}

public class KanonImages
{
    public string UserId { get; set; } = "";
    public string? InfoPanelV1ImageUrl { get; set; }
    public string? InfoPanelV2ImageUrl { get; set; }
    public string? InfoPanelV1CoverImageUrl { get; set; }
    public string? InfoPanelV2CoverImageUrl { get; set; }
    public InfoPanelV2CustomTheme? infoPanelV2CustomThemeJson { get; set; }
    public string? InfoPanelV2ColorMode { get; set; }
    public string? InfoPanelDefaultVersion { get; set; }
    public string? PreferredGameMode { get; set; }
    public string? PpySbPreferredGameMode { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class UserBadgeResponse
{
    public string UserBadgeId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string BadgeId { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string NameZh { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? ImageUrl { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset GrantedAt { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset? ExpiresAt { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsExpired { get; set; }
    public int? WearSortOrder { get; set; }
}

public class UserPermissionsResponse
{
    public string UserId { get; set; } = "";
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}
