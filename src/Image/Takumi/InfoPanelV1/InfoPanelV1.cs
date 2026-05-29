using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Takumi.Render.UniFFI;
using KanonBot.API.OSU;
using KanonBot.Functions;
using SixLabors.ImageSharp.Formats.Png;
using static KanonBot.Image.Takumi.TakumiHelper;
using IOPath = System.IO.Path;

namespace KanonBot.Image.Takumi;

public static class InfoV1Takumi
{
    private static readonly string templateRoot = IOPath.Combine(
        AppContext.BaseDirectory,
        "resources",
        "templates",
        "InfoPanelV1"
    );

    private static readonly string[] TemplateFontPaths =
    [
        IOPath.Combine(workingRoot, "fonts", "Exo2", "Exo2-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Exo2", "Exo2-SemiBold.ttf"),
        IOPath.Combine(workingRoot, "fonts", "HarmonyOS_Sans_SC", "HarmonyOS_Sans_SC_Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "HarmonyOS_Sans_SC", "HarmonyOS_Sans_SC_Medium.ttf"),
        IOPath.Combine(workingRoot, "fonts", "HarmonyOS_Sans_SC", "HarmonyOS_Sans_SC_Bold.ttf"),
        IOPath.Combine(workingRoot, "fonts", "HarmonyOS_Sans_Naskh_Arabic", "HarmonyOS_Sans_Naskh_Arabic_Regular.ttf"),
    ];

    private static readonly TemplateEngine TemplateEngine = CreateTemplateEngine(templateRoot);
    private static readonly Renderer Renderer = CreateTemplateRenderer(
        templateRoot,
        TemplateFontPaths
    );

    public static async Task<RenderedImage> Draw(
        KanonBot.Image.InfoV1.UserPanelData data,
        bool isBonded = false,
        bool isDataOfDayAvailable = true
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.userInfo);

        var templatePath = IOPath.Combine(templateRoot, "index.jinja");
        var context = await BuildTemplateContext(data, isBonded, isDataOfDayAvailable);

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
                Format = ImageFormat.Png,
            }
        );
    }

    private static async Task<InfoPanelV1Context> BuildTemplateContext(
        KanonBot.Image.InfoV1.UserPanelData data,
        bool isBonded,
        bool isDataOfDayAvailable
    )
    {
        var avatarPath = await LoadOrDownloadAvatar(data.userInfo);
        var statistics = data.userInfo.Statistics;
        var prevStatistics = data.prevUserInfo?.Statistics ?? data.userInfo.Statistics;
        var pplusValues = GetPpPlusValues(data);
        var ppPlusLabels = null as List<PpPlusLabelModel>;
        var hexPoints = null as string;

        if (data.userInfo.Mode is Mode.OSU && data.pplusInfo is not null)
        {
            var xOffset = new[] { 372, 330, 122, 52, 128, 317 };
            ppPlusLabels = new List<PpPlusLabelModel>(6);
            for (var i = 0; i < pplusValues.Length; i++)
            {
                ppPlusLabels.Add(
                    new PpPlusLabelModel
                    {
                        Value = $"({Math.Round(pplusValues[i])})",
                        X = xOffset[i],
                        Y = (i % 3 != 0) ? (i < 3 ? 640 : 829) : 734
                    }
                );
            }

            hexPoints = HexagramHelper.ToSvgPoints(
                pplusValues,
                PpPlusMulti,
                PpPlusExp,
                6,
                200,
                10000,
                1
            );
        }

        return new InfoPanelV1Context
        {
            CoverSrc = await ResolveCoverAsset(data),
            PanelSrc = await ResolvePanelAsset(data),
            AvatarSrc = avatarPath,
            FlagSrc = AssetPath("flags", $"{data.userInfo.Country!.Code}.png"),
            ModeIconSrc = AssetPath("legacy", "mode_icon", $"{data.userInfo.Mode.ToStr()}.png"),
            PpPlusPanelSrc = hexPoints is not null ? AssetPath("legacy", "pp+-v1.png") : null,
            NoPpPlusPanelSrc = hexPoints is null ? AssetPath("legacy", "nopp+info-v1.png") : null,
            Badges = await BuildBadges(data),
            HexSvgPoints = hexPoints,
            HexFillColor = hexPoints is not null ? "rgba(253, 148, 62, 0.502)" : null,
            HexStrokeColor = hexPoints is not null ? "#fd943e" : null,
            PpPlusLabels = ppPlusLabels,
            UpdateTimeDisplay = $"update: {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
            DaysBeforeDisplay = BuildDaysBeforeDisplay(data, isDataOfDayAvailable),
            UsernameDisplay = data.userInfo.Username,
            CountryRankDisplay = BuildCountryRankDisplay(statistics, prevStatistics, isBonded),
            GlobalRankDisplay = string.Format("{0:N0}", statistics.GlobalRank),
            RankDiffDisplay = BuildGlobalRankDiffDisplay(statistics, prevStatistics, isBonded),
            PpDisplay = string.Format("{0:0.##}", statistics.PP),
            PpDiffDisplay = BuildPpDiffDisplay(statistics, prevStatistics, isBonded),
            SshDisplay = statistics.GradeCounts.SSH.ToString(),
            SsDisplay = statistics.GradeCounts.SS.ToString(),
            ShDisplay = statistics.GradeCounts.SH.ToString(),
            SDisplay = statistics.GradeCounts.S.ToString(),
            ADisplay = statistics.GradeCounts.A.ToString(),
            LevelDisplay = statistics.Level.Current.ToString(),
            LevelPercentDisplay = $"{statistics.Level.Progress}%",
            LevelProgress = Math.Clamp(statistics.Level.Progress, 0, 100),
            RankedScoreDisplay = string.Format("{0:N0}", statistics.RankedScore),
            AccuracyDisplay = BuildAccuracyDisplay(statistics, prevStatistics, isBonded),
            PlayCountDisplay = BuildPlayCountDisplay(statistics, prevStatistics, isBonded),
            TotalScoreDisplay = string.Format("{0:N0}", statistics.TotalScore),
            TotalHitsDisplay = BuildTotalHitsDisplay(statistics, prevStatistics, isBonded),
            PlayTimeDisplay = Utils.Duration2StringWithoutSec(statistics.PlayTime)
        };
    }

    private static readonly double[] PpPlusMulti =  [ 14.1, 69.7, 1.92, 19.8, 0.588, 3.06 ];
    private static readonly double[] PpPlusExp =  [ 0.769, 0.596, 0.953, 0.8, 1.175, 0.993 ];

    private static double[] GetPpPlusValues(KanonBot.Image.InfoV1.UserPanelData data)
    {
        var ppd = new double[6];
        try
        {
            ppd[0] = data.pplusInfo!.AccuracyTotal;
            ppd[1] = data.pplusInfo.FlowAimTotal;
            ppd[2] = data.pplusInfo.JumpAimTotal;
            ppd[3] = data.pplusInfo.PrecisionTotal;
            ppd[4] = data.pplusInfo.SpeedTotal;
            ppd[5] = data.pplusInfo.StaminaTotal;
        }
        catch
        {
            System.Array.Clear(ppd);
        }

        return ppd;
    }

    private static async Task<string> ResolvePanelAsset(KanonBot.Image.InfoV1.UserPanelData data)
    {
        if (!string.IsNullOrEmpty(data.v1PanelUrl))
        {
            var path = await CacheRemoteImage(data.v1PanelUrl, $"panel-{data.osuId}");
            if (path is not null)
                return path;
        }

        return AssetPath("legacy", "default-info-v1.png");
    }

    private static async Task<string> ResolveCoverAsset(KanonBot.Image.InfoV1.UserPanelData data)
    {
        if (!string.IsNullOrEmpty(data.v1CoverUrl))
        {
            var path = await CacheRemoteImage(data.v1CoverUrl, $"cover-{data.osuId}");
            if (path is not null)
                return path;
        }

        var coverPath = AssetPath("legacy", "v1_cover", "osu!web", $"{data.osuId}.png");
        if (File.Exists(coverPath))
            return coverPath;

        if (data.userInfo.Cover is not null)
        {
            if (data.userInfo.Cover.CustomUrl is not null)
            {
                var path = await CacheRemoteImage(
                    data.userInfo.Cover.CustomUrl.ToString(),
                    $"osu-cover-{data.osuId}"
                );
                if (path is not null)
                    return path;
            }
            else
            {
                var coverId = data.userInfo.Cover.Id ?? "0";
                coverPath = AssetPath("legacy", "v1_cover", "osu!web", $"default_{coverId}.png");
                if (File.Exists(coverPath))
                    return coverPath;

                var path = await CacheRemoteImage(
                    data.userInfo.Cover.Url.ToString(),
                    $"osu-default-cover-{coverId}"
                );
                if (path is not null)
                    return path;
            }
        }

        var n = new Random().Next(1, 6);
        return AssetPath("legacy", "v1_cover", "default", $"default_{n}.png");
    }

    private static async Task<List<BadgeModel>?> BuildBadges(
        KanonBot.Image.InfoV1.UserPanelData data
    )
    {
        if (data.badgeImageUrls.Count == 0)
            return null;

        var badges = new List<BadgeModel>(5);
        foreach (var url in data.badgeImageUrls.Where(url => !string.IsNullOrEmpty(url)).Take(5))
        {
            var path = await CacheRemoteImage(url, $"badge-{Utils.Hash(url)}");
            if (path is not null)
                badges.Add(new BadgeModel { Src = path });
        }

        return badges.Count > 0 ? badges : null;
    }

    private static string? BuildDaysBeforeDisplay(
        KanonBot.Image.InfoV1.UserPanelData data,
        bool isDataOfDayAvailable
    )
    {
        if (data.daysBefore <= 1)
            return null;

        return isDataOfDayAvailable
            ? $"对比自{data.daysBefore}天前"
            : $" 请求的日期没有数据..当前数据对比自{data.daysBefore}天前";
    }

    private static string BuildCountryRankDisplay(
        Models.UserStatistics statistics,
        Models.UserStatistics prevStatistics,
        bool isBonded
    )
    {
        if (!isBonded)
            return string.Format("#{0:N0}", statistics.CountryRank);

        var diff = statistics.CountryRank - prevStatistics.CountryRank;
        if (diff > 0)
            return string.Format("#{0:N0}(-{1:N0})", statistics.CountryRank, diff);
        if (diff < 0)
            return string.Format("#{0:N0}(+{1:N0})", statistics.CountryRank, Math.Abs(diff ?? 0));

        return string.Format("#{0:N0}", statistics.CountryRank);
    }

    private static string BuildGlobalRankDiffDisplay(
        Models.UserStatistics statistics,
        Models.UserStatistics prevStatistics,
        bool isBonded
    )
    {
        if (!isBonded)
            return "↑ -";

        var diff = statistics.GlobalRank - prevStatistics.GlobalRank;
        if (diff > 0)
            return string.Format("↓ {0:N0}", diff);
        if (diff < 0)
            return string.Format("↑ {0:N0}", Math.Abs(diff ?? 0));

        return "↑ -";
    }

    private static string BuildPpDiffDisplay(
        Models.UserStatistics statistics,
        Models.UserStatistics prevStatistics,
        bool isBonded
    )
    {
        if (!isBonded)
            return "↑ -";

        var diff = statistics.PP - prevStatistics.PP;
        if (diff >= 0.01)
            return string.Format("↑ {0:0.##}", diff);
        if (diff <= -0.01)
            return string.Format("↓ {0:0.##}", Math.Abs(diff));

        return "↑ -";
    }

    private static string BuildAccuracyDisplay(
        Models.UserStatistics statistics,
        Models.UserStatistics prevStatistics,
        bool isBonded
    )
    {
        if (!isBonded)
            return string.Format("{0:0.##}%", statistics.HitAccuracy);

        var diff = statistics.HitAccuracy - prevStatistics.HitAccuracy;
        if (diff >= 0.01)
            return string.Format("{0:0.##}%(+{1:0.##}%)", statistics.HitAccuracy, diff);
        if (diff <= -0.01)
            return string.Format("{0:0.##}%({1:0.##}%)", statistics.HitAccuracy, diff);

        return string.Format("{0:0.##}%", statistics.HitAccuracy);
    }

    private static string BuildPlayCountDisplay(
        Models.UserStatistics statistics,
        Models.UserStatistics prevStatistics,
        bool isBonded
    )
    {
        if (!isBonded)
            return string.Format("{0:N0}", statistics.PlayCount);

        var diff = statistics.PlayCount - prevStatistics.PlayCount;
        if (diff > 0)
            return string.Format("{0:N0}(+{1:N0})", statistics.PlayCount, diff);
        if (diff < 0)
            return string.Format("{0:N0}({1:N0})", statistics.PlayCount, diff);

        return string.Format("{0:N0}", statistics.PlayCount);
    }

    private static string BuildTotalHitsDisplay(
        Models.UserStatistics statistics,
        Models.UserStatistics prevStatistics,
        bool isBonded
    )
    {
        if (!isBonded)
            return string.Format("{0:N0}", statistics.TotalHits);

        var diff = statistics.TotalHits - prevStatistics.TotalHits;
        if (diff > 0)
            return string.Format("{0:N0}(+{1:N0})", statistics.TotalHits, diff);
        if (diff < 0)
            return string.Format("{0:N0}({1:N0})", statistics.TotalHits, diff);

        return string.Format("{0:N0}", statistics.TotalHits);
    }
}
