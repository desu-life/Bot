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
using osu.Game.Rulesets.Catch.Difficulty;

namespace OsuPP;

public class Program
{
    public static void Main(string[] args)
    {
        var j = """
                        [
                            { "acronym": "HD" },
                            { "acronym": "CL" },
                        ]
                        """;
        var beatmap = new CalculatorWorkingBeatmap(File.OpenRead("resources/657916.osu"));
        var c = Calculater.New(beatmap);
        c.Mode(0);
        c.Mods(j);
        c.N300 = 1300;
        c.N100 = 66;
        c.N50 = 1;
        c.NMiss = 1;
        c.accuracy = 96.65;
        c.combo = 1786;

        var dattr = c.CalculateDifficulty();
        Console.WriteLine(JsonConvert.SerializeObject(dattr));

        var attr = c.Calculate();
        Console.WriteLine(JsonConvert.SerializeObject(attr));
    }
}