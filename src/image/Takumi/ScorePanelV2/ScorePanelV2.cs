namespace KanonBot.Image.Takumi;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Takumi.Render.UniFFI;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using static KanonBot.Image.Fonts;
using Img = SixLabors.ImageSharp.Image;
using IOPath = System.IO.Path;
using OSU = KanonBot.API.OSU;

public static class ScoreV2
{
    private static readonly string workingRoot = IOPath.Combine(Directory.GetCurrentDirectory(), "work");
    private static readonly JsonSerializerOptions TemplateJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] TemplateFontPaths =
    [
        IOPath.Combine(workingRoot, "fonts", "Torus-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Torus-SemiBold.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Fredoka", "Fredoka-Bold.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Fredoka", "Fredoka-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "mizolet.ttf"),
        IOPath.Combine(workingRoot, "fonts", "HarmonyOS_Sans_SC", "HarmonyOS_Sans_SC_Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "HarmonyOS_Sans_Naskh_Arabic", "HarmonyOS_Sans_Naskh_Arabic_Regular.ttf")
    ];

    private static readonly Renderer Renderer = CreateTemplateRenderer(workingRoot);

    public static async Task<Img> DrawScore(ScorePanelData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.ppInfo);
        ArgumentNullException.ThrowIfNull(data.scoreInfo);
        ArgumentNullException.ThrowIfNull(data.scoreInfo.Beatmap);
        ArgumentNullException.ThrowIfNull(data.scoreInfo.Beatmapset);
        ArgumentNullException.ThrowIfNull(data.scoreInfo.User);

        var templatePath = IOPath.Combine(workingRoot, "templates", "ScorePanelV2", "index.jinja");
        var context = await BuildTemplateContext(data, workingRoot);

        var rendered = Renderer.RenderTemplateFile(
            templatePath,
            new RenderRequest
            {
                ContextJson = JsonSerializer.Serialize(context, TemplateJsonOptions),
                Viewport = new RenderSize(1950u, 1088u),
                Format = ImageFormat.Png,
                LoadLinkedStylesheets = true,
                ResolveLocalAssets = true
            }
        );

        using var stream = new MemoryStream(rendered.Bytes, writable: false);
        return await Img.LoadAsync<Rgba32>(stream);
    }

    private static Renderer CreateTemplateRenderer(string workingRoot)
    {
        var renderer = new Renderer();
        renderer.AddSearchPath(workingRoot);

        foreach (var p in TemplateFontPaths)
        {
            renderer.AddFontFile(p);
        }

        return renderer;
    }

    private static async Task<ScorePanelContext> BuildTemplateContext(ScorePanelData data, string workingRoot)
    {
        var ppInfo = data.ppInfo;
        var score = data.scoreInfo!;
        var beatmap = score.Beatmap!;
        var beatmapset = score.Beatmapset!;
        var user = score.User!;

        using var _ = await Utils.LoadOrDownloadAvatar(user);
        using var background = await Utils.LoadOrDownloadBackground(beatmap.BeatmapsetId, beatmap.BeatmapId);

        var backgroundSrc = background is not null && File.Exists(AssetPath("background", $"{beatmap.BeatmapId}.png"))
            ? AssetPath("background", $"{beatmap.BeatmapId}.png")
            : AssetPath("legacy", "load-failed-img.png");

        var accuracyPercent = score.AccAuto * 100f;

        return new ScorePanelContext
        {
            Mode = GetTemplateMode(data.mode),
            MapBgSrc = backgroundSrc,
            AvatarSrc = GetAvatarAssetPath(user),
            PanelSrc = GetPanelAssetPath(data.mode),
            RankingSrc = AssetPath("ranking", $"ranking-{(score.Passed ? score.RankAuto : "F")}.png"),
            DifficultyRing = new DifficultyRingModel
            {
                Color = ToCssHex(Utils.ForStarDifficultyScore(ppInfo!.star)),
                CoverSrc = AssetPath("icons", "ringcontent.png"),
                RingSrc = GetRingAssetPath(data.mode)
            },
            TitleDisplay = Utils.TruncateTextByWidth(beatmapset.Title, CreateTextOptions(TorusRegular.Get(60)), 725),
            ArtistDisplay = Utils.TruncateTextByWidth(beatmapset.Artist, CreateTextOptions(TorusRegular.Get(40)), 205),
            CreatorDisplay = Utils.TruncateTextByWidth(beatmapset.Creator, CreateTextOptions(TorusRegular.Get(40)), 145),
            BeatmapIdDisplay = beatmap.BeatmapId.ToString(),
            SongTimeDisplay = BuildSongTimeDisplay(data),
            BpmDisplay = ppInfo.bpm.ToString("0.##"),
            ArDisplay = ppInfo.AR.ToString("0.0#"),
            OdDisplay = ppInfo.OD.ToString("0.0#"),
            CsDisplay = ppInfo.CS.ToString("0.0#"),
            HpDisplay = ppInfo.HP.ToString("0.0#"),
            StarDisplay = $"Stars: {ppInfo.star:0.##}",
            VersionDisplay = BuildVersionDisplay(beatmap.Version),
            UsernameDisplay = user.Username,
            EndedAtDisplay = score.EndedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"),
            PpIfFcValue = GetForecastValue(ppInfo.ppStats, 5),
            PpForecastValues =
            [
                GetForecastValue(ppInfo.ppStats, 4),
                GetForecastValue(ppInfo.ppStats, 3),
                GetForecastValue(ppInfo.ppStats, 2),
                GetForecastValue(ppInfo.ppStats, 1),
                GetForecastValue(ppInfo.ppStats, 0)
            ],
            PpBreakdownValues =
            [
                FormatPpValue(ppInfo.ppStat.aim),
                FormatPpValue(ppInfo.ppStat.speed),
                FormatPpValue(ppInfo.ppStat.acc)
            ],
            TotalPpValue = Math.Round(ppInfo.ppStat.total).ToString("0"),
            OldPpValue = data.oldPP is not null ? Math.Round(data.oldPP.Value).ToString("0") : null,
            ScoreDisplay = score.ScoreAuto.ToString("N0"),
            Judgements = BuildJudgements(data),
            AccuracyDisplay = $"{accuracyPercent:0.0#}%",
            AccuracyHueDeg = GetAccuracyHue(accuracyPercent),
            ComboCurrentDisplay = $"{score.MaxCombo}x",
            ComboMaxDisplay = GetComboMaxDisplay(ppInfo.maxCombo),
            ComboHueDeg = GetComboHue(score.MaxCombo, ppInfo.maxCombo),
            Server = string.IsNullOrWhiteSpace(data.server) ? null : data.server,
            StatusIconSrc = GetStatusIconAssetPath(beatmap.Status),
            IsLazer = score.IsLazer ? true : null,
            Mods = await BuildModModels(data, workingRoot)
        };
    }

    private static async Task<List<ModModel>?> BuildModModels(ScorePanelData data, string workingRoot)
    {
        var mods = data.scoreInfo!.Mods.AsEnumerable();
        if (data.scoreInfo.IsClassic)
        {
            mods = mods.Where(mod => !mod.IsClassic);
        }

        var models = new List<ModModel>();

        foreach (var mod in mods)
        {
            var acronym = mod.Acronym.ToUpperInvariant();
            var iconRelativePath = AssetPath("mods", $"{acronym}.png");
            var iconAbsolutePath = IOPath.Combine(workingRoot, iconRelativePath.Replace('/', IOPath.DirectorySeparatorChar));
            var fallbackText = null as string;

            if (!File.Exists(iconAbsolutePath))
            {
                iconRelativePath = AssetPath("mods", "Unknown.png");
                iconAbsolutePath = IOPath.Combine(workingRoot, iconRelativePath.Replace('/', IOPath.DirectorySeparatorChar));
                fallbackText = acronym;
            }

            string? barDisplay = null;
            string? overlayColor = null;

            if (data.scoreInfo.IsLazer)
            {
                var speedChange = (double?)mod.Settings?.GetValue("speed_change");
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

    private static async Task<string?> GetOverlayColorAsync(string iconAbsolutePath)
    {
        var icon = await Utils.TryReadImageRgba(iconAbsolutePath);
        if (icon is null)
        {
            return null;
        }

        using (icon)
        {
            return ToCssHex(Utils.GetDominantColor(icon));
        }
    }

    private static List<JudgementModel> BuildJudgements(ScorePanelData data)
    {
        var score = data.scoreInfo!;

        if (data.mode is RosuPP.Mode.Catch)
        {
            var statistics = score.ConvertStatistics;
            return
            [
                new JudgementModel { Value = statistics.CountGreat.ToString(), X = 790, Y = 849, Size = 40.0 },
                new JudgementModel { Value = statistics.CountOk.ToString(), X = 790, Y = 972, Size = 40.0 },
                new JudgementModel { Value = statistics.CountMeh.ToString(), X = 1152, Y = 849, Size = 40.0 },
                new JudgementModel { Value = statistics.CountMiss.ToString(), X = 1152, Y = 972, Size = 40.0 }
            ];
        }

        if (data.mode is RosuPP.Mode.Mania)
        {
            var statistics = score.Statistics;
            return
            [
                new JudgementModel { Value = statistics.CountGreat.ToString(), X = 790, Y = 832, Size = 35.0 },
                new JudgementModel { Value = statistics.CountGeki.ToString(), X = 1156, Y = 834, Size = 35.0 },
                new JudgementModel { Value = statistics.CountKatu.ToString(), X = 790, Y = 907, Size = 35.0 },
                new JudgementModel { Value = statistics.CountOk.ToString(), X = 1156, Y = 907, Size = 35.0 },
                new JudgementModel { Value = statistics.CountMeh.ToString(), X = 790, Y = 982, Size = 35.0 },
                new JudgementModel { Value = statistics.CountMiss.ToString(), X = 1156, Y = 982, Size = 35.0 }
            ];
        }

        return
        [
            new JudgementModel { Value = score.Statistics.CountGreat.ToString(), X = 792, Y = 854, Size = 53.09 },
            new JudgementModel { Value = score.Statistics.CountOk.ToString(), X = 792, Y = 982, Size = 53.09 },
            new JudgementModel { Value = score.Statistics.CountMeh.ToString(), X = 1154, Y = 854, Size = 53.09 },
            new JudgementModel { Value = score.Statistics.CountMiss.ToString(), X = 1154, Y = 982, Size = 53.09 }
        ];
    }

    private static string BuildSongTimeDisplay(ScorePanelData data)
    {
        var songTime = Utils.Duration2TimeString((long)Math.Round(data.scoreInfo!.Beatmap!.TotalLength / data.ppInfo!.clockrate));
        if (data.playtime is not null)
        {
            var playTime = Utils.Duration2TimeString((long)Math.Round(data.playtime.Value / data.ppInfo.clockrate));
            songTime = $"{playTime} / {songTime}";
        }

        return songTime;
    }

    private static string BuildVersionDisplay(string version)
    {
        var textOptions = CreateTextOptions(TorusRegular.Get(24.25f));
        var display = string.Empty;

        foreach (var c in version)
        {
            display += c;
            if (TextMeasurer.MeasureSize(display, textOptions).Width > 140)
            {
                display += "...";
                break;
            }
        }

        return display;
    }

    private static RichTextOptions CreateTextOptions(Font font)
    {
        return new RichTextOptions(font)
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            FallbackFontFamilies = [HarmonySans, HarmonySansArabic]
        };
    }

    private static string GetForecastValue(List<OsuPerformance.PPInfo.PPStat>? stats, int index)
    {
        return stats is not null && index >= 0 && index < stats.Count
            ? stats[index].total.ToString("0")
            : "-";
    }

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
        return mode switch
        {
            RosuPP.Mode.Catch => "fruits",
            RosuPP.Mode.Mania => "mania",
            _ => "osu"
        };
    }

    private static string GetPanelAssetPath(RosuPP.Mode mode)
    {
        return mode switch
        {
            RosuPP.Mode.Catch => AssetPath("legacy", "v2_scorepanel", "default-score-v2-fruits.png"),
            RosuPP.Mode.Mania => AssetPath("legacy", "v2_scorepanel", "default-score-v2-mania.png"),
            _ => AssetPath("legacy", "v2_scorepanel", "default-score-v2.png")
        };
    }

    private static string GetRingAssetPath(RosuPP.Mode mode)
    {
        return mode switch
        {
            RosuPP.Mode.Osu => AssetPath("icons", "std-expertplus.png"),
            RosuPP.Mode.Taiko => AssetPath("icons", "taiko-expertplus.png"),
            RosuPP.Mode.Catch => AssetPath("icons", "ctb-expertplus.png"),
            RosuPP.Mode.Mania => AssetPath("icons", "mania-expertplus.png"),
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

    private static string GetAvatarAssetPath(OSU.Models.User user)
    {
        var fileName = user.AvatarUrl.Host == "a.ppy.sb"
            ? $"sb-{user.Id}.png"
            : $"{user.Id}.png";

        return AssetPath("avatar", fileName);
    }

    private static string AssetPath(params string[] parts)
    {
        return IOPath.Combine(workingRoot, IOPath.Combine(parts));
    }

    private static string ToCssHex(Color color)
    {
        var pixel = color.ToPixel<Rgba32>();
        return $"#{pixel.R:x2}{pixel.G:x2}{pixel.B:x2}";
    }
}
