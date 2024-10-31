using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Skinning;
using System;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;

namespace OsuPP;

public static class Utils {
    public static double CalculateAccuracy(Dictionary<HitResult, int> statistics)
    {
        var countGreat = statistics[HitResult.Great];
        var countOk = statistics[HitResult.Ok];
        var countMeh = statistics[HitResult.Meh];
        var countMiss = statistics[HitResult.Miss];
        var total = countGreat + countOk + countMeh + countMiss;

        return (double)((6 * countGreat) + (2 * countOk) + countMeh) / (6 * total);
    }

    public static Ruleset? ParseRuleset(int mode) {
        if (mode == 0) {
            return new OsuRuleset();
        } else if (mode == 1) {
            return new TaikoRuleset();
        } else if (mode == 2) {
            return new CatchRuleset();
        } else if (mode == 3) {
            return new ManiaRuleset();
        }
        return null;
    }
}

public class OsuMods {
    public required osu.Game.Rulesets.Mods.Mod[] Mods { get; set; }

    public static OsuMods? FromJson(Ruleset ruleset, string json) {
        var mods = JsonConvert.DeserializeObject<osu.Game.Online.API.APIMod[]>(json);
        if (mods is null) {
            return null;
        } else{
            return new OsuMods { Mods = mods.Select(x => x.ToMod(ruleset)).ToArray() };
        }
    }
}


public class Calculater {
    public required WorkingBeatmap beatmap { get; set; }
    public required OsuMods? mods { get; set; }
    public required Ruleset ruleset { get; set; }
    public required osu.Game.Rulesets.Difficulty.DifficultyAttributes? difficultyAttributes { get; set; }

    public double? accuracy { get; set; }
    public uint? combo { get; set; }
    public uint? N300 { get; set; }
    public uint? N100 { get; set; }
    public uint? N50 { get; set; }
    public uint? NKatu { get; set; }
    public uint? NGeki { get; set; }
    public uint? NMiss { get; set; }
    public uint? SliderTailHit { get; set; }
    public uint? SliderTickMiss { get; set; }

    public static Calculater New(Ruleset ruleset, WorkingBeatmap beatmap) {
        return new Calculater {
            beatmap = beatmap,
            mods = null,
            ruleset = ruleset,
            difficultyAttributes = null
        };
    }

    public osu.Game.Rulesets.Mods.Mod[]? GetMods() {
        if (mods is not null) {
            return mods.Mods;
        } else {
            return null;
        }
    }

    public void Mods(string json) {
        mods = OsuMods.FromJson(ruleset, json);
    }

    public osu.Game.Rulesets.Difficulty.DifficultyAttributes CalculateDifficulty() {
        var difficultyCalculator = ruleset.CreateDifficultyCalculator(beatmap);

        if (mods is not null) {
            difficultyAttributes = difficultyCalculator.Calculate(mods.Mods);
        } else {
            difficultyAttributes = difficultyCalculator.Calculate();
        }

        return difficultyAttributes;
    }

    public osu.Game.Beatmaps.BeatmapDifficulty CalculateBeatmap() {
        var playable_beatmap = beatmap.GetPlayableBeatmap(ruleset.RulesetInfo, GetMods());
        return playable_beatmap.Difficulty;
    }

    public osu.Game.Rulesets.Difficulty.PerformanceAttributes Calculate() {

        if (difficultyAttributes is null) {
            CalculateDifficulty();
        }

        var scoreInfo = new ScoreInfo(beatmap.BeatmapInfo, ruleset.RulesetInfo, null);

        if (mods is not null) {
            scoreInfo.Mods = mods.Mods;
        }

        var statistics = new Dictionary<HitResult, int>();

        if (ruleset is CatchRuleset) {
            if (N100 is not null) {
                statistics[HitResult.LargeTickHit] = (int)N100;
            }

            if (N50 is not null) {
                statistics[HitResult.SmallTickHit] = (int)N50;
            }

            if (NKatu is not null) {
                statistics[HitResult.SmallTickMiss] = (int)NKatu;
            }
        } else {
            if (N100 is not null) {
                statistics[HitResult.Ok] = (int)N100;
            }

            if (N50 is not null) {
                statistics[HitResult.Meh] = (int)N50;
            }

            if (NKatu is not null) {
                statistics[HitResult.Good] = (int)NKatu;
            }
        }

        if (ruleset is OsuRuleset) {
            if (SliderTailHit is not null) {
                statistics[HitResult.SliderTailHit] = (int)SliderTailHit;
            }

            if (SliderTickMiss is not null) {
                statistics[HitResult.LargeTickMiss] = (int)SliderTickMiss;
            }
        }

        if (N300 is not null) {
            statistics[HitResult.Great] = (int)N300;
        }

        if (NGeki is not null) {
            statistics[HitResult.Perfect] = (int)NGeki;
        }

        if (NMiss is not null) {
            statistics[HitResult.Miss] = (int)NMiss;
        }

        if (combo is not null) {
            scoreInfo.MaxCombo = (int)combo;
        }

        if (accuracy is not null) {
            scoreInfo.Accuracy = accuracy.Value / 100.0;
        }


        scoreInfo.Statistics = statistics;

        var ppcalc = ruleset.CreatePerformanceCalculator()!;
        return ppcalc.Calculate(scoreInfo, difficultyAttributes!);
    }
}