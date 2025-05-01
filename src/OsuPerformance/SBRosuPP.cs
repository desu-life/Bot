using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.LegacyImage;
using KanonBot.Serializer;
using LanguageExt.ClassInstances.Pred;
using SBRosuPP;
using static KanonBot.API.OSU.OSUExtensions;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public static class SBRosuCalculator
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

    public static Draw.ScorePanelData CalculatePanelSSData(
        byte[] b,
        API.OSU.Models.Beatmap map,
        API.OSU.Models.Mod[] rawMods
    )
    {
        var is_lazer = true;
        using var beatmap = Beatmap.FromBytes(b);
        using var builder = BeatmapAttributesBuilder.New();
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();
        using var rmods = Mods.FromJson(Serializer.Json.Serialize(rawMods), beatmap.Mode());

        using var js = RosuPP.OwnedString.Empty();
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
        var data = new Draw.ScorePanelData
        {
            scoreInfo = new API.OSU.Models.ScoreLazer
            {
                ConvertFromOld = true,
                Accuracy = 1.0,
                Beatmap = map,
                MaxCombo = (uint?)map.MaxCombo ?? 0,
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
                Passed = true,
                Rank = "X",
            },
            server = "ppy.sb",
        };
        var statistics = data.scoreInfo.Statistics;
        data.ppInfo = PPInfo.New(res, bmAttr, bpm);

        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.Accuracy * 100.00];
        data.ppInfo.ppStats = accs.Select(acc =>
            {
                using var p = Performance.New();
                p.Lazer(is_lazer);
                p.Mods(rmods);
                p.Accuracy(acc);
                return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
            })
            .ToList();

        data.mode = map.Mode.ToRosu();

        return data;
    }

    public static Draw.ScorePanelData CalculatePanelData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var data = new Draw.ScorePanelData { scoreInfo = score, server = "ppy.sb" };
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
            using var hitobjects = HitObjects.New(beatmap);
            var obj = hitobjects.Get(passedObjects - 1).ToNullable();
            if (obj.HasValue) {
                var endTime = obj.Value.data.duration + obj.Value.start_time;
                data.playtime = endTime / 1000.0;
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
        data.ppInfo = PPInfo.New(p.Calculate(beatmap), bmAttr, bpm, dattr);

        // 5种acc + 全连
        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00];

        data.ppInfo.ppStats = [.. accs.Select(acc =>
            {
                using var p = Performance.New();
                p.Lazer(score.IsLazer);
                p.Mode(rmode);
                p.Mods(mods);
                p.Accuracy(acc);
                return PPInfo.New(p.CalculateFromDifficulty(dattr), bmAttr, bpm).ppStat;
            })];

        data.mode = rmode.ToRosu();

        return data;
    }

    public static PPInfo CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
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

        return PPInfo.New(pattr, bmAttr, bpm);
    }
}

public partial class PPInfo
{
    public static PPInfo New(
        SBRosuPP.PerformanceAttributes result,
        SBRosuPP.BeatmapAttributes bmAttr,
        double bpm,
        SBRosuPP.DifficultyAttributes? dresult = null
    )
    {
        switch (result.mode)
        {
            case SBRosuPP.Mode.Osu:
            {
                var attr = result.osu.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.osu.Unwrap() : attr.difficulty;
                return new PPInfo()
                {
                    star = dattr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = attr.pp_acc,
                    maxCombo = dattr.max_combo,
                    bpm = bpm,
                    clockrate = bmAttr.clock_rate,
                    ppStat = new PPInfo.PPStat()
                    {
                        total = attr.pp,
                        aim = attr.pp_aim,
                        speed = attr.pp_speed,
                        acc = attr.pp_acc,
                        strain = null,
                        flashlight = attr.pp_flashlight,
                    },
                    ppStats = null
                };
            }
            case SBRosuPP.Mode.Taiko:
            {
                var attr = result.taiko.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.taiko.Unwrap() : attr.difficulty;
                return new PPInfo()
                {
                    star = dattr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = attr.pp_acc,
                    maxCombo = dattr.max_combo,
                    bpm = bpm,
                    clockrate = bmAttr.clock_rate,
                    ppStat = new PPInfo.PPStat()
                    {
                        total = attr.pp,
                        aim = null,
                        speed = null,
                        acc = attr.pp_acc,
                        strain = attr.pp_difficulty,
                        flashlight = null,
                    },
                    ppStats = null
                };
            }
            case SBRosuPP.Mode.Catch:
            {
                var attr = result.fruit.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.fruit.Unwrap() : attr.difficulty;
                return new PPInfo()
                {
                    star = dattr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = null,
                    maxCombo = dattr.max_combo,
                    bpm = bpm,
                    clockrate = bmAttr.clock_rate,
                    ppStat = new PPInfo.PPStat()
                    {
                        total = attr.pp,
                        aim = null,
                        speed = null,
                        acc = null,
                        strain = null,
                        flashlight = null,
                    },
                    ppStats = null
                };
            }
            case SBRosuPP.Mode.Mania:
            {
                var attr = result.mania.Unwrap();
                var dattr = dresult is not null ? dresult!.Value.mania.Unwrap() : attr.difficulty;
                return new PPInfo()
                {
                    star = dattr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = null,
                    maxCombo = dattr.max_combo,
                    bpm = bpm,
                    clockrate = bmAttr.clock_rate,
                    ppStat = new PPInfo.PPStat()
                    {
                        total = attr.pp,
                        aim = null,
                        speed = null,
                        acc = null,
                        strain = attr.pp_difficulty,
                        flashlight = null,
                    },
                    ppStats = null
                };
            }
            default:
                throw new ArgumentOutOfRangeException("mode", result.mode, "Unknown mode");
        }
    }
}
