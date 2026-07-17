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
            KanonBot.Config.inner?.calcOldPP == true
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
        if (string.IsNullOrWhiteSpace(requestedVersion))
        {
            if (isPpysb)
            {
                return PerformanceCalculatorIds.PpySb;
            }

            return specialVersionPp
                ? PerformanceCalculatorIds.OsuCurrent
                : KanonBot.Config.inner?.calcOldPP == true
                    ? PerformanceCalculatorIds.OsuCurrent
                    : PerformanceCalculatorIds.RosuCurrent;
        }

        var requested = requestedVersion.Trim();
        var direct = registry.Calculators.FirstOrDefault(calculator =>
            calculator.Descriptor.Id.Equals(requested, StringComparison.OrdinalIgnoreCase)
        );
        if (direct is not null)
        {
            if (!direct.Descriptor.Supports(mode))
            {
                throw new NotSupportedException(
                    $"Performance calculator '{direct.Descriptor.Id}' does not support mode '{mode}'."
                );
            }

            return direct.Descriptor.Id;
        }

        var normalized = requested.ToLowerInvariant() switch
        {
            "current" => isPpysb
                ? PerformanceCalculatorIds.PpySb
                : PerformanceCalculatorIds.RosuCurrent,
            "official" or "osu" => PerformanceCalculatorIds.OsuCurrent,
            "rosu" => PerformanceCalculatorIds.RosuCurrent,
            "sb" or "ppy.sb" => PerformanceCalculatorIds.PpySb,
            _ => ResolveHistoricalAlias(requested, mode),
        };

        var calculator = registry.GetRequired(normalized);
        if (!calculator.Descriptor.Supports(mode))
        {
            throw new NotSupportedException(
                $"Performance calculator '{normalized}' does not support mode '{mode}'."
            );
        }

        return normalized;
    }

    private static IPerformanceCalculator Resolve(
        string calculatorId,
        API.OSU.Models.ScoreLazer score
    ) => registry.Resolve(ResolveCalculatorId(calculatorId, score.Mode), score);

    private static string ResolveHistoricalAlias(string alias, API.OSU.Mode mode)
    {
        var suffix = $"/{alias.Trim().ToLowerInvariant()}";
        var match = registry.Calculators
            .Where(calculator => calculator.Descriptor.Supports(mode))
            .Where(calculator =>
                calculator.Descriptor.Id.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            )
            .OrderByDescending(calculator => calculator.Descriptor.Id.Count(character => character == '/'))
            .FirstOrDefault();

        return match?.Descriptor.Id
            ?? throw new KeyNotFoundException(
                $"Performance calculator version '{alias}' is not registered for mode '{mode}'."
            );
    }

    private static PerformanceCalculatorRegistry CreateRegistry()
    {
        var result = new PerformanceCalculatorRegistry();
        result.Register(new OsuCalculator());
        result.Register(new RosuCalculator());
        result.Register(new OppaiCalculator());
        result.Register(new SBRosuCalculator());
        foreach (var calculator in PerformanceCalculatorCatalog.LegacyCalculators)
        {
            result.Register(new RosuLegacyCalculator(calculator.Id, calculator.Version, calculator.Mode));
        }

        return result;
    }

}
