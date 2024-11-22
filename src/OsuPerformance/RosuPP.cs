using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.LegacyImage;
using KanonBot.Serializer;
using LanguageExt.ClassInstances.Pred;
using RosuPP;
using static KanonBot.API.OSU.OSUExtensions;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public static class RosuCalculator
{
    public static RosuPP.Mode ToRosu(this API.OSU.Mode mode) =>
        mode switch
        {
            OSU.Mode.OSU => RosuPP.Mode.Osu,
            OSU.Mode.Taiko => RosuPP.Mode.Taiko,
            OSU.Mode.Fruits => RosuPP.Mode.Catch,
            OSU.Mode.Mania => RosuPP.Mode.Mania,
            _ => throw new ArgumentException()
        };

    public static Draw.ScorePanelData CalculatePanelSSData(
        byte[] b,
        API.OSU.Models.Beatmap map,
        API.OSU.Models.Mod[] rawMods
    )
    {
        Beatmap beatmap = Beatmap.FromBytes(b);
        var builder = BeatmapAttributesBuilder.New();
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();
        var rmods = Mods.FromJson(Serializer.Json.Serialize(rawMods), beatmap.Mode());

        var js = RosuPP.OwnedString.Empty();
        rmods.Json(js.Context);
        var mods = Json.Deserialize<OSU.Models.Mod[]>(js.ToCstr())!;
        var is_lazer = !mods.Any(m => m.IsClassic);

        var p = Performance.New();
        p.Lazer(is_lazer);
        p.Mods(rmods);
        var pstate = p.GenerateState(beatmap);
        var res = p.Calculate(beatmap);
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
                    LargeTickHit = pstate.slider_tick_hits,
                    LargeTickMiss = pstate.slider_tick_misses,
                    SliderTailHit = pstate.slider_end_hits,
                },
                Mods = mods,
                ModeInt = map.Mode.ToNum(),
                Score = 1000000,
                Passed = true,
                Rank = "X",
            }
        };
        var statistics = data.scoreInfo.Statistics;
        data.ppInfo = PPInfo.New(res, bmAttr, bpm);

        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.Accuracy * 100.00];
        data.ppInfo.ppStats = accs.Select(acc =>
            {
                var p = Performance.New();
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
        var data = new Draw.ScorePanelData { scoreInfo = score };
        if (score.IsLazer)
        {
            data.server = "Lazer";
        }
        var statistics = data.scoreInfo.ConvertStatistics;

        Beatmap beatmap = Beatmap.FromBytes(b);

        Mode rmode = data.scoreInfo.Mode.ToRosu();

        var mods = Mods.FromJson(data.scoreInfo.JsonMods, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        var p = Performance.New();
        p.Lazer(score.IsLazer);
        p.Mode(rmode);
        p.Mods(mods);
        p.Combo(data.scoreInfo.MaxCombo);
        p.N50(statistics.CountMeh);
        p.N100(statistics.CountOk);
        p.N300(statistics.CountGreat);
        p.NKatu(statistics.CountKatu);
        p.NGeki(statistics.CountGeki);
        p.SliderTickMisses(statistics.LargeTickMiss);
        p.SliderEndHits(statistics.SliderTailHit);
        p.Misses(statistics.CountMiss);

        // 开始计算
        data.ppInfo = PPInfo.New(p.Calculate(beatmap), bmAttr, bpm);

        // 5种acc + 全连
        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00];

        data.ppInfo.ppStats = accs.Select(acc =>
            {
                var p = Performance.New();
                p.Lazer(score.IsLazer);
                p.Mode(rmode);
                p.Mods(mods);
                p.Accuracy(acc);
                return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
            })
            .ToList();

        data.mode = rmode;

        return data;
    }

    public static PPInfo CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        Beatmap beatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToRosu();

        var mods = Mods.FromJson(score.JsonMods, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        var p = Performance.New();
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
        p.SliderTickMisses(statistics.LargeTickMiss);
        p.SliderEndHits(statistics.SliderTailHit);
        var pattr = p.Calculate(beatmap);

        return PPInfo.New(pattr, bmAttr, bpm);
    }
}

public partial class PPInfo
{
    public static PPInfo New(
        RosuPP.PerformanceAttributes result,
        RosuPP.BeatmapAttributes bmAttr,
        double bpm
    )
    {
        switch (result.mode)
        {
            case RosuPP.Mode.Osu:
            {
                var attr = result.osu.ToNullable()!.Value;
                return new PPInfo()
                {
                    star = attr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = attr.pp_acc,
                    maxCombo = attr.max_combo,
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
            case RosuPP.Mode.Taiko:
            {
                var attr = result.taiko.ToNullable()!.Value;
                return new PPInfo()
                {
                    star = attr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = attr.pp_acc,
                    maxCombo = attr.max_combo,
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
            case RosuPP.Mode.Catch:
            {
                var attr = result.fruit.ToNullable()!.Value;
                return new PPInfo()
                {
                    star = attr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = null,
                    maxCombo = attr.max_combo,
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
            case RosuPP.Mode.Mania:
            {
                var attr = result.mania.ToNullable()!.Value;
                return new PPInfo()
                {
                    star = attr.stars,
                    CS = bmAttr.cs,
                    HP = bmAttr.hp,
                    AR = bmAttr.ar,
                    OD = bmAttr.od,
                    accuracy = null,
                    maxCombo = attr.max_combo,
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
                throw new ArgumentOutOfRangeException();
        }
    }
}
