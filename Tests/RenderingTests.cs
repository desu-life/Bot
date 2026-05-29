using KanonBot.API.OSU;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace Tests;

public class RenderingTests
{
    private const string OutputDir = "TestOutput";

    [Fact]
    public async Task InfoV1_OldRenderer_GeneratesBaseline()
    {
        EnsureRepoRoot();
        PrepareLocalAvatar();
        Directory.CreateDirectory(OutputDir);

        var data = BuildTestInfoData();
        using var img = await KanonBot.Image.InfoV1.DrawInfo(
            data,
            isBonded: true,
            isDataOfDayAvaiavle: true
        );

        await using var stream = File.Create(Path.Combine(OutputDir, "infov1_baseline.png"));
        await img.SaveAsync(stream, new PngEncoder());

        Assert.Equal(1200, img.Width);
        Assert.Equal(857, img.Height);
    }

    [Fact]
    public async Task InfoV1_NewRenderer_GeneratesOutput()
    {
        EnsureRepoRoot();
        PrepareLocalAvatar();
        Directory.CreateDirectory(OutputDir);

        var data = BuildTestInfoData();
        var rendered = await KanonBot.Image.Takumi.InfoV1Takumi.Draw(
            data,
            isBonded: true,
            isDataOfDayAvailable: true
        );
        var output = Path.Combine(OutputDir, "infov1_takumi.png");
        await File.WriteAllBytesAsync(output, rendered.Bytes);

        using var check = Image.Load(output);
        Assert.Equal(1200, check.Width);
        Assert.Equal(857, check.Height);
    }

    [Fact]
    public async Task PPVS_OldRenderer_GeneratesBaseline()
    {
        EnsureRepoRoot();
        Directory.CreateDirectory(OutputDir);

        var data = BuildTestPPVSData();
        using var img = await KanonBot.Image.PPVS.DrawPPVS(data);
        await using var stream = File.Create(Path.Combine(OutputDir, "ppvs_baseline.png"));
        await img.SaveAsync(stream, new PngEncoder());

        Assert.Equal(1134, img.Width);
        Assert.Equal(1553, img.Height);
    }

    [Fact]
    public async Task PPVS_NewRenderer_GeneratesOutput()
    {
        EnsureRepoRoot();
        Directory.CreateDirectory(OutputDir);

        var data = BuildTestPPVSData();
        var rendered = await KanonBot.Image.Takumi.PPVSTakumi.Draw(data);
        var output = Path.Combine(OutputDir, "ppvs_takumi.png");
        await File.WriteAllBytesAsync(output, rendered.Bytes);

        using var check = Image.Load(output);
        Assert.Equal(1134, check.Width);
        Assert.Equal(1553, check.Height);
    }

    [Fact]
    public async Task PPVS_VisualComparison()
    {
        EnsureRepoRoot();
        await PPVS_OldRenderer_GeneratesBaseline();
        await PPVS_NewRenderer_GeneratesOutput();

        var diffRatio = CalculatePixelDifferenceRatio(
            Path.Combine(OutputDir, "ppvs_baseline.png"),
            Path.Combine(OutputDir, "ppvs_takumi.png")
        );

        Assert.True(diffRatio < 0.05, $"Pixel difference ratio was {diffRatio:P2}.");
    }

    private static KanonBot.Image.InfoV1.UserPanelData BuildTestInfoData()
    {
        var user = BuildUser("水瓶啊啊啊", 9037287, 75801, 2760, 4194.73, 97.12, 42);
        var prev = BuildUser("水瓶啊啊啊", 9037287, 76100, 2772, 4180.12, 97.08, 35);

        return new KanonBot.Image.InfoV1.UserPanelData
        {
            osuId = user.Id,
            userInfo = user,
            prevUserInfo = prev,
            pplusInfo = BuildPpPlus(4194.73),
            daysBefore = 7,
            badgeImageUrls = []
        };
    }

    private static KanonBot.Image.PPVS.PPVSPanelData BuildTestPPVSData()
    {
        return new KanonBot.Image.PPVS.PPVSPanelData
        {
            u1Name = "RightUser",
            u2Name = "LeftUser",
            u1 = BuildPpPlus(4194.73),
            u2 = BuildPpPlus(3870.21, 0.92)
        };
    }

    private static Models.UserExtended BuildUser(
        string username,
        long id,
        long globalRank,
        long countryRank,
        double pp,
        double accuracy,
        int levelProgress)
    {
        var stats = new Models.UserStatistics
        {
            Level = new Models.UserLevel { Current = 101, Progress = levelProgress },
            GlobalRank = globalRank,
            CountryRank = countryRank,
            PP = pp,
            RankedScore = 123456789012,
            HitAccuracy = accuracy,
            PlayCount = 32459,
            PlayTime = 4567890,
            TotalScore = 9876543210123,
            TotalHits = 12345678,
            GradeCounts = new Models.UserGradeCounts
            {
                SSH = 3,
                SS = 12,
                SH = 24,
                S = 468,
                A = 930
            },
            Rank = new Models.UserRank { Country = countryRank }
        };

        return new Models.UserExtended
        {
            Id = id,
            Username = username,
            AvatarUrl = new Uri($"https://a.ppy.sh/{id}"),
            CountryCode = "CN",
            Country = new Models.Country { Code = "CN", Name = "China" },
            Cover = null,
            Mode = Mode.OSU,
            StatisticsCurrent = stats,
            StatisticsModes = new Models.UserStatisticsModes
            {
                Osu = stats,
                Taiko = stats,
                Catch = stats,
                Mania = stats
            }
        };
    }

    private static Models.PPlusData.UserPerformancesNext BuildPpPlus(
        double total,
        double scale = 1.0)
    {
        return new Models.PPlusData.UserPerformancesNext
        {
            PerformanceTotal = total,
            AccuracyTotal = 787.56 * scale,
            FlowAimTotal = 919.77 * scale,
            JumpAimTotal = 1965.46 * scale,
            PrecisionTotal = 451.25 * scale,
            SpeedTotal = 1242.41 * scale,
            StaminaTotal = 1042.38 * scale
        };
    }

    private static void PrepareLocalAvatar()
    {
        Directory.CreateDirectory(Path.Combine("work", "avatar"));
        var target = Path.Combine("work", "avatar", "9037287.png");
        if (File.Exists(target))
            return;

        File.Copy(Path.Combine("Tests", "TestFiles", "info.png"), target);
    }

    private static void EnsureRepoRoot()
    {
        if (File.Exists("config.toml"))
            return;

        Directory.SetCurrentDirectory(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")));
    }

    private static double CalculatePixelDifferenceRatio(string baselinePath, string outputPath)
    {
        using var baseline = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(baselinePath);
        using var output = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(outputPath);
        Assert.Equal(baseline.Width, output.Width);
        Assert.Equal(baseline.Height, output.Height);

        var changed = 0L;
        var total = (long)baseline.Width * baseline.Height;
        for (var y = 0; y < baseline.Height; y++)
        {
            for (var x = 0; x < baseline.Width; x++)
            {
                var a = baseline[x, y];
                var b = output[x, y];
                if (
                    Math.Abs(a.R - b.R) > 8
                    || Math.Abs(a.G - b.G) > 8
                    || Math.Abs(a.B - b.B) > 8
                    || Math.Abs(a.A - b.A) > 8
                )
                {
                    changed++;
                }
            }
        }

        return (double)changed / total;
    }
}
