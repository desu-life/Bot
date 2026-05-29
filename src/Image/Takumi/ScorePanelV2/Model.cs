using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KanonBot.Image.Takumi;

public record ScorePanelContext
{
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("map_bg_src")]
    public required string MapBgSrc { get; init; }

    [JsonPropertyName("avatar_src")]
    public required string AvatarSrc { get; init; }

    [JsonPropertyName("panel_src")]
    public required string PanelSrc { get; init; }

    [JsonPropertyName("ranking_src")]
    public required string RankingSrc { get; init; }

    [JsonPropertyName("difficulty_ring")]
    public required DifficultyRingModel DifficultyRing { get; init; }

    [JsonPropertyName("title_display")]
    public required string TitleDisplay { get; init; }

    [JsonPropertyName("artist_display")]
    public required string ArtistDisplay { get; init; }

    [JsonPropertyName("creator_display")]
    public required string CreatorDisplay { get; init; }

    [JsonPropertyName("beatmap_id_display")]
    public required string BeatmapIdDisplay { get; init; }

    [JsonPropertyName("song_time_display")]
    public required string SongTimeDisplay { get; init; }

    [JsonPropertyName("bpm_display")]
    public required string BpmDisplay { get; init; }

    [JsonPropertyName("ar_display")]
    public required string ArDisplay { get; init; }

    [JsonPropertyName("od_display")]
    public required string OdDisplay { get; init; }

    [JsonPropertyName("cs_display")]
    public required string CsDisplay { get; init; }

    [JsonPropertyName("hp_display")]
    public required string HpDisplay { get; init; }

    [JsonPropertyName("star_display")]
    public required string StarDisplay { get; init; }

    [JsonPropertyName("version_display")]
    public required string VersionDisplay { get; init; }

    [JsonPropertyName("username_display")]
    public required string UsernameDisplay { get; init; }

    [JsonPropertyName("ended_at_display")]
    public required string EndedAtDisplay { get; init; }

    [JsonPropertyName("pp_if_fc_value")]
    public required string PpIfFcValue { get; init; }

    [JsonPropertyName("total_pp_value")]
    public required string TotalPpValue { get; init; }

    [JsonPropertyName("score_display")]
    public required string ScoreDisplay { get; init; }

    [JsonPropertyName("accuracy_display")]
    public required string AccuracyDisplay { get; init; }

    [JsonPropertyName("accuracy_hue_deg")]
    public required double AccuracyHueDeg { get; init; }

    [JsonPropertyName("combo_current_display")]
    public required string ComboCurrentDisplay { get; init; }

    [JsonPropertyName("combo_hue_deg")]
    public required double ComboHueDeg { get; init; }

    [JsonPropertyName("pp_forecast_values")]
    public required List<string> PpForecastValues { get; init; }

    [JsonPropertyName("pp_breakdown_values")]
    public required List<string> PpBreakdownValues { get; init; }

    [JsonPropertyName("judgements")]
    public required List<JudgementModel> Judgements { get; init; }

    [JsonPropertyName("server")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Server { get; init; }

    [JsonPropertyName("status_icon_src")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusIconSrc { get; init; }

    [JsonPropertyName("is_lazer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsLazer { get; init; }

    [JsonPropertyName("combo_max_display")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ComboMaxDisplay { get; init; }

    [JsonPropertyName("old_pp_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OldPpValue { get; init; }

    [JsonPropertyName("mods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ModModel>? Mods { get; init; }
}

public record DifficultyRingModel
{
    [JsonPropertyName("color")]
    public required string Color { get; init; }

    [JsonPropertyName("cover_src")]
    public required string CoverSrc { get; init; }

    [JsonPropertyName("ring_src")]
    public required string RingSrc { get; init; }
}

public record JudgementModel
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("x")]
    public required int X { get; init; }

    [JsonPropertyName("y")]
    public required int Y { get; init; }

    [JsonPropertyName("size")]
    public required double Size { get; init; }
}

public record ModModel
{
    [JsonPropertyName("acronym")]
    public required string Acronym { get; init; }

    [JsonPropertyName("icon_src")]
    public required string IconSrc { get; init; }

    [JsonPropertyName("fallback_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FallbackText { get; init; }

    [JsonPropertyName("bar_display")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BarDisplay { get; init; }

    [JsonPropertyName("overlay_color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OverlayColor { get; init; }
}
