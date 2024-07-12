using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using KanonBot.API;
using KanonBot.LegacyImage;
using LanguageExt.ClassInstances.Pred;
using RosuPP;

namespace KanonBot.Functions.OSU
{
    public static class PerformanceCalculator
    {
        public struct PPInfo
        {
            public required double star,
                CS,
                HP,
                AR,
                OD;
            public double? accuracy;
            public uint? maxCombo;
            public double? bpm;
            public required PPStat ppStat;
            public List<PPStat>? ppStats;

            public struct PPStat
            {
                public required double total;
                public double? aim,
                    speed,
                    acc,
                    strain,
                    flashlight;
            }

            public static PPInfo New(PerformanceAttributes result, BeatmapAttributes bmAttr, double bpm) {
                switch (result.mode)
                {
                    case Mode.Osu: {
                        var attr = result.osu.ToNullable()!.Value;
                        return new PPInfo()
                        {
                            star = attr.stars,
                            CS = bmAttr.cs,
                            HP = attr.difficulty.hp,
                            AR = attr.difficulty.ar,
                            OD = attr.difficulty.od,
                            accuracy = attr.pp_acc,
                            maxCombo = attr.max_combo,
                            bpm = bpm,
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
                    case Mode.Taiko: {
                        var attr = result.taiko.ToNullable()!.Value;
                        return  new PPInfo()
                        {
                            star = attr.stars,
                            CS = bmAttr.cs,
                            HP = bmAttr.hp,
                            AR = bmAttr.ar,
                            OD = bmAttr.od,
                            accuracy = attr.pp_acc,
                            maxCombo = attr.max_combo,
                            bpm = bpm,
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
                    case Mode.Catch: {
                        var attr = result.fruit.ToNullable()!.Value;
                        return  new PPInfo()
                        {
                            star = attr.stars,
                            CS = bmAttr.cs,
                            HP = bmAttr.hp,
                            AR = attr.difficulty.ar,
                            OD = bmAttr.od,
                            accuracy = null,
                            maxCombo = attr.max_combo,
                            bpm = bpm,
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
                    case Mode.Mania: {
                        var attr = result.mania.ToNullable()!.Value;
                        return  new PPInfo()
                        {
                            star = attr.stars,
                            CS = bmAttr.cs,
                            HP = bmAttr.hp,
                            AR = bmAttr.ar,
                            OD = bmAttr.od,
                            accuracy = null,
                            maxCombo = attr.max_combo,
                            bpm = bpm,
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
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static RosuPP.Mode ToRosu(this API.OSU.Enums.Mode mode) => mode switch
        {
            API.OSU.Enums.Mode.OSU => RosuPP.Mode.Osu,
            API.OSU.Enums.Mode.Taiko => RosuPP.Mode.Taiko,
            API.OSU.Enums.Mode.Fruits => RosuPP.Mode.Catch,
            API.OSU.Enums.Mode.Mania => RosuPP.Mode.Mania,
            _ => throw new ArgumentException()
        };

        async public static Task<Draw.ScorePanelData> CalculatePanelSSData(API.OSU.Models.Beatmap map)
        {
            
            Beatmap beatmap;
            try
            {
                // 下载谱面
                await API.OSU.BeatmapFileChecker(map.BeatmapId);
                // 读取铺面
                beatmap = Beatmap.FromBytes(
                    await File.ReadAllBytesAsync(
                        $"./work/beatmap/{map.BeatmapId}.osu"
                    )
                );
            }
            catch (Exception)
            {
                // 加载失败，删除重新抛异常
                File.Delete($"./work/beatmap/{map.BeatmapId}.osu");
                throw;
            }

            var builder = BeatmapAttributesBuilder.New();
            var bpm = builder.GetClockRate() * beatmap.Bpm();
            var bmAttr = builder.Build(beatmap);
            var p = Performance.New();
            p.Accuracy(100);
            // 开始计算
            var res = p.Calculate(beatmap);
            var data = new Draw.ScorePanelData
            {
                scoreInfo = new API.OSU.Models.Score
                {
                    Accuracy = 1.0,
                    Beatmap = map,
                    MaxCombo = (uint)map.MaxCombo,
                    Statistics = new API.OSU.Models.ScoreStatistics 
                    {
                        CountGreat = (uint)(map.CountCircles + map.CountSliders),
                        CountMeh = 0,
                        CountMiss = 0,
                        CountKatu = 0,
                        CountOk = 0,
                    },
                    Mods = new string[0],
                    Mode = map.Mode,
                    Scores = 1000000,
                    Passed = true,
                    Rank = "X",
                }
            };
            var statistics = data.scoreInfo.Statistics;
            data.ppInfo = PPInfo.New(res, bmAttr, bpm);

            double[] accs =
            {
                100.00,
                99.00,
                98.00,
                97.00,
                95.00,
                data.scoreInfo.Accuracy * 100.00
            };
            data.ppInfo.ppStats = accs.Select(acc =>
                {
                    var p = Performance.New();
                    p.Mode(data.scoreInfo.Mode.ToRosu());
                    p.Mods(data.scoreInfo.Mods);
                    p.Accuracy(acc);
                    return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
                })
                .ToList();

            return data;
        }

        async public static Task<Draw.ScorePanelData> CalculatePanelData(API.OSU.Models.Score score)
        {
            var data = new Draw.ScorePanelData
            {
                scoreInfo = score
            };
            var statistics = data.scoreInfo.Statistics;
            Beatmap beatmap;
            try
            {
                // 下载谱面
                await API.OSU.BeatmapFileChecker(score.Beatmap!.BeatmapId);
                // 读取铺面
                beatmap = Beatmap.FromBytes(
                    await File.ReadAllBytesAsync(
                        $"./work/beatmap/{data.scoreInfo.Beatmap!.BeatmapId}.osu"
                    )
                );
            }
            catch (Exception)
            {
                // 加载失败，删除重新抛异常
                File.Delete($"./work/beatmap/{data.scoreInfo.Beatmap!.BeatmapId}.osu");
                throw;
            }


            var builder = BeatmapAttributesBuilder.New();
            builder.Mode(data.scoreInfo.Mode.ToRosu());
            builder.Mods(data.scoreInfo.Mods);
            var bpm = builder.GetClockRate() * beatmap.Bpm();
            var bmAttr = builder.Build(beatmap);

            var p = Performance.New();
            p.Mode(data.scoreInfo.Mode.ToRosu());
            p.Mods(data.scoreInfo.Mods);
            p.Combo(data.scoreInfo.MaxCombo);
            p.N300(statistics.CountGreat);
            p.N100(statistics.CountOk);
            p.N50(statistics.CountMeh);
            p.Misses(statistics.CountMiss);
            p.NKatu(statistics.CountKatu);
            // 开始计算
            data.ppInfo = PPInfo.New(p.Calculate(beatmap), bmAttr, bpm);

            // 5种acc + 全连
            double[] accs =
            {
                100.00,
                99.00,
                98.00,
                97.00,
                95.00,
                data.scoreInfo.Accuracy * 100.00
            };
            data.ppInfo.ppStats = accs.Select(acc =>
                {
                    var p = Performance.New();
                    p.Mode(data.scoreInfo.Mode.ToRosu());
                    p.Mods(data.scoreInfo.Mods);
                    p.Accuracy(acc);
                    return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
                })
                .ToList();            

            return data;
        }
    }
}
