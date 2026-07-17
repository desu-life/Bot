namespace KanonBot.OsuPerformance;

using System.Collections.Immutable;

public enum PerformanceComponent
{
    Aim,
    Speed,
    Accuracy,
    Difficulty,
    Flashlight,
    Reading,
}

public enum PerformanceForecast
{
    Perfect,
    Accuracy99,
    Accuracy98,
    Accuracy97,
    Accuracy95,
    FullComboAtCurrentAccuracy,
}

public sealed record PerformanceBreakdown
{
    public required double Total { get; init; }

    public ImmutableDictionary<PerformanceComponent, double> Components { get; init; } =
        ImmutableDictionary<PerformanceComponent, double>.Empty;

    public double? Get(PerformanceComponent component) =>
        Components.TryGetValue(component, out var value) ? value : null;

    public static PerformanceBreakdown Create(
        double total,
        params (PerformanceComponent Component, double? Value)[] components
    ) =>
        new()
        {
            Total = total,
            Components = components
                .Where(static component => component.Value.HasValue)
                .ToImmutableDictionary(
                    static component => component.Component,
                    static component => component.Value!.Value
                ),
        };
}

public sealed record PerformanceDifficulty
{
    public required double Stars { get; init; }
    public required double CircleSize { get; init; }
    public required double DrainRate { get; init; }
    public required double ApproachRate { get; init; }
    public required double OverallDifficulty { get; init; }
    public required double Bpm { get; init; }
    public required double ClockRate { get; init; }
    public uint? MaxCombo { get; init; }
}

public sealed partial record PerformanceResult
{
    public required string CalculatorId { get; init; }
    public required API.OSU.Mode Mode { get; init; }
    public required PerformanceDifficulty Difficulty { get; init; }
    public required PerformanceBreakdown Score { get; init; }

    public ImmutableDictionary<PerformanceForecast, PerformanceBreakdown> Forecasts { get; init; } =
        ImmutableDictionary<PerformanceForecast, PerformanceBreakdown>.Empty;

    public PerformanceBreakdown? GetForecast(PerformanceForecast forecast) =>
        Forecasts.TryGetValue(forecast, out var value) ? value : null;

    public PerformanceResult WithForecasts(IEnumerable<PerformanceBreakdown> forecasts)
    {
        var values = forecasts.ToArray();
        var keys = Enum.GetValues<PerformanceForecast>();

        if (values.Length != keys.Length)
        {
            throw new ArgumentException(
                $"Expected {keys.Length} performance forecasts but received {values.Length}.",
                nameof(forecasts)
            );
        }

        return this with
        {
            Forecasts = keys
                .Zip(values, static (key, value) => KeyValuePair.Create(key, value))
                .ToImmutableDictionary(),
        };
    }

    internal static PerformanceResult Create(
        string calculatorId,
        API.OSU.Mode mode,
        double stars,
        double circleSize,
        double drainRate,
        double approachRate,
        double overallDifficulty,
        double bpm,
        double clockRate,
        uint? maxCombo,
        double total,
        params (PerformanceComponent Component, double? Value)[] components
    ) =>
        new()
        {
            CalculatorId = calculatorId,
            Mode = mode,
            Difficulty = new PerformanceDifficulty
            {
                Stars = stars,
                CircleSize = circleSize,
                DrainRate = drainRate,
                ApproachRate = approachRate,
                OverallDifficulty = overallDifficulty,
                Bpm = bpm,
                ClockRate = clockRate,
                MaxCombo = maxCombo,
            },
            Score = PerformanceBreakdown.Create(total, components),
        };
}

public sealed record PerformanceComparison(string CalculatorId, double Total);
