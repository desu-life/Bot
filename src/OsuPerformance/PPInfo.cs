using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.LegacyImage;
using LanguageExt.ClassInstances.Pred;
using static KanonBot.API.OSU.OSUExtensions;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

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

    public static PPInfo New(
        API.OSU.Models.ScoreLazer score,
        osu.Game.Rulesets.Difficulty.PerformanceAttributes result,
        osu.Game.Rulesets.Difficulty.DifficultyAttributes dAttr,
        RosuPP.BeatmapAttributes bmAttr,
        double bpm,
        double clockrate
    )
    {
        if (result is osu.Game.Rulesets.Osu.Difficulty.OsuPerformanceAttributes osu)
        {
            var dosu = (dAttr as osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes)!;
            return new PPInfo()
            {
                star = dosu.StarRating,
                CS = bmAttr.cs,
                HP = bmAttr.hp,
                AR = bmAttr.ar,
                OD = bmAttr.od,
                accuracy = osu.Accuracy,
                maxCombo = (uint)dosu.MaxCombo,
                bpm = bpm,
                clockrate = clockrate,
                ppStat = new PPInfo.PPStat()
                {
                    total = osu.Total,
                    aim = osu.Aim,
                    speed = osu.Speed,
                    acc = osu.Accuracy,
                    strain = null,
                    flashlight = osu.Flashlight,
                },
                ppStats = null
            };
        }
        if (result is osu.Game.Rulesets.Taiko.Difficulty.TaikoPerformanceAttributes taiko)
        {
            var dtaiko = (dAttr as osu.Game.Rulesets.Taiko.Difficulty.TaikoDifficultyAttributes)!;
            return new PPInfo()
            {
                star = dtaiko.StarRating,
                CS = bmAttr.cs,
                HP = bmAttr.hp,
                AR = bmAttr.ar,
                OD = bmAttr.od,
                accuracy = taiko.Accuracy,
                maxCombo = (uint)dtaiko.MaxCombo,
                bpm = bpm,
                clockrate = clockrate,
                ppStat = new PPInfo.PPStat()
                {
                    total = taiko.Total,
                    aim = null,
                    speed = null,
                    acc = taiko.Accuracy,
                    strain = taiko.Difficulty,
                    flashlight = null,
                },
                ppStats = null
            };
        }
        if (result is osu.Game.Rulesets.Mania.Difficulty.ManiaPerformanceAttributes mania)
        {
            var dmania = (dAttr as osu.Game.Rulesets.Mania.Difficulty.ManiaDifficultyAttributes)!;
            return new PPInfo()
            {
                star = dmania.StarRating,
                CS = bmAttr.cs,
                HP = bmAttr.hp,
                AR = bmAttr.ar,
                OD = bmAttr.od,
                accuracy = null,
                maxCombo = (uint)dmania.MaxCombo,
                bpm = bpm,
                clockrate = clockrate,
                ppStat = new PPInfo.PPStat()
                {
                    total = mania.Total,
                    aim = null,
                    speed = null,
                    acc = null,
                    strain = mania.Difficulty,
                    flashlight = null,
                },
                ppStats = null
            };
        }
        if (result is osu.Game.Rulesets.Catch.Difficulty.CatchPerformanceAttributes fruit)
        {
            var dfruit = (dAttr as osu.Game.Rulesets.Catch.Difficulty.CatchDifficultyAttributes)!;
            return new PPInfo()
            {
                star = dfruit.StarRating,
                CS = bmAttr.cs,
                HP = bmAttr.hp,
                AR = bmAttr.ar,
                OD = bmAttr.od,
                accuracy = null,
                maxCombo = (uint)dfruit.MaxCombo,
                bpm = bpm,
                clockrate = clockrate,
                ppStat = new PPInfo.PPStat()
                {
                    total = fruit.Total,
                    aim = null,
                    speed = null,
                    acc = null,
                    strain = null,
                    flashlight = null,
                },
                ppStats = null
            };
        }
        throw new ArgumentOutOfRangeException();
    }

    public static PPInfo New(RosuPP.PerformanceAttributes result, RosuPP.BeatmapAttributes bmAttr, double bpm)
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

    public static PPInfo New(SBRosuPP.PerformanceAttributes result, SBRosuPP.BeatmapAttributes bmAttr, double bpm)
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
