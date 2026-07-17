using KanonBot.Image;

namespace KanonBot.OsuPerformance;

public static class UniversalCalculator
{
    private static readonly PerformanceCalculatorRegistry registry = CreateRegistry();

    public static IReadOnlyCollection<IPerformanceCalculator> Calculators => registry.Calculators;

    public static void Register(IPerformanceCalculator calculator, bool replace = false) =>
        registry.Register(calculator, replace);

    public static async Task<ScorePanelData> CalculatePanelData(
        API.OSU.Models.ScoreLazer score,
        string calculatorId
    )
    {
        var beatmap = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
        return CalculatePanelData(beatmap, score, calculatorId);
    }

    public static ScorePanelData CalculatePanelData(
        byte[] beatmap,
        API.OSU.Models.ScoreLazer score,
        string calculatorId
    )
    {
        var resolvedId = ResolveCalculatorId(calculatorId, score.Mode);
        var result = registry.Resolve(resolvedId, score).CalculatePanelData(beatmap, score);

        if (
            KanonBot.Config.inner?.preview == true
            && resolvedId == PerformanceCalculatorIds.OsuCurrent
        )
        {
            var comparison = registry
                .Resolve(PerformanceCalculatorIds.RosuCurrent, score)
                .CalculateData(beatmap, score);
            result.comparisons =
            [new PerformanceComparison(comparison.CalculatorId, comparison.Score.Total)];
        }

        return result;
    }

    public static ScorePanelData CalculatePerfectPanelData(
        byte[] beatmap,
        API.OSU.Models.Beatmap beatmapInfo,
        API.OSU.Models.Mod[] mods,
        string calculatorId
    )
    {
        var resolvedId = ResolveCalculatorId(calculatorId, beatmapInfo.Mode);
        return registry
            .Resolve(resolvedId, beatmapInfo.Mode)
            .CalculatePerfectPanelData(beatmap, beatmapInfo, mods);
    }

    public static async Task<PerformanceResult> CalculateData(
        API.OSU.Models.ScoreLazer score,
        string calculatorId
    )
    {
        var beatmap = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
        return CalculateData(beatmap, score, calculatorId);
    }

    public static PerformanceResult CalculateData(
        byte[] beatmap,
        API.OSU.Models.ScoreLazer score,
        string calculatorId
    ) => Resolve(calculatorId, score).CalculateData(beatmap, score);

    public static string ResolveCalculatorId(
        string? requestedVersion,
        API.OSU.Mode mode,
        bool isPpysb = false,
        bool specialVersionPp = false
    )
    {
        var defaultId = isPpysb
            ? PerformanceCalculatorIds.PpySb
            : specialVersionPp
                ? KanonBot.Config.inner?.preview == true
                    ? PerformanceCalculatorIds.OsuCurrent
                    : PerformanceCalculatorIds.V2025_10
                : PerformanceCalculatorIds.RosuCurrent;

        if (string.IsNullOrWhiteSpace(requestedVersion))
            return defaultId;

        var requested = requestedVersion.Trim();
        var alias = requested.ToLowerInvariant() switch
        {
            "current" => defaultId,
            "official" or "preview" => PerformanceCalculatorIds.OsuCurrent,
            "rosu" => PerformanceCalculatorIds.RosuCurrent,
            "sb" or "ppy.sb" => PerformanceCalculatorIds.PpySb,
            _ => requested,
        };

        var calculator = registry.Calculators.FirstOrDefault(candidate =>
            candidate.Descriptor.Id.Equals(alias, StringComparison.OrdinalIgnoreCase)
            || (!alias.Contains('/')
                && candidate.Descriptor.Id.EndsWith($"/{alias}", StringComparison.OrdinalIgnoreCase))
        );

        return calculator?.Descriptor.Supports(mode) == true
            ? calculator.Descriptor.Id
            : defaultId;
    }

    private static IPerformanceCalculator Resolve(
        string calculatorId,
        API.OSU.Models.ScoreLazer score
    ) => registry.Resolve(ResolveCalculatorId(calculatorId, score.Mode), score);

    private static PerformanceCalculatorRegistry CreateRegistry()
    {
        var result = new PerformanceCalculatorRegistry();
        result.Register(new OsuCalculator());
        result.Register(new RosuCalculator());
        result.Register(new SBRosuCalculator());
        foreach (var calculator in PerformanceCalculatorCatalog.LegacyCalculators)
        {
            result.Register(new RosuLegacyCalculator(calculator.Id, calculator.Version));
        }

        return result;
    }

}
