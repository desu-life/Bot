using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.Image;
using KanonBot.Serializer;
using LanguageExt.ClassInstances.Pred;
using SBRosuPP;
using static KanonBot.API.OSU.OSUExtensions;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

internal static class SBRosuCalculatorExtensions
{
    public static SBRosuPP.Mode ToSBRosu(this API.OSU.Mode mode) =>
        mode switch
        {
            OSU.Mode.OSU => SBRosuPP.Mode.Osu,
            OSU.Mode.Taiko => SBRosuPP.Mode.Taiko,
            OSU.Mode.Fruits => SBRosuPP.Mode.Catch,
            OSU.Mode.Mania => SBRosuPP.Mode.Mania,
            _ => throw new ArgumentException()
        };

    public static RosuPP.Mode ToRosu(this SBRosuPP.Mode mode) =>
        mode switch
        {
            SBRosuPP.Mode.Osu => RosuPP.Mode.Osu,
            SBRosuPP.Mode.Taiko => RosuPP.Mode.Taiko,
            SBRosuPP.Mode.Catch => RosuPP.Mode.Catch,
            SBRosuPP.Mode.Mania => RosuPP.Mode.Mania,
            _ => throw new ArgumentException()
        };

    public static uint GetLargeTicks(this DifficultyAttributes dattr) {
        return dattr.osu.ToNullable()?.n_large_ticks ?? 0;
    }
}

public sealed class SBRosuCalculator : IPerformanceCalculator
{
    public PerformanceCalculatorDescriptor Descriptor { get; } =
        new(
            PerformanceCalculatorIds.PpySb,
            "rosu! ppy.sb",
            Enum.GetValues<API.OSU.Mode>().ToHashSet()
        );

    public ScorePanelData CalculatePerfectPanelData(
        byte[] b,
        API.OSU.Models.Beatmap map,
        API.OSU.Models.Mod[] rawMods
    )
    {
        var is_lazer = !rawMods.Any(mod => mod.IsClassic);
        using var beatmap = Beatmap.FromBytes(b);
        var mode = map.Mode.ToSBRosu();
        using var rmods = Mods.FromJson(Serializer.Json.Serialize(rawMods), mode);
        beatmap.Convert(mode, rmods);
        using var builder = BeatmapAttributesBuilder.New();
        builder.Mode(mode);
        builder.Mods(rmods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        using var js = SBRosuPP.OwnedString.Empty();
        rmods.Json(js.Context);
        var mods = Json.Deserialize<OSU.Models.Mod[]>(js.ToCstr())!;

        using var d = Difficulty.New();
        d.Lazer(is_lazer);
        d.Mods(rmods);
        var dattr = d.Calculate(beatmap);

        using var p = Performance.New();
        p.Lazer(is_lazer);
        p.Mods(rmods);
        var pstate = p.GenerateStateFromDifficulty(dattr);
        var res = p.CalculateFromDifficulty(dattr);
        var data = new ScorePanelData
        {
            scoreInfo = new API.OSU.Models.ScoreLazer
            {
                ConvertFromOld = true,
                Accuracy = 1.0,
                Beatmap = map,
                MaxCombo = pstate.max_combo,
                Statistics = new API.OSU.Models.ScoreStatisticsLazer
                {
                    CountGeki = pstate.n_geki,
                    CountKatu = pstate.n_katu,
                    CountGreat = pstate.n300,
                    CountOk = pstate.n100,
                    CountMeh = pstate.n50,
                    CountMiss = pstate.misses,
                    LargeTickHit = pstate.osu_large_tick_hits,
                    SmallTickHit = pstate.osu_small_tick_hits,
                    LargeTickMiss = dattr.GetLargeTicks() - pstate.osu_large_tick_hits,
                    SliderTailHit = pstate.slider_end_hits,
                },
                Mods = mods,
                ModeInt = map.Mode.ToNum(),
                Score = 1000000,
                LegacyTotalScore = 1000000,
                Passed = true,
                Rank = "X",
            },
            server = "ppy.sb",
        };
        var statistics = data.scoreInfo.Statistics;
        data.performance = PerformanceResult.FromSbRosu(
            res,
            bmAttr,
            bpm,
            PerformanceCalculatorIds.PpySb,
            map.Mode
        );

        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.Accuracy * 100.00];
        data.performance = data.performance.WithForecasts(accs.Select(acc =>
            {
                using var p = Performance.New();
                p.Lazer(is_lazer);
                p.Mods(rmods);
                p.Accuracy(acc);
                return PerformanceResult.FromSbRosu(
                    p.Calculate(beatmap),
                    bmAttr,
                    bpm,
                    PerformanceCalculatorIds.PpySb,
                    map.Mode
                ).Score;
            })
        );

        data.mode = map.Mode.ToRosu();

        return data;
    }

    public ScorePanelData CalculatePanelData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var data = new ScorePanelData { scoreInfo = score, server = "ppy.sb" };
        var statistics = data.scoreInfo.ConvertStatistics;

        Mode rmode = data.scoreInfo.Mode.ToSBRosu();
        using var mods = Mods.FromJson(data.scoreInfo.JsonMods, rmode);
        using var beatmap = Beatmap.FromBytes(b);
        beatmap.Convert(rmode, mods);

        using var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();
        var passedObjects = statistics.PassedObjects(data.scoreInfo.Mode);

        if (score.Passed == false) {
            if (passedObjects > 0) {
                using var hitobjects = HitObjects.New(beatmap);
                var obj = hitobjects.Get(passedObjects - 1).ToNullable();
                if (obj.HasValue) {
                    var endTime = obj.Value.data.duration + obj.Value.start_time;
                    data.playtime = endTime / 1000.0;
                }
            }
        }

        using var d = Difficulty.New();
        d.Lazer(score.IsLazer);
        d.Mods(mods);
        var dattr = d.Calculate(beatmap);

        using var p = Performance.New();
        p.Lazer(score.IsLazer);
        p.Mode(rmode);
        p.Mods(mods);
        p.Combo(data.scoreInfo.MaxCombo);
        p.LegacyTotalScore(data.scoreInfo.LegacyTotalScore);
        p.N50(statistics.CountMeh);
        p.N100(statistics.CountOk);
        p.N300(statistics.CountGreat);
        p.NKatu(statistics.CountKatu);
        p.NGeki(statistics.CountGeki);
        p.SmallTickHits(statistics.SmallTickHit);
        p.LargeTickHits(statistics.LargeTickHit);
        p.SliderEndHits(statistics.SliderTailHit);
        p.Misses(statistics.CountMiss);
        p.PassedObjects(passedObjects);

        // 开始计算
        data.performance = PerformanceResult.FromSbRosu(
            p.Calculate(beatmap),
            bmAttr,
            bpm,
            Descriptor.Id,
            score.Mode,
            dattr
        );

        // 5种acc + 全连
        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00];

        data.performance = data.performance.WithForecasts(accs.Select(acc =>
            {
                using var p = Performance.New();
                p.Lazer(score.IsLazer);
                p.Mode(rmode);
                p.Mods(mods);
                p.Accuracy(acc);
                return PerformanceResult.FromSbRosu(
                    p.CalculateFromDifficulty(dattr),
                    bmAttr,
                    bpm,
                    Descriptor.Id,
                    score.Mode
                ).Score;
            }));

        data.mode = rmode.ToRosu();

        return data;
    }

    public PerformanceResult CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        using var beatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToSBRosu();

        using var mods = Mods.FromJson(score.JsonMods, rmode);

        using var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        using var p = Performance.New();
        p.Lazer(score.IsLazer);
        p.Mode(rmode);
        p.Mods(mods);
        p.Combo(score.MaxCombo);
        p.LegacyTotalScore(score.LegacyTotalScore);
        p.N50(statistics.CountMeh);
        p.N100(statistics.CountOk);
        p.N300(statistics.CountGreat);
        p.NKatu(statistics.CountKatu);
        p.NGeki(statistics.CountGeki);
        p.Misses(statistics.CountMiss);
        p.SmallTickHits(statistics.SmallTickHit);
        p.LargeTickHits(statistics.LargeTickHit);
        p.SliderEndHits(statistics.SliderTailHit);
        var pattr = p.Calculate(beatmap);

        return PerformanceResult.FromSbRosu(pattr, bmAttr, bpm, Descriptor.Id, score.Mode);
    }
}

public sealed partial record PerformanceResult
{
    public static PerformanceResult FromSbRosu(
        SBRosuPP.PerformanceAttributes result,
        SBRosuPP.BeatmapAttributes bmAttr,
        double bpm,
        string calculatorId,
        API.OSU.Mode mode,
        SBRosuPP.DifficultyAttributes? dresult = null
    )
    {
        switch (result.mode)
        {
            case SBRosuPP.Mode.Osu:
            {
                var attr = result.osu.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.osu.Unwrap() : attr.difficulty;
                return Create(
                    calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                    bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp,
                    (PerformanceComponent.Aim, attr.pp_aim),
                    (PerformanceComponent.Speed, attr.pp_speed),
                    (PerformanceComponent.Accuracy, attr.pp_acc),
                    (PerformanceComponent.Flashlight, attr.pp_flashlight)
                );
            }
            case SBRosuPP.Mode.Taiko:
            {
                var attr = result.taiko.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.taiko.Unwrap() : attr.difficulty;
                return Create(
                    calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                    bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp,
                    (PerformanceComponent.Accuracy, attr.pp_acc),
                    (PerformanceComponent.Difficulty, attr.pp_difficulty)
                );
            }
            case SBRosuPP.Mode.Catch:
            {
                var attr = result.fruit.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.fruit.Unwrap() : attr.difficulty;
                return Create(
                    calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                    bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp
                );
            }
            case SBRosuPP.Mode.Mania:
            {
                var attr = result.mania.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.mania.Unwrap() : attr.difficulty;
                return Create(
                    calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                    bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp,
                    (PerformanceComponent.Difficulty, attr.pp_difficulty)
                );
            }
            default:
                throw new ArgumentOutOfRangeException("mode", result.mode, "Unknown mode");
        }
    }
}
