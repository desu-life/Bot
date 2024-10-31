using System.Collections.Immutable;
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
        rosubeatmap.Convert(rmode);

        var clockRate = 1.0;
        var mods = Mods.FromJson(data.scoreInfo.JsonMods, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(rosubeatmap);
        var bpm = bmAttr.clock_rate * rosubeatmap.Bpm();
        clockRate = bmAttr.clock_rate;

        var ruleset = OsuPP.Utils.ParseRuleset(data.scoreInfo.ModeInt)!;
        var beatmap = new OsuPP.CalculatorWorkingBeatmap(ruleset, b);
        var c = OsuPP.Calculater.New(ruleset, beatmap);
        c.Mods(data.scoreInfo.JsonMods);
        c.combo = data.scoreInfo.MaxCombo;
        c.N50 = statistics.CountMeh;
        c.N100 = statistics.CountOk;
        c.N300 = statistics.CountGreat;
        c.NKatu = statistics.CountKatu;
        c.NGeki = statistics.CountGeki;
        c.NMiss = statistics.CountMiss;

        if (rmode is Mode.Osu) {
            c.SliderTickMiss = statistics.LargeTickMiss;
            c.SliderTailHit = statistics.SliderTailHit;
        }
        
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
            data.scoreInfo.AccAuto * 100.00
        };

        data.ppInfo.ppStats = [];

        for (int i = 0; i < accs.Length; i++)
        {
            ref var acc = ref accs[i];

            var p = Performance.New();
            p.Lazer(score.IsLazer);
            p.Mode(rmode);
            p.Mods(mods);
            p.Accuracy(acc);
            p.SliderTickHits(score.MaximumStatistics.LargeTickHit);
            var state = p.GenerateState(rosubeatmap);

            c.N50 = state.n50;
            c.N100 = state.n100;
            c.N300 = state.n300;
            c.NKatu = state.n_katu;
            c.NGeki = state.n_geki;
            c.NMiss = state.misses;
            c.combo = state.max_combo;
            c.accuracy = acc;

            if (rmode is Mode.Osu) {
                c.SliderTailHit = state.slider_end_hits;
                c.SliderTickMiss = score.MaximumStatistics.LargeTickHit - state.slider_tick_hits;
            }

            bAttr = c.Calculate();

            data.ppInfo.ppStats.Add(PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate).ppStat);
        }

        data.mode = rmode;

        return data;
    }

    public static async Task<PPInfo> CalculateData(API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        var b = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
        var rosubeatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToRosu();

        var mods_json = score.JsonMods;
        var mods = Mods.FromJson(mods_json, rmode);

        var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(rosubeatmap);

        var bpm = bmAttr.clock_rate * rosubeatmap.Bpm();
        var clockRate = bmAttr.clock_rate;

        var ruleset = OsuPP.Utils.ParseRuleset(score.ModeInt)!;
        var beatmap = new OsuPP.CalculatorWorkingBeatmap(ruleset, b);
        var c = OsuPP.Calculater.New(ruleset, beatmap);

        c.Mods(mods_json);
        c.combo = score.MaxCombo;
        c.N50 = statistics.CountMeh;
        c.N100 = statistics.CountOk;
        c.N300 = statistics.CountGreat;
        c.NKatu = statistics.CountKatu;
        c.NGeki = statistics.CountGeki;
        c.NMiss = statistics.CountMiss;
        c.SliderTickMiss = statistics.LargeTickMiss;
        c.SliderTailHit = statistics.SliderTailHit;
        c.accuracy = score.Accuracy * 100.00;
        var dAttr = c.CalculateDifficulty();
        var bAttr = c.Calculate();

        return PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate);
    }
}
