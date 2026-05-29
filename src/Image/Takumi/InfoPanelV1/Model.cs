using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KanonBot.Image.Takumi;

public record InfoPanelV1Context
{
    [JsonPropertyName("cover_src")] public required string CoverSrc { get; init; }
    [JsonPropertyName("panel_src")] public required string PanelSrc { get; init; }
    [JsonPropertyName("avatar_src")] public required string AvatarSrc { get; init; }
    [JsonPropertyName("flag_src")] public required string FlagSrc { get; init; }
    [JsonPropertyName("mode_icon_src")] public required string ModeIconSrc { get; init; }

    [JsonPropertyName("pp_plus_panel_src")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PpPlusPanelSrc { get; init; }

    [JsonPropertyName("no_pp_plus_panel_src")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NoPpPlusPanelSrc { get; init; }

    [JsonPropertyName("badges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BadgeModel>? Badges { get; init; }

    [JsonPropertyName("hex_svg_points")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HexSvgPoints { get; init; }

    [JsonPropertyName("hex_fill_color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HexFillColor { get; init; }

    [JsonPropertyName("hex_stroke_color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HexStrokeColor { get; init; }

    [JsonPropertyName("pp_plus_labels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PpPlusLabelModel>? PpPlusLabels { get; init; }

    [JsonPropertyName("update_time_display")] public required string UpdateTimeDisplay { get; init; }

    [JsonPropertyName("days_before_display")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DaysBeforeDisplay { get; init; }

    [JsonPropertyName("username_display")] public required string UsernameDisplay { get; init; }
    [JsonPropertyName("country_rank_display")] public required string CountryRankDisplay { get; init; }
    [JsonPropertyName("global_rank_display")] public required string GlobalRankDisplay { get; init; }
    [JsonPropertyName("rank_diff_display")] public required string RankDiffDisplay { get; init; }
    [JsonPropertyName("pp_display")] public required string PpDisplay { get; init; }
    [JsonPropertyName("pp_diff_display")] public required string PpDiffDisplay { get; init; }
    [JsonPropertyName("ssh_display")] public required string SshDisplay { get; init; }
    [JsonPropertyName("ss_display")] public required string SsDisplay { get; init; }
    [JsonPropertyName("sh_display")] public required string ShDisplay { get; init; }
    [JsonPropertyName("s_display")] public required string SDisplay { get; init; }
    [JsonPropertyName("a_display")] public required string ADisplay { get; init; }
    [JsonPropertyName("level_display")] public required string LevelDisplay { get; init; }
    [JsonPropertyName("level_percent_display")] public required string LevelPercentDisplay { get; init; }
    [JsonPropertyName("level_progress")] public required int LevelProgress { get; init; }
    [JsonPropertyName("ranked_score_display")] public required string RankedScoreDisplay { get; init; }
    [JsonPropertyName("accuracy_display")] public required string AccuracyDisplay { get; init; }
    [JsonPropertyName("play_count_display")] public required string PlayCountDisplay { get; init; }
    [JsonPropertyName("total_score_display")] public required string TotalScoreDisplay { get; init; }
    [JsonPropertyName("total_hits_display")] public required string TotalHitsDisplay { get; init; }
    [JsonPropertyName("play_time_display")] public required string PlayTimeDisplay { get; init; }
}

public record BadgeModel
{
    [JsonPropertyName("src")] public required string Src { get; init; }
}

public record PpPlusLabelModel
{
    [JsonPropertyName("value")] public required string Value { get; init; }
    [JsonPropertyName("x")] public required double X { get; init; }
    [JsonPropertyName("y")] public required double Y { get; init; }
}
