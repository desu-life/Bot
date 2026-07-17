using System.Collections.ObjectModel;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public static class LegacyVersionExtensions
{
    public static OSU.Mode[] SupportedModes(this RosuPP.LegacyVersion version)
    {
        using var modes = RosuPP.Interop.legacy_version_supported_modes(version);
        return [ .. modes.Select(mode => mode.ToOsu()) ];
    }
}

public static class PerformanceCalculatorIds
{
    public const string RosuCurrent = "rosu/current";
    public const string V2022 = "rosu/2022";
    public const string V2025_10 = "rosu/2025_10";
    public const string V2025_3 = "rosu/2025_3";
    public const string V2024 = "rosu/2024";
    public const string V2021_11 = "rosu/2021_11";
    public const string V2021_7 = "rosu/2021_7";
    public const string V2021_1 = "rosu/2021_1";
    public const string V2020 = "rosu/2020";
    public const string V2019 = "rosu/2019";
    public const string V2018 = "rosu/2018";
    public const string V2015_4 = "rosu/2015_4";
    public const string V2015_2 = "rosu/2015_2";
    public const string V2014_7 = "rosu/2014_7";
    public const string V2014_5 = "rosu/2014_5";
    public const string VPpv1 = "rosu/ppv1";
    public const string OsuCurrent = "osu/current";
    public const string PpySb = "rosu/ppy.sb";

    public static bool ShouldRenderVersion(string id) => id != RosuCurrent && id != PpySb;
}

internal readonly record struct LegacyPerformanceCalculatorRegistration(
    string Id,
    RosuPP.LegacyVersion Version
);

internal static class PerformanceCalculatorCatalog
{
    public static IReadOnlyList<LegacyPerformanceCalculatorRegistration> LegacyCalculators { get; } =
        [
            new(PerformanceCalculatorIds.V2022, RosuPP.LegacyVersion.V2022),
            new(PerformanceCalculatorIds.V2025_10, RosuPP.LegacyVersion.V2025_10),
            new(PerformanceCalculatorIds.V2025_3, RosuPP.LegacyVersion.V2025_3),
            new(PerformanceCalculatorIds.V2024, RosuPP.LegacyVersion.V2024),
            new(PerformanceCalculatorIds.V2021_11, RosuPP.LegacyVersion.V2021_11),
            new(PerformanceCalculatorIds.V2021_7, RosuPP.LegacyVersion.V2021_7),
            new(PerformanceCalculatorIds.V2021_1, RosuPP.LegacyVersion.V2021_1),
            new(PerformanceCalculatorIds.V2020, RosuPP.LegacyVersion.V2020),
            new(PerformanceCalculatorIds.V2019, RosuPP.LegacyVersion.V2019),
            new(PerformanceCalculatorIds.V2018, RosuPP.LegacyVersion.V2018),
            new(PerformanceCalculatorIds.V2015_4, RosuPP.LegacyVersion.V2015_4),
            new(PerformanceCalculatorIds.V2015_2, RosuPP.LegacyVersion.V2015_2),
            new(PerformanceCalculatorIds.V2014_7, RosuPP.LegacyVersion.V2014_7),
            new(PerformanceCalculatorIds.V2014_5, RosuPP.LegacyVersion.V2014_5),
            new(PerformanceCalculatorIds.VPpv1, RosuPP.LegacyVersion.VPpv1),
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
                return new ReadOnlyCollection<IPerformanceCalculator>([ .. calculators.Values ]);
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

    public IPerformanceCalculator Resolve(string id, OSU.Models.ScoreLazer score) =>
        Resolve(id, score.Mode);

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
