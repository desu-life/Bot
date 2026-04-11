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

public class KanonImages
{
    public string UserId { get; set; } = "";
    public string? InfoPanelV1ImageUrl { get; set; }
    public string? InfoPanelV2ImageUrl { get; set; }
    public string? InfoPanelV1CoverImageUrl { get; set; }
    public string? InfoPanelV2CoverImageUrl { get; set; }
    public string? infoPanelV2CustomThemeJson { get; set; }
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
