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
        Console.WriteLine(Json.Serialize(mods));

        var p = Performance.New();
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
                var p = Performance.New();
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

        Beatmap beatmap = Beatmap.FromBytes(b);

        Mode rmode = data.scoreInfo.Mode.ToSBRosu();

        var mods = Mods.FromJson(data.scoreInfo.JsonMods, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        var p = Performance.New();
        p.Mode(rmode);
        p.Mods(mods);
        p.Combo(data.scoreInfo.MaxCombo);
        p.N50(statistics.CountMeh);
        p.N100(statistics.CountOk);
        p.N300(statistics.CountGreat);
        p.NKatu(statistics.CountKatu);
        p.NGeki(statistics.CountGeki);
        p.Misses(statistics.CountMiss);

        // 开始计算
        data.ppInfo = PPInfo.New(p.Calculate(beatmap), bmAttr, bpm);

        // 5种acc + 全连
        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00];

        data.ppInfo.ppStats = accs.Select(acc =>
            {
                var p = Performance.New();
                p.Mode(rmode);
                p.Mods(mods);
                p.Accuracy(acc);
                return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
            })
            .ToList();

        data.mode = rmode.ToRosu();

        return data;
    }

    public static PPInfo CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        Beatmap beatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToSBRosu();

        var mods = Mods.FromJson(score.JsonMods, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();

        var p = Performance.New();
        p.Mode(rmode);
        p.Mods(mods);
        p.Combo(score.MaxCombo);
        p.N50(statistics.CountMeh);
        p.N100(statistics.CountOk);
        p.N300(statistics.CountGreat);
        p.NKatu(statistics.CountKatu);
        p.NGeki(statistics.CountGeki);
        p.Misses(statistics.CountMiss);
        var pattr = p.Calculate(beatmap);

        return PPInfo.New(pattr, bmAttr, bpm);
    }
}

public partial class PPInfo
{
    public static PPInfo New(
        SBRosuPP.PerformanceAttributes result,
        SBRosuPP.BeatmapAttributes bmAttr,
        double bpm
    )
    {
        switch (result.mode)
        {
            case SBRosuPP.Mode.Osu:
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
            case SBRosuPP.Mode.Taiko:
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
            case SBRosuPP.Mode.Catch:
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
            case SBRosuPP.Mode.Mania:
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