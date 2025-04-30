using System.IO;
using KanonBot.LegacyImage;
using KanonBot.Serializer;
using LanguageExt.ClassInstances.Pred;
using RosuPP;
using static KanonBot.API.OSU.OSUExtensions;
using Oppai = OppaiSharp;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public static class OppaiCalculator
{
    public static Oppai.Beatmap LoadBeatmap(byte[] b)
    {
        using var stream = new MemoryStream(b, false);
        using var reader = new StreamReader(stream);
        return Oppai.Beatmap.Read(reader);
    }

    public static Draw.ScorePanelData CalculatePanelData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var data = new Draw.ScorePanelData { scoreInfo = score };
        if (score.IsLazer) {
            data.server = "Lazer";
        }
        var statistics = data.scoreInfo.ConvertStatistics;

        using var rosubeatmap = Beatmap.FromBytes(b);

        Mode rmode = data.scoreInfo.Mode.ToRosu();
        rosubeatmap.Convert(rmode);

        if (rmode != Mode.Catch && score.Rank == "F") {
            using var hitobjects = HitObjects.New(rosubeatmap);
            var playtime = hitobjects.Get(statistics.PassedObjects(data.scoreInfo.Mode) - 1).ToNullable()?.start_time;
            if (playtime.HasValue) {
                data.playtime = playtime / 1000.0;
            }
        }

        var clockRate = 1.0;
        using var mods = Mods.FromJson(data.scoreInfo.JsonMods, rmode);

        using var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(rosubeatmap);
        var bpm = bmAttr.clock_rate * rosubeatmap.Bpm();
        clockRate = bmAttr.clock_rate;

        var beatmap = LoadBeatmap(b);
        var dAttr = new Oppai.DiffCalc().Calc(beatmap, (Oppai.Mods)mods.Bits());
        var bAttr = new Oppai.PPv2(
            new Oppai.PPv2Parameters(
                beatmap,
                dAttr,
                c300: (int)statistics.CountGreat,
                c100: (int)statistics.CountOk,
                c50: (int)statistics.CountMeh,
                cMiss: (int)statistics.CountMiss,
                combo: (int)data.scoreInfo.MaxCombo,
                mods: (Oppai.Mods)mods.Bits()
            )
        );
        var maxcombo = beatmap.GetMaxCombo();

        data.ppInfo = PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate, maxcombo);

        // 5种acc + 全连
        double[] accs = [100.00, 99.00, 98.00, 97.00, 95.00, data.scoreInfo.AccAuto * 100.00];

        data.ppInfo.ppStats = [];

        for (int i = 0; i < accs.Length; i++)
        {
            ref var acc = ref accs[i];

            using var p = Performance.New();
            p.Lazer(score.IsLazer);
            p.Mode(rmode);
            p.Mods(mods);
            p.Accuracy(acc);
            var state = p.GenerateState(rosubeatmap);

            bAttr = new Oppai.PPv2(
            new Oppai.PPv2Parameters(
                    beatmap,
                    dAttr,
                    c300: (int)state.n300,
                    c100: (int)state.n100,
                    c50: (int)state.n50,
                    cMiss: (int)state.misses,
                    combo: (int)state.max_combo,
                    mods: (Oppai.Mods)mods.Bits()
                )
            );

            data.ppInfo.ppStats.Add(PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate, maxcombo).ppStat);
        }

        data.mode = rmode;

        return data;
    }

    public static PPInfo CalculateData(byte[] b, API.OSU.Models.ScoreLazer score)
    {
        var statistics = score.ConvertStatistics;

        using var rosubeatmap = Beatmap.FromBytes(b);

        Mode rmode = score.Mode.ToRosu();

        var mods_json = score.JsonMods;
        using var mods = Mods.FromJson(mods_json, rmode);

        using var builder = BeatmapAttributesBuilder.New();
        builder.Mode(rmode);
        builder.Mods(mods);
        var bmAttr = builder.Build(rosubeatmap);

        var bpm = bmAttr.clock_rate * rosubeatmap.Bpm();
        var clockRate = bmAttr.clock_rate;

        var beatmap = LoadBeatmap(b);
        var dAttr = new Oppai.DiffCalc().Calc(beatmap, (Oppai.Mods)mods.Bits());
        var bAttr = new Oppai.PPv2(
            new Oppai.PPv2Parameters(
                beatmap,
                dAttr,
                c300: (int)statistics.CountGreat,
                c100: (int)statistics.CountOk,
                c50: (int)statistics.CountMeh,
                cMiss: (int)statistics.CountMiss,
                combo: (int)score.MaxCombo,
                mods: (Oppai.Mods)mods.Bits()
            )
        );
        var maxcombo = beatmap.GetMaxCombo();

        return PPInfo.New(score, bAttr, dAttr, bmAttr, bpm, clockRate, maxcombo);
    }
}

public partial class PPInfo
{
    public static PPInfo New(
        API.OSU.Models.ScoreLazer score,
        Oppai.PPv2 result,
        Oppai.DiffCalc dAttr,
        RosuPP.BeatmapAttributes bmAttr,
        double bpm,
        double clockrate,
        int maxcombo
    )
    {
        return new PPInfo()
        {
            star = dAttr.Total,
            CS = bmAttr.cs,
            HP = bmAttr.hp,
            AR = bmAttr.ar,
            OD = bmAttr.od,
            accuracy = result.Acc,
            maxCombo = (uint)maxcombo,
            bpm = bpm,
            clockrate = clockrate,
            ppStat = new PPInfo.PPStat()
            {
                total = result.Total,
                aim = result.Aim,
                speed = result.Speed,
                acc = result.Acc,
                strain = null,
                flashlight = null,
            },
            ppStats = null
        };
    }
}
