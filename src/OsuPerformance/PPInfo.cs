using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KanonBot.Image;
using LanguageExt.ClassInstances.Pred;
using static KanonBot.API.OSU.OSUExtensions;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public partial class PPInfo
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
}
