using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.Image;
using KanonBot.Serializer;
using LanguageExt.ClassInstances.Pred;
using RosuPP;
using static KanonBot.API.OSU.OSUExtensions;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

internal static class RosuCalculatorExtensions
{
    public static uint GetMaxCombo(this DifficultyAttributes dattr)
    {
        if (dattr.IsOsu) return dattr.AsOsu().max_combo;
        if (dattr.IsTaiko) return dattr.AsTaiko().max_combo;
        if (dattr.IsCatch) return dattr.AsCatch().max_combo;
        if (dattr.IsMania) return dattr.AsMania().max_combo;

        throw new ArgumentException("Invalid mode for DifficultyAttributes");
    }

    public static RosuPP.Mode ToRosu(this API.OSU.Mode mode) =>
        mode switch
        {
            OSU.Mode.OSU => RosuPP.Mode.Osu,
            OSU.Mode.Taiko => RosuPP.Mode.Taiko,
            OSU.Mode.Fruits => RosuPP.Mode.Catch,
            OSU.Mode.Mania => RosuPP.Mode.Mania,
            _ => throw new ArgumentException()
        };

    public static uint GetLargeTicks(this DifficultyAttributes dattr) {
        return dattr.IsOsu ? dattr.AsOsu().n_large_ticks : 0;
    }
}

public sealed class RosuCalculator : IPerformanceCalculator
{
    public PerformanceCalculatorDescriptor Descriptor { get; } =
        new(
            PerformanceCalculatorIds.RosuCurrent,
            "rosu! current",
            Enum.GetValues<API.OSU.Mode>().ToHashSet()
        );

    public ScorePanelData CalculatePerfectPanelData(
        byte[] b,
        API.OSU.Models.Beatmap map,
        API.OSU.Models.Mod[] rawMods
    )
    {
        using var beatmap = Beatmap.FromBytes(b);
        var mode = map.Mode.ToRosu();
        using var rmods = Mods.FromJson(Serializer.Json.Serialize(rawMods), mode);
        beatmap.Convert(mode, rmods);
        using var builder = BeatmapAttributesBuilder.Create();
        builder.Mode(mode);
        builder.Mods(rmods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        using var js = rmods.Json();
        var mods = Json.Deserialize<OSU.Models.Mod[]>(js.String)!;
        var is_lazer = !mods.Any(m => m.IsClassic);

        using var d = Difficulty.Create();
        d.Lazer(is_lazer);
        d.Mods(rmods);
        var dattr = d.Calculate(beatmap);

        using var p = Performance.Create();
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
            }
        };
        var statistics = data.scoreInfo.Statistics;
        data.performance = PerformanceResult.FromRosu(
            res,
            bmAttr,
            bpm,
            PerformanceCalculatorIds.RosuCurrent,
            map.Mode
        );

        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.Accuracy * 100.00];
        data.performance = data.performance.WithForecasts(accs.Select(acc =>
            {
                using var p = Performance.Create();
                p.Lazer(is_lazer);
                p.Mods(rmods);
                p.Accuracy(acc);
                return PerformanceResult.FromRosu(
                    p.Calculate(beatmap),
                    bmAttr,
                    bpm,
                    PerformanceCalculatorIds.RosuCurrent,
                    map.Mode
                ).Score;
            })
        );

        data.mode = map.Mode.ToRosu();

        return data;
    }

    public ScorePanelData CalculatePanelData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var data = new ScorePanelData { scoreInfo = score };
        if (score.IsLazer)
        {
            data.server = "Lazer";
        }
        var statistics = data.scoreInfo.ConvertStatistics;

        Mode rmode = data.scoreInfo.Mode.ToRosu();
        using var mods = Mods.FromJson(data.scoreInfo.JsonMods, rmode);
        using var beatmap = Beatmap.FromBytes(b);
        beatmap.Convert(rmode, mods);

        using var builder = BeatmapAttributesBuilder.Create();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();
        var passedObjects = statistics.PassedObjects(data.scoreInfo.Mode);

        if (score.Passed == false) {
            var endTime = passedObjects > 0
                ? beatmap.HitObjectEndTime(passedObjects - 1).ToNullable()
                : null;
            if (endTime.HasValue) {
                data.playtime = endTime.Value / 1000.0;
            }
        }

        using var d = Difficulty.Create();
        d.Lazer(score.IsLazer);
        d.Mods(mods);
        var dattr_full = d.Calculate(beatmap);
        d.PassedObjects(passedObjects);
        var dattr = d.Calculate(beatmap);

        using var p = Performance.Create();
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

        // 开始计算
        var state = p.GenerateStateFromDifficulty(dattr);
        data.performance = PerformanceResult.FromRosu(
            p.CalculateFromDifficulty(dattr),
            bmAttr,
            bpm,
            Descriptor.Id,
            score.Mode,
            dattr_full
        );

        // 5种acc + 全连
        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00];

        data.performance = data.performance.WithForecasts(accs.Select(acc =>
            {
                using var p = Performance.Create();
                p.Lazer(score.IsLazer);
                p.Mode(rmode);
                p.Mods(mods);
                p.Accuracy(acc);
                var s = p.GenerateStateFromDifficulty(dattr_full);

                if (dattr.IsOsu && score.IsLazer && score.Rank != "F") {
                    var a = state.Acc(ref dattr_full, OsuScoreOrigin.WithoutSliderAcc);
                    var a2 = s.Acc(ref dattr_full, OsuScoreOrigin.WithoutSliderAcc);
                    if (a > a2) {
                        p.SliderEndHits(statistics.SliderTailHit);
                    }
                }
                
                var pattr = p.CalculateFromDifficulty(dattr_full);
                return PerformanceResult.FromRosu(
                    pattr,
                    bmAttr,
                    bpm,
                    Descriptor.Id,
                    score.Mode
                ).Score;
            }));

        

        data.mode = rmode;

        return data;
    }

    public PerformanceResult CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        using var beatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToRosu();

        using var mods = Mods.FromJson(score.JsonMods, rmode);

        using var builder = BeatmapAttributesBuilder.Create();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        using var p = Performance.Create();
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

        return PerformanceResult.FromRosu(pattr, bmAttr, bpm, Descriptor.Id, score.Mode);
    }
}

public sealed partial record PerformanceResult
{
    public static PerformanceResult FromRosu(
        RosuPP.PerformanceAttributes result,
        RosuPP.BeatmapAttributes bmAttr,
        double bpm,
        string calculatorId,
        API.OSU.Mode mode,
        RosuPP.DifficultyAttributes? dresult = null
    )
    {
        if (result.IsOsu)
        {
            var attr = result.AsOsu();
            var dattr = dresult is not null ? dresult.Value.AsOsu() : attr.difficulty;
            return Create(
                calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp,
(PerformanceComponent.Aim, attr.pp_aim),
                (PerformanceComponent.Speed, attr.pp_speed),
                (PerformanceComponent.Accuracy, attr.pp_acc),
                (PerformanceComponent.Flashlight, attr.pp_flashlight),
                (PerformanceComponent.Reading, attr.pp_reading)
            );
        }

        if (result.IsTaiko)
        {
            var attr = result.AsTaiko();
            var dattr = dresult is not null ? dresult.Value.AsTaiko() : attr.difficulty;
            return Create(
                calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp,
                (PerformanceComponent.Accuracy, attr.pp_acc),
                (PerformanceComponent.Difficulty, attr.pp_difficulty)
            );
        }

        if (result.IsCatch)
        {
            var attr = result.AsCatch();
            var dattr = dresult is not null ? dresult.Value.AsCatch() : attr.difficulty;
            return Create(
                calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp
            );
        }

        if (result.IsMania)
        {
            var attr = result.AsMania();
            var dattr = dresult is not null ? dresult.Value.AsMania() : attr.difficulty;
            return Create(
                calculatorId, mode, dattr.stars, bmAttr.cs, bmAttr.hp, bmAttr.ar, bmAttr.od,
                bpm, bmAttr.clock_rate, dattr.max_combo, attr.pp,
                (PerformanceComponent.Difficulty, attr.pp_difficulty)
            );
        }

        throw new ArgumentOutOfRangeException(nameof(result), "Unknown mode");
    }
}
