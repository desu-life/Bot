namespace KanonBot.Image.Takumi;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Takumi.Template.UniFFI;
using global::Takumi.Render.UniFFI;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using static KanonBot.Image.Fonts;
using static KanonBot.Image.Takumi.TakumiHelper;
using KanonBot.OsuPerformance;
using Img = SixLabors.ImageSharp.Image;
using IOPath = System.IO.Path;
using OSU = KanonBot.API.OSU;

public static class ScoreV2
{
    private static readonly string templateRoot = IOPath.Combine(
        AppContext.BaseDirectory,
        "resources",
        "templates",
        "ScorePanelV2"
    );

    private static readonly string[] TemplateFontPaths =
    [
        IOPath.Combine(workingRoot, "fonts", "Torus-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Torus-SemiBold.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Fredoka", "Fredoka-Bold.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Fredoka", "Fredoka-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "mizolet.ttf"),
        IOPath.Combine(workingRoot, "fonts", "rhr", "ResourceHanRoundedCN-VF.otf"),
        IOPath.Combine(workingRoot, "fonts", "noto_arabic", "NotoSansArabic-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "noto_arabic", "NotoSansArabic-SemiBold.ttf"),
    ];

    private static readonly TemplateEngine TemplateEngine = CreateTemplateEngine(templateRoot);
    private static readonly Renderer Renderer = CreateTemplateRenderer(
        templateRoot,
        TemplateFontPaths
    );

    public static async Task<RenderedImage> DrawScore(
        ScorePanelData data,
        ImageFormat format = ImageFormat.Jpeg
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.performance);
        ArgumentNullException.ThrowIfNull(data.scoreInfo);
        ArgumentNullException.ThrowIfNull(data.scoreInfo.Beatmap);
        ArgumentNullException.ThrowIfNull(data.scoreInfo.Beatmapset);
        ArgumentNullException.ThrowIfNull(data.scoreInfo.User);

        var templatePath = IOPath.Combine(templateRoot, "index.jinja");
        var context = await BuildTemplateContext(data, workingRoot);

        var html = TemplateEngine.Render(
            new TemplateRequest
            {
                Input = TemplateInput.File(templatePath),
                ContextJson = JsonSerializer.Serialize(context, TemplateJsonOptions),
                ContentKind = TemplateContentKind.JinjaHtml,
            }
        );

        return Renderer.Render(
            new RenderRequest
            {
                Input = RenderInput.Inline(html),
                Viewport = new RenderSize(),
                Format = format,
            }
        );
    }

    private static async Task<ScorePanelContext> BuildTemplateContext(
        ScorePanelData data,
        string workingRoot
    )
    {
        var performance = data.performance!;
        var difficulty = performance.Difficulty;
        var comparison = data.comparisons.FirstOrDefault();
        var score = data.scoreInfo!;
        var beatmap = score.Beatmap!;
        var beatmapset = score.Beatmapset!;
        var user = score.User!;

        var avatarPath = await LoadOrDownloadAvatar(user);
        var backgroundPath =
            await LoadOrDownloadBackground(beatmap.BeatmapsetId, beatmap.BeatmapId)
            ?? AssetPath("legacy", "load-failed-img.png");

        var accuracyPercent = score.AccAuto * 100f;

        return new ScorePanelContext
        {
            Mode = GetTemplateMode(data.mode),
            MapBgSrc = backgroundPath,
            AvatarSrc = avatarPath,
            PanelSrc = GetPanelAssetPath(data.mode),
            RankingSrc = AssetPath(
                "ranking",
                $"ranking-{(score.Passed ? score.RankAuto : "F")}.png"
            ),
            DifficultyRing = new DifficultyRingModel
            {
                Color = Utils.ForStarDifficultyScore(difficulty.Stars).ToCssColor(),
                CoverSrc = AssetPath("icons", "ringcontent.png"),
                RingSrc = GetRingAssetPath(data.mode)
            },
            TitleDisplay = beatmapset.Title,
            ArtistDisplay = beatmapset.Artist,
            CreatorDisplay = beatmapset.Creator,
            BeatmapIdDisplay = beatmap.BeatmapId.ToString(),
            SongTimeDisplay = BuildSongTimeDisplay(data),
            BpmDisplay = difficulty.Bpm.ToString("0.##"),
            ArDisplay = difficulty.ApproachRate.ToString("0.0#"),
            OdDisplay = difficulty.OverallDifficulty.ToString("0.0#"),
            CsDisplay = difficulty.CircleSize.ToString("0.0#"),
            HpDisplay = difficulty.DrainRate.ToString("0.0#"),
            StarDisplay = $"Stars: {difficulty.Stars:0.##}",
            VersionDisplay = beatmap.Version,
            UsernameDisplay = user.Username,
            EndedAtDisplay = score.EndedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"),
            PpIfFcValue = GetForecastValue(performance, PerformanceForecast.FullComboAtCurrentAccuracy),
            PpForecastValues =
            [
                GetForecastValue(performance, PerformanceForecast.Accuracy95),
                GetForecastValue(performance, PerformanceForecast.Accuracy97),
                GetForecastValue(performance, PerformanceForecast.Accuracy98),
                GetForecastValue(performance, PerformanceForecast.Accuracy99),
                GetForecastValue(performance, PerformanceForecast.Perfect)
            ],
            PpBreakdownValues =
            [
                FormatPpValue(performance.Score.Get(PerformanceComponent.Aim)),
                FormatPpValue(performance.Score.Get(PerformanceComponent.Speed)),
                FormatPpValue(performance.Score.Get(PerformanceComponent.Accuracy))
            ],
            TotalPpValue = Math.Round(performance.Score.Total).ToString("0"),
            OldPpValue = comparison is not null ? Math.Round(comparison.Total).ToString("0") : null,
            OldPpLabel = comparison?.CalculatorId,
            CalculatorId = performance.CalculatorId,
            ScoreDisplay = score.ScoreAuto.ToString("N0"),
            Judgements = BuildJudgements(data),
            AccuracyDisplay = $"{accuracyPercent:0.0#}%",
            AccuracyHueDeg = GetAccuracyHue(accuracyPercent),
            ComboCurrentDisplay = $"{score.MaxCombo}x",
            ComboMaxDisplay = GetComboMaxDisplay(difficulty.MaxCombo),
            ComboHueDeg = GetComboHue(score.MaxCombo, difficulty.MaxCombo),
            Server = string.IsNullOrWhiteSpace(data.server) ? null : data.server,
            StatusIconSrc = GetStatusIconAssetPath(beatmap.Status),
            IsLazer = score.IsLazer ? true : null,
            Mods = await BuildModModels(data, workingRoot)
        };
    }

    private static async Task<List<ModModel>?> BuildModModels(
        ScorePanelData data,
        string workingRoot
    )
    {
        var mods = data.scoreInfo.Mods.AsEnumerable();
        if (data.scoreInfo.IsClassic)
        {
            mods = mods.Where(mod => !mod.IsClassic);
        }

        var models = new List<ModModel>();

        foreach (var mod in mods)
        {
            var acronym = mod.Acronym.ToUpperInvariant();
            var iconRelativePath = AssetPath("mods", $"{acronym}.png");
            var iconAbsolutePath = IOPath.Combine(
                workingRoot,
                iconRelativePath.Replace('/', IOPath.DirectorySeparatorChar)
            );
            var fallbackText = null as string;

            if (!File.Exists(iconAbsolutePath))
            {
                iconRelativePath = AssetPath("mods", "Unknown.png");
                iconAbsolutePath = IOPath.Combine(
                    workingRoot,
                    iconRelativePath.Replace('/', IOPath.DirectorySeparatorChar)
                );
                fallbackText = acronym;
            }

            string? barDisplay = null;
            string? overlayColor = null;

            if (data.scoreInfo.IsLazer)
            {
                var speedChange = (double?)mod.Settings?["speed_change"];
                if (speedChange is not null)
                {
                    barDisplay = $"{speedChange}x";
                    overlayColor = await GetOverlayColorAsync(iconAbsolutePath) ?? "#666666";
                }
            }

            models.Add(
                new ModModel
                {
                    Acronym = acronym,
                    IconSrc = iconRelativePath,
                    FallbackText = fallbackText,
                    BarDisplay = barDisplay,
                    OverlayColor = overlayColor
                }
            );
        }

        return models.Count > 0 ? models : null;
    }

    private static List<JudgementModel> BuildJudgements(ScorePanelData data)
    {
        var score = data.scoreInfo!;

        if (data.mode == RosuPP.Mode.Catch)
        {
            var statistics = score.ConvertStatistics;
            return [
                new JudgementModel { Value = statistics.CountGreat.ToString(), X = 790, Y = 849, Size = 40.0 },
                new JudgementModel { Value = statistics.CountOk.ToString(), X = 790, Y = 972, Size = 40.0 },
                new JudgementModel { Value = statistics.CountMeh.ToString(), X = 1152, Y = 849, Size = 40.0 },
                new JudgementModel { Value = statistics.CountMiss.ToString(), X = 1152, Y = 972, Size = 40.0 }
            ];
        }

        if (data.mode == RosuPP.Mode.Mania)
        {
            var statistics = score.Statistics;
            return [
                new JudgementModel { Value = statistics.CountGreat.ToString(), X = 790, Y = 832, Size = 35.0 },
                new JudgementModel { Value = statistics.CountGeki.ToString(), X = 1156, Y = 834, Size = 35.0 },
                new JudgementModel { Value = statistics.CountKatu.ToString(), X = 790, Y = 907, Size = 35.0 },
                new JudgementModel { Value = statistics.CountOk.ToString(), X = 1156, Y = 907, Size = 35.0 },
                new JudgementModel { Value = statistics.CountMeh.ToString(), X = 790, Y = 982, Size = 35.0 },
                new JudgementModel { Value = statistics.CountMiss.ToString(), X = 1156, Y = 982, Size = 35.0 }
            ];
        }

        return [
            new JudgementModel { Value = score.Statistics.CountGreat.ToString(), X = 792, Y = 854, Size = 53.09 },
            new JudgementModel { Value = score.Statistics.CountOk.ToString(), X = 792, Y = 982, Size = 53.09 },
            new JudgementModel { Value = score.Statistics.CountMeh.ToString(), X = 1154, Y = 854, Size = 53.09 },
            new JudgementModel { Value = score.Statistics.CountMiss.ToString(), X = 1154, Y = 982, Size = 53.09 }
        ];
    }

    private static string BuildSongTimeDisplay(ScorePanelData data)
    {
        var songTime = Utils.Duration2TimeString(
            (long)Math.Round(data.scoreInfo.Beatmap!.TotalLength / data.performance!.Difficulty.ClockRate)
        );
        if (data.playtime is not null)
        {
            var playTime = Utils.Duration2TimeString(
                (long)Math.Round(data.playtime.Value / data.performance.Difficulty.ClockRate)
            );
            songTime = $"{playTime} / {songTime}";
        }

        return songTime;
    }

    private static string GetForecastValue(
        PerformanceResult performance,
        PerformanceForecast forecast
    ) => performance.GetForecast(forecast)?.Total.ToString("0") ?? "-";

    private static string FormatPpValue(double? value)
    {
        return value is null ? "-" : value.Value.ToString("0");
    }

    private static string? GetComboMaxDisplay(uint? maxCombo)
    {
        return maxCombo is > 0 ? $"{maxCombo.Value}x" : null;
    }

    private static double GetComboHue(uint combo, uint? maxCombo)
    {
        return maxCombo is > 0 ? (double)combo / maxCombo.Value * 100 + 260 : 260;
    }

    private static double GetAccuracyHue(double accuracyPercent)
    {
        return accuracyPercent < 60 ? 260f : (accuracyPercent - 60) * 2 + 280f;
    }

    private static string GetTemplateMode(RosuPP.Mode mode)
    {
        return (int)mode switch
        {
            2 => "fruits",
            3 => "mania",
            _ => "osu"
        };
    }

    private static string GetPanelAssetPath(RosuPP.Mode mode)
    {
        return (int)mode switch
        {
            2 => AssetPath("legacy", "v2_scorepanel", "default-score-v2-fruits.png"),
            3 => AssetPath("legacy", "v2_scorepanel", "default-score-v2-mania.png"),
            _ => AssetPath("legacy", "v2_scorepanel", "default-score-v2.png")
        };
    }

    private static string GetRingAssetPath(RosuPP.Mode mode)
    {
        return (int)mode switch
        {
            0 => AssetPath("icons", "std-expertplus.png"),
            1 => AssetPath("icons", "taiko-expertplus.png"),
            2 => AssetPath("icons", "ctb-expertplus.png"),
            3 => AssetPath("icons", "mania-expertplus.png"),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static string? GetStatusIconAssetPath(OSU.Models.Status status)
    {
        return status switch
        {
            OSU.Models.Status.Ranked => AssetPath("icons", "ranked.png"),
            OSU.Models.Status.Approved => AssetPath("icons", "approved.png"),
            OSU.Models.Status.Loved => AssetPath("icons", "loved.png"),
            _ => null
        };
    }
}
