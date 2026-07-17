using System.Collections.ObjectModel;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public static class PerformanceCalculatorIds
{
    public const string RosuCurrent = "rosu/current";
    public const string Rosu2022 = "rosu/2022";
    public const string RosuOlderBase = "rosu/older-base";
    public const string RosuOsu2014May = "rosu/osu/2014-05";
    public const string RosuOsu2014July = "rosu/osu/2014-07";
    public const string RosuOsu2015February = "rosu/osu/2015-02";
    public const string RosuOsu2015April = "rosu/osu/2015-04";
    public const string RosuOsu2018 = "rosu/osu/2018";
    public const string RosuOsu2019 = "rosu/osu/2019";
    public const string RosuOsu2021January = "rosu/osu/2021-01";
    public const string RosuOsu2021July = "rosu/osu/2021-07";
    public const string RosuOsu2021November = "rosu/osu/2021-11";
    public const string RosuOsu2022 = "rosu/osu/2022";
    public const string RosuOsu2024 = "rosu/osu/2024";
    public const string RosuOsu2025 = "rosu/osu/2025";
    public const string RosuTaikoPpv1 = "rosu/taiko/ppv1";
    public const string RosuTaiko2020 = "rosu/taiko/2020";
    public const string RosuTaiko2022 = "rosu/taiko/2022";
    public const string RosuTaiko2024 = "rosu/taiko/2024";
    public const string RosuTaiko2025 = "rosu/taiko/2025";
    public const string RosuFruitsPpv1 = "rosu/catch/ppv1";
    public const string RosuFruits2022 = "rosu/catch/2022";
    public const string RosuFruits2024 = "rosu/catch/2024";
    public const string RosuManiaPpv1 = "rosu/mania/ppv1";
    public const string RosuMania2018 = "rosu/mania/2018";
    public const string RosuMania2022 = "rosu/mania/2022";
    public const string OsuCurrent = "osu/current";
    public const string Oppai = "oppai/current";
    public const string PpySb = "rosu/ppy.sb";
}

internal readonly record struct LegacyPerformanceCalculatorRegistration(
    string Id,
    RosuPP.LegacyVersion Version,
    OSU.Mode? Mode = null
);

internal static class PerformanceCalculatorCatalog
{
    public static IReadOnlyList<LegacyPerformanceCalculatorRegistration> LegacyCalculators { get; } =
    [
        new(PerformanceCalculatorIds.Rosu2022, RosuPP.LegacyVersion.V2022),
        new(PerformanceCalculatorIds.RosuOlderBase, RosuPP.LegacyVersion.RosuPpOlderBase),
        new(PerformanceCalculatorIds.RosuOsu2014May, RosuPP.LegacyVersion.Osu2014May, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2014July, RosuPP.LegacyVersion.Osu2014July, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2015February, RosuPP.LegacyVersion.Osu2015February, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2015April, RosuPP.LegacyVersion.Osu2015April, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2018, RosuPP.LegacyVersion.Osu2018, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2019, RosuPP.LegacyVersion.Osu2019, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2021January, RosuPP.LegacyVersion.Osu2021January, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2021July, RosuPP.LegacyVersion.Osu2021July, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2021November, RosuPP.LegacyVersion.Osu2021November, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2022, RosuPP.LegacyVersion.Osu2022, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2024, RosuPP.LegacyVersion.Osu2024, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuOsu2025, RosuPP.LegacyVersion.Osu2025, OSU.Mode.OSU),
        new(PerformanceCalculatorIds.RosuTaikoPpv1, RosuPP.LegacyVersion.TaikoPpv1, OSU.Mode.Taiko),
        new(PerformanceCalculatorIds.RosuTaiko2020, RosuPP.LegacyVersion.Taiko2020, OSU.Mode.Taiko),
        new(PerformanceCalculatorIds.RosuTaiko2022, RosuPP.LegacyVersion.Taiko2022, OSU.Mode.Taiko),
        new(PerformanceCalculatorIds.RosuTaiko2024, RosuPP.LegacyVersion.Taiko2024, OSU.Mode.Taiko),
        new(PerformanceCalculatorIds.RosuTaiko2025, RosuPP.LegacyVersion.Taiko2025, OSU.Mode.Taiko),
        new(PerformanceCalculatorIds.RosuFruitsPpv1, RosuPP.LegacyVersion.FruitsPpv1, OSU.Mode.Fruits),
        new(PerformanceCalculatorIds.RosuFruits2022, RosuPP.LegacyVersion.Fruits2022, OSU.Mode.Fruits),
        new(PerformanceCalculatorIds.RosuFruits2024, RosuPP.LegacyVersion.Fruits2024, OSU.Mode.Fruits),
        new(PerformanceCalculatorIds.RosuManiaPpv1, RosuPP.LegacyVersion.ManiaPpv1, OSU.Mode.Mania),
        new(PerformanceCalculatorIds.RosuMania2018, RosuPP.LegacyVersion.Mania2018, OSU.Mode.Mania),
        new(PerformanceCalculatorIds.RosuMania2022, RosuPP.LegacyVersion.Mania2022, OSU.Mode.Mania),
    ];
}

public sealed class PerformanceCalculatorRegistry
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, IPerformanceCalculator> calculators =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IPerformanceCalculator> Calculators
    {
        get
        {
            lock (syncRoot)
            {
                return new ReadOnlyCollection<IPerformanceCalculator>([.. calculators.Values]);
            }
        }
    }

    public void Register(IPerformanceCalculator calculator, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(calculator);
        ArgumentException.ThrowIfNullOrWhiteSpace(calculator.Descriptor.Id);

        lock (syncRoot)
        {
            if (!replace && calculators.ContainsKey(calculator.Descriptor.Id))
            {
                throw new InvalidOperationException(
                    $"Performance calculator '{calculator.Descriptor.Id}' is already registered."
                );
            }

            calculators[calculator.Descriptor.Id] = calculator;
        }
    }

    public IPerformanceCalculator GetRequired(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        lock (syncRoot)
        {
            if (calculators.TryGetValue(id, out var calculator))
            {
                return calculator;
            }
        }

        throw new KeyNotFoundException($"Performance calculator '{id}' is not registered.");
    }

    public IPerformanceCalculator Resolve(
        string id,
        OSU.Models.ScoreLazer score
    ) => Resolve(id, score.Mode);

    public IPerformanceCalculator Resolve(string id, OSU.Mode mode)
    {
        var calculator = GetRequired(id);
        if (calculator.Descriptor.Supports(mode))
        {
            return calculator;
        }

        throw new NotSupportedException(
            $"Performance calculator '{id}' does not support mode '{mode}'."
        );
    }
}
