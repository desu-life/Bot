using KanonBot;
using KanonBot.API.OSU;
using KanonBot.Image;
using KanonBot.OsuPerformance;

namespace Tests;

public class PerformanceCalculatorTests
{
    [Fact]
    public void BuiltInCalculatorIdsAreUnique()
    {
        var calculators = UniversalCalculator.Calculators.ToArray();

        Assert.Equal(18, calculators.Length);
        Assert.Equal(
            calculators.Length,
            calculators.Select(calculator => calculator.Descriptor.Id).Distinct().Count()
        );
    }

    [Theory]
    [InlineData("2019", Mode.OSU, PerformanceCalculatorIds.V2019)]
    [InlineData("2022", Mode.Taiko, PerformanceCalculatorIds.V2022)]
    [InlineData("2022", Mode.Fruits, PerformanceCalculatorIds.V2022)]
    [InlineData("ppv1", Mode.Mania, PerformanceCalculatorIds.VPpv1)]
    [InlineData("official", Mode.OSU, PerformanceCalculatorIds.OsuCurrent)]
    public void ResolvesModeAwareAliases(string alias, Mode mode, string expected)
    {
        Assert.Equal(expected, UniversalCalculator.ResolveCalculatorId(alias, mode));
    }

    [Fact]
    public void ExplicitUnsupportedCalculatorFallsBackToDefault()
    {
        Assert.Equal(
            PerformanceCalculatorIds.RosuCurrent,
            UniversalCalculator.ResolveCalculatorId(
                PerformanceCalculatorIds.V2019,
                Mode.Taiko
            )
        );
    }

    [Theory]
    [InlineData("ppv1", Mode.OSU)]
    [InlineData("not-registered", Mode.OSU)]
    public void InvalidCalculatorFallsBackToDefault(string requested, Mode mode)
    {
        Assert.Equal(
            PerformanceCalculatorIds.RosuCurrent,
            UniversalCalculator.ResolveCalculatorId(requested, mode)
        );
    }

    [Fact]
    public void RegistryDoesNotFallbackForUnsupportedMode()
    {
        var registry = new PerformanceCalculatorRegistry();
        registry.Register(new TestCalculator());

        Assert.Throws<NotSupportedException>(() => registry.Resolve("test", Mode.Mania));
    }

    [Fact]
    public void ForecastsHaveNamedScenarios()
    {
        var result = new PerformanceResult
        {
            CalculatorId = "test",
            Mode = Mode.OSU,
            Difficulty = new PerformanceDifficulty
            {
                Stars = 1,
                CircleSize = 4,
                DrainRate = 5,
                ApproachRate = 6,
                OverallDifficulty = 7,
                Bpm = 180,
                ClockRate = 1,
                MaxCombo = 100,
            },
            Score = PerformanceBreakdown.Create(10),
        }.WithForecasts(
            Enum.GetValues<PerformanceForecast>()
                .Select((_, index) => PerformanceBreakdown.Create(index + 1))
        );

        Assert.Equal(1, result.GetForecast(PerformanceForecast.Perfect)!.Total);
        Assert.Equal(6, result.GetForecast(PerformanceForecast.FullComboAtCurrentAccuracy)!.Total);
    }

    [Fact]
    public void LegacyPerfectCalculationKeepsRequestedVersion()
    {
        var beatmapBytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "TestFiles", "Kakichoco - Zan'ei (Lasse) [Illusion].osu")
        );
        var beatmap = new Models.Beatmap { Mode = Mode.OSU, ModeInt = 0 };

        var panel = UniversalCalculator.CalculatePerfectPanelData(
            beatmapBytes,
            beatmap,
            [],
            "2019"
        );

        Assert.Equal(PerformanceCalculatorIds.V2019, panel.performance!.CalculatorId);
        Assert.True(panel.performance.Score.Total > 0);
        Assert.Equal(6, panel.performance.Forecasts.Count);
    }

    [Fact]
    public void Mania2018UsesLegacyScore()
    {
        var beatmapBytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "TestFiles", "Kakichoco - Zan'ei (Lasse) [Illusion].osu")
        );
        var beatmap = new Models.Beatmap { Mode = Mode.Mania, ModeInt = 3 };
        var score = UniversalCalculator
            .CalculatePerfectPanelData(
                beatmapBytes,
                beatmap,
                [],
                PerformanceCalculatorIds.V2018
            )
            .scoreInfo;

        score.LegacyTotalScore = 1_000_000;
        var perfect = UniversalCalculator.CalculateData(
            beatmapBytes,
            score,
            PerformanceCalculatorIds.V2018
        );
        score.LegacyTotalScore = 500_000;
        var halfScore = UniversalCalculator.CalculateData(
            beatmapBytes,
            score,
            PerformanceCalculatorIds.V2018
        );

        Assert.True(perfect.Score.Total > halfScore.Score.Total);
    }

    private sealed class TestCalculator : IPerformanceCalculator
    {
        public PerformanceCalculatorDescriptor Descriptor { get; } =
            new("test", "test", new HashSet<Mode> { Mode.OSU });

        public ScorePanelData CalculatePerfectPanelData(
            byte[] beatmap,
            Models.Beatmap beatmapInfo,
            Models.Mod[] mods
        ) => throw new NotImplementedException();

        public ScorePanelData CalculatePanelData(byte[] beatmap, Models.ScoreLazer score) =>
            throw new NotImplementedException();

        public PerformanceResult CalculateData(byte[] beatmap, Models.ScoreLazer score) =>
            throw new NotImplementedException();
    }
}
