using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using KanonBot.API;
using KanonBot.LegacyImage;
using LanguageExt.ClassInstances.Pred;
using System.Security.Cryptography;
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
            public double bpm;
            public double clockrate;
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
                    case Mode.Catch: {
                        var attr = result.fruit.ToNullable()!.Value;
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
            
            Beatmap beatmap = await LoadBeatmap(map);
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
                    p.Accuracy(acc);
                    return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
                })
                .ToList();

            data.mode = map.Mode.ToRosu();

            return data;
        }

        async public static Task<Beatmap> LoadBeatmap(API.OSU.Models.Beatmap bm)
        {
            try
            {   
                byte[]? f = null;
                // 检查有没有本地的谱面
                if (File.Exists($"./work/beatmap/{bm.BeatmapId}.osu")) {
                    f = await File.ReadAllBytesAsync($"./work/beatmap/{bm.BeatmapId}.osu");
                    if (bm.Checksum is not null) {
                        using (var md5 = MD5.Create())
                        {
                            var hash = md5.ComputeHash(f);
                            var hash_online = System.Convert.FromHexString(bm.Checksum);

                            if (!hash.SequenceEqual(hash_online)) {
                                // 删除本地的谱面
                                File.Delete($"./work/beatmap/{bm.BeatmapId}.osu");
                                f = null;
                            }
                        }
                    }
                }

                if (f is null) {
                    // 下载谱面
                    await API.OSU.BeatmapFileChecker(bm.BeatmapId);
                    f = await File.ReadAllBytesAsync($"./work/beatmap/{bm.BeatmapId}.osu");
                }

                // 读取铺面
                return Beatmap.FromBytes(
                    f
                );
            }
            catch
            {
                // 加载失败，删除重新抛异常
                File.Delete($"./work/beatmap/{bm.BeatmapId}.osu");
                throw;
            }
        }

        async public static Task<Draw.ScorePanelData> CalculatePanelData(API.OSU.Models.ScoreLazer score)
        {
            var data = new Draw.ScorePanelData
            {
                scoreInfo = score
            };
            var statistics = data.scoreInfo.Statistics;

            Beatmap beatmap = await LoadBeatmap(data.scoreInfo.Beatmap!);

            var builder = BeatmapAttributesBuilder.New();

            var mode = API.OSU.Enums.Int2Mode(data.scoreInfo.ModeInt);
            Mode rmode;
            if (mode is null)
            {
                rmode = beatmap.Mode();
            } else {
                rmode = mode.Value.ToRosu();
            }

            var mods = Mods.FromJson(Serializer.Json.Serialize(data.scoreInfo.Mods), rmode);

            builder.Mode(rmode);
            builder.Mods(mods);
            var bmAttr = builder.Build(beatmap);
            var bpm = bmAttr.clock_rate * beatmap.Bpm();

            var p = Performance.New();
            p.Mode(rmode);
            p.Mods(mods);
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
                    p.Mode(rmode);
                    p.Mods(mods);
                    p.Accuracy(acc);
                    return PPInfo.New(p.Calculate(beatmap), bmAttr, bpm).ppStat;
                })
                .ToList();            

            data.mode = rmode;

            return data;
        }
    }
}
