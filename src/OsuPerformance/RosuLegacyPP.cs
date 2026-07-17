using KanonBot.Image;
using RosuPP;
using static KanonBot.API.OSU.OSUExtensions;

namespace KanonBot.OsuPerformance;

/// Calculator for a specific algorithm exposed by rosu-pp-older.
public sealed class RosuLegacyCalculator(
    string id,
    LegacyVersion version,
    API.OSU.Mode? supportedMode = null
) : IPerformanceCalculator
{
    public PerformanceCalculatorDescriptor Descriptor { get; } =
        new(
            id,
            id,
            supportedMode is null
                ? Enum.GetValues<API.OSU.Mode>().ToHashSet()
                : new System.Collections.Generic.HashSet<API.OSU.Mode> { supportedMode.Value },
            true
        );

    public ScorePanelData CalculatePerfectPanelData(
        byte[] beatmap,
        API.OSU.Models.Beatmap beatmapInfo,
        API.OSU.Models.Mod[] mods
    )
    {
        var score = new RosuCalculator()
            .CalculatePerfectPanelData(beatmap, beatmapInfo, mods)
            .scoreInfo;
        return CalculatePanelData(beatmap, score);
    }

    public ScorePanelData CalculatePanelData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var data = new ScorePanelData { scoreInfo = score };
        var statistics = score.ConvertStatistics;
        var mode = score.Mode.ToRosu();

        using var mods = Mods.FromJson(score.JsonMods, mode);
        using var beatmap = Beatmap.FromBytes(b);
        using var legacyBeatmap = LegacyBeatmap.FromBytes(b);
        using var builder = BeatmapAttributesBuilder.Create();
        builder.Mode(mode);
        builder.Mods(mods);
        var beatmapAttributes = builder.Build(beatmap);
        var bpm = beatmapAttributes.clock_rate * beatmap.Bpm();
        var passedObjects = statistics.PassedObjects(score.Mode);

        if (!score.Passed && passedObjects > 0)
        {
            var endTime = beatmap.HitObjectEndTime(passedObjects - 1).ToNullable();
            if (endTime.HasValue)
            {
                data.playtime = endTime.Value / 1000.0;
            }
        }

        using var difficulty = CreateDifficulty(
            legacyBeatmap,
            mode,
            mods.Bits(),
            beatmapAttributes.clock_rate,
            passedObjects
        );
        using var fullDifficulty = CreateDifficulty(
            legacyBeatmap,
            mode,
            mods.Bits(),
            beatmapAttributes.clock_rate
        );
        using var performance = CreatePerformance(score, mods);
        var result = performance.Calculate(legacyBeatmap, difficulty);
        data.performance = PerformanceResult.FromLegacy(
            result,
            beatmapAttributes,
            bpm,
            Descriptor.Id,
            score.Mode,
            fullDifficulty.Stars(),
            fullDifficulty.MaxCombo()
        );

        double[] accuracies =
        [
            100.0,
            99.0,
            98.0,
            97.0,
            95.0,
            score.AccAuto * 100.0,
        ];

        data.performance = data.performance.WithForecasts(
            accuracies.Select(accuracy =>
            {
                using var accuracyPerformance = CreatePerformance(mods);
                accuracyPerformance.Accuracy(accuracy);
                accuracyPerformance.Score((uint)Math.Round(accuracy * 10_000.0));
                return PerformanceResult.FromLegacy(
                    accuracyPerformance.Calculate(legacyBeatmap, fullDifficulty),
                    beatmapAttributes,
                    bpm,
                    Descriptor.Id,
                    score.Mode
                ).Score;
            })
        );

        data.mode = mode;
        return data;
    }

    public PerformanceResult CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var mode = score.Mode.ToRosu();
        using var mods = Mods.FromJson(score.JsonMods, mode);
        using var beatmap = Beatmap.FromBytes(b);
        using var legacyBeatmap = LegacyBeatmap.FromBytes(b);
        using var builder = BeatmapAttributesBuilder.Create();
        builder.Mode(mode);
        builder.Mods(mods);
        var beatmapAttributes = builder.Build(beatmap);
        var bpm = beatmapAttributes.clock_rate * beatmap.Bpm();
        var passedObjects = score.ConvertStatistics.PassedObjects(score.Mode);

        using var difficulty = CreateDifficulty(
            legacyBeatmap,
            mode,
            mods.Bits(),
            beatmapAttributes.clock_rate,
            passedObjects
        );
        using var fullDifficulty = CreateDifficulty(
            legacyBeatmap,
            mode,
            mods.Bits(),
            beatmapAttributes.clock_rate
        );
        using var performance = CreatePerformance(score, mods);
        return PerformanceResult.FromLegacy(
            performance.Calculate(legacyBeatmap, difficulty),
            beatmapAttributes,
            bpm,
            Descriptor.Id,
            score.Mode,
            fullDifficulty.Stars(),
            fullDifficulty.MaxCombo()
        );
    }

    private LegacyPerformance CreatePerformance(
        API.OSU.Models.ScoreLazer score,
        in Mods mods
    )
    {
        var statistics = score.ConvertStatistics;
        var performance = CreatePerformance(mods);
        performance.Combo(score.MaxCombo);
        performance.Misses(statistics.CountMiss);
        performance.N300(statistics.CountGreat);
        performance.N100(statistics.CountOk);
        performance.N50(statistics.CountMeh);
        performance.NKatu(statistics.CountKatu);
        performance.NGeki(statistics.CountGeki);
        performance.Accuracy(score.AccAuto * 100.0);
        performance.Score(score.LegacyTotalScore);
        performance.Lazer(score.IsLazer);
        performance.LegacyTotalScore(score.LegacyTotalScore);
        performance.LargeTickHits(statistics.LargeTickHit);
        performance.SmallTickHits(statistics.SmallTickHit);
        performance.SliderEndHits(statistics.SliderTailHit);
        return performance;
    }

    private static LegacyPerformance CreatePerformance(in Mods mods)
    {
        var performance = LegacyPerformance.Create();
        performance.PMods(mods);
        return performance;
    }

    private LegacyDifficulty CreateDifficulty(
        LegacyBeatmap beatmap,
        Mode mode,
        uint mods,
        double clockRate,
        uint? passedObjects = null
    )
    {
        var difficulty = LegacyDifficulty.Create();
        difficulty.SetMode(mode);
        difficulty.Mods(mods);
        difficulty.ClockRate(clockRate);

        if (passedObjects.HasValue)
        {
            difficulty.PassedObjects(passedObjects.Value);
        }

        var attributes = difficulty.Calculate(beatmap, version);

        if (attributes.IsNone)
        {
            difficulty.Dispose();
            throw new NotSupportedException(
                $"Legacy PP version {version} does not support mode {mode}."
            );
        }

        return difficulty;
    }
}

public sealed partial record PerformanceResult
{
    public static PerformanceResult FromLegacy(
        LegacyPerformanceAttributes result,
        BeatmapAttributes beatmapAttributes,
        double bpm,
        string calculatorId,
        API.OSU.Mode mode,
        double? displayStars = null,
        uint? displayMaxCombo = null
    ) =>
        Create(
            calculatorId,
            mode,
            displayStars ?? result.difficulty.stars,
            beatmapAttributes.cs,
            beatmapAttributes.hp,
            beatmapAttributes.ar,
            beatmapAttributes.od,
            bpm,
            beatmapAttributes.clock_rate,
            displayMaxCombo ?? result.difficulty.max_combo,
            result.pp,
            (PerformanceComponent.Aim, result.pp_aim.ToNullable()),
            (PerformanceComponent.Speed, result.pp_speed.ToNullable()),
            (PerformanceComponent.Accuracy, result.pp_acc.ToNullable()),
            (PerformanceComponent.Difficulty, result.pp_difficulty.ToNullable()),
            (PerformanceComponent.Flashlight, result.pp_flashlight.ToNullable())
        );
}
