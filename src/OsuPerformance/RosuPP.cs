using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.LegacyImage;
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

    public static async Task<Draw.ScorePanelData> CalculatePanelSSData(API.OSU.Models.Beatmap map)
    {
        Beatmap beatmap = Beatmap.FromBytes(await Utils.LoadOrDownloadBeatmap(map));
        var builder = BeatmapAttributesBuilder.New();
        var bmAttr = builder.Build(beatmap);
        var bpm = bmAttr.clock_rate * beatmap.Bpm();
        var p = Performance.New();
        p.Accuracy(100);
        // 开始计算
        var res = p.Calculate(beatmap);
        var data = new Draw.ScorePanelData
        {
            scoreInfo = new API.OSU.Models.ScoreLazer
            {
                ConvertFromOld = true,
                Accuracy = 1.0,
                Beatmap = map,
                MaxCombo = (uint)map.MaxCombo,
                Statistics = new API.OSU.Models.ScoreStatisticsLazer
                {
                    CountGreat = (uint)(map.CountCircles + map.CountSliders),
                    CountMeh = 0,
                    CountMiss = 0,
                    CountKatu = 0,
                    CountOk = 0,
                },
                Mods = [],
                ModeInt = map.Mode.ToNum(),
                Score = 1000000,
                Passed = true,
                Rank = "X",
            }
        };
        var statistics = data.scoreInfo.Statistics;
        data.ppInfo = PPInfo.New(res, bmAttr, bpm);

        double[] accs = { 100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.Accuracy * 100.00 };
        data.ppInfo.ppStats = accs.Select(acc =>
            {
                var p = Performance.New();
                p.Accuracy(acc);
                return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
            })
            .ToList();

        data.mode = map.Mode.ToRosu();

        return data;
    }

    public static async Task<Draw.ScorePanelData> CalculatePanelData(
        API.OSU.Models.ScoreLazer score
    )
    {
        var data = new Draw.ScorePanelData { scoreInfo = score };
        var statistics = data.scoreInfo.ConvertStatistics;

        Beatmap beatmap = Beatmap.FromBytes(
            await Utils.LoadOrDownloadBeatmap(data.scoreInfo.Beatmap!)
        );

        Mode rmode = data.scoreInfo.Mode.ToRosu();

        var mods = Mods.FromJson(Serializer.Json.Serialize(data.scoreInfo.Mods), rmode);

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
        double[] accs = { 100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00 };

        data.ppInfo.ppStats = accs.Select(acc =>
            {
                var p = Performance.New();
                p.Mode(rmode);
                p.Mods(mods);
                p.Accuracy(acc);
                return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
            })
            .ToList();

        data.mode = rmode;

        return data;
    }

    public static async Task<PPInfo> CalculateData(API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        Beatmap beatmap = Beatmap.FromBytes(await Utils.LoadOrDownloadBeatmap(score.Beatmap!));

        Mode rmode = score.Mode.ToRosu();

        var mods = Mods.FromJson(Serializer.Json.Serialize(score.Mods), rmode);

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
        return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm);
    }
}
