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

class OsuCalculator
{
    public static async Task<Draw.ScorePanelData> CalculatePanelData(
        API.OSU.Models.ScoreLazer score
    )
    {
        var data = new Draw.ScorePanelData { scoreInfo = score };
        var statistics = data.scoreInfo.ConvertStatistics;

        var b = await Utils.LoadOrDownloadBeatmap(data.scoreInfo.Beatmap!);
        var rosubeatmap = Beatmap.FromBytes(b);

        Mode rmode = data.scoreInfo.Mode.ToRosu();

        var clockRate = 1.0;
        var mods = Mods.FromJson(Serializer.Json.Serialize(data.scoreInfo.Mods), rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(rosubeatmap);
        var bpm = bmAttr.clock_rate * rosubeatmap.Bpm();
        clockRate = bmAttr.clock_rate;

        var ruleset = OsuPP.Utils.ParseRuleset(data.scoreInfo.ModeInt)!;
        var beatmap = new OsuPP.CalculatorWorkingBeatmap(ruleset, b);
        var c = OsuPP.Calculater.New(ruleset, beatmap);
        c.Mods(Serializer.Json.Serialize(data.scoreInfo.Mods));
        c.combo = data.scoreInfo.MaxCombo;
        c.N50 = statistics.CountMeh;
        c.N100 = statistics.CountOk;
        c.N300 = statistics.CountGreat;
        c.NKatu = statistics.CountKatu;
        c.NGeki = statistics.CountGeki;
        c.NMiss = statistics.CountMiss;
        c.accuracy = data.scoreInfo.AccAuto * 100.00;
        var dAttr = c.CalculateDifficulty();
        var bAttr = c.Calculate();

        // 开始计算
        data.ppInfo = PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate);

        // 5种acc + 全连
        double[] accs =
        {
            100.00,
            99.00,
            98.00,
            97.00,
            95.00,
            data.scoreInfo.LeagcyAcc * 100.00 // if fc使用旧版acc计算，现在不知道怎么按新的lazer acc来模拟成绩
        };

        data.ppInfo.ppStats = [];

        for (int i = 0; i < accs.Length; i++)
        {
            ref var acc = ref accs[i];

            var p = Performance.New();
            p.Mode(rmode);
            p.Mods(mods);
            p.Accuracy(acc);
            var state = p.GenerateState(rosubeatmap);

            c.N50 = state.n50;
            c.N100 = state.n100;
            c.N300 = state.n300;
            c.NKatu = state.n_katu;
            c.NGeki = state.n_geki;
            c.NMiss = state.misses;
            c.combo = state.max_combo;

            if (i == 5)
            {
                c.accuracy = data.scoreInfo.AccAuto * 100.00;
            }
            else
            {
                c.accuracy = acc;
            }

            bAttr = c.Calculate();

            data.ppInfo.ppStats.Add(PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate).ppStat);
        }

        data.mode = data.scoreInfo.Mode.ToRosu();

        return data;
    }

    public static async Task<PPInfo> CalculateData(API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        var b = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
        var rosubeatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToRosu();

        var mods_str = Serializer.Json.Serialize(score.Mods);
        var mods = Mods.FromJson(mods_str, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(rosubeatmap);

        var bpm = bmAttr.clock_rate * rosubeatmap.Bpm();
        var clockRate = bmAttr.clock_rate;

        var ruleset = OsuPP.Utils.ParseRuleset(score.ModeInt)!;
        var beatmap = new OsuPP.CalculatorWorkingBeatmap(ruleset, b);
        var c = OsuPP.Calculater.New(ruleset, beatmap);

        c.Mods(mods_str);
        c.combo = score.MaxCombo;
        c.N50 = statistics.CountMeh;
        c.N100 = statistics.CountOk;
        c.N300 = statistics.CountGreat;
        c.NKatu = statistics.CountKatu;
        c.NGeki = statistics.CountGeki;
        c.NMiss = statistics.CountMiss;
        c.accuracy = score.Accuracy * 100.00;
        var dAttr = c.CalculateDifficulty();
        var bAttr = c.Calculate();

        return PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate);
    }
}
