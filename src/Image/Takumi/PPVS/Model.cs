using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KanonBot.Image.Takumi;

public record PPVSContext
{
    [JsonPropertyName("background_src")]
    public required string BackgroundSrc { get; init; }

    [JsonPropertyName("title_src")]
    public required string TitleSrc { get; init; }

    [JsonPropertyName("u1_svg_points")]
    public required string U1SvgPoints { get; init; }

    [JsonPropertyName("u2_svg_points")]
    public required string U2SvgPoints { get; init; }

    [JsonPropertyName("u1_fill_color")]
    public required string U1FillColor { get; init; }

    [JsonPropertyName("u1_stroke_color")]
    public required string U1StrokeColor { get; init; }

    [JsonPropertyName("u2_fill_color")]
    public required string U2FillColor { get; init; }

    [JsonPropertyName("u2_stroke_color")]
    public required string U2StrokeColor { get; init; }

    [JsonPropertyName("u1_draw_first")]
    public required bool U1DrawFirst { get; init; }

    [JsonPropertyName("u1_name")]
    public required string U1Name { get; init; }

    [JsonPropertyName("u2_name")]
    public required string U2Name { get; init; }

    [JsonPropertyName("u1_values")]
    public required List<string> U1Values { get; init; }

    [JsonPropertyName("u2_values")]
    public required List<string> U2Values { get; init; }

    [JsonPropertyName("u1_total")]
    public required string U1Total { get; init; }

    [JsonPropertyName("u2_total")]
    public required string U2Total { get; init; }

    [JsonPropertyName("diff_values")]
    public required List<string> DiffValues { get; init; }

    [JsonPropertyName("diff_total")]
    public required string DiffTotal { get; init; }
}
