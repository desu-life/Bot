using KanonBot.API.OSU;

namespace KanonBot.API.PPYSB;

public static class PPYSBExtensions
{
    public static string ToStr(this UserScoreType type)
    {
        return type switch
        {
            UserScoreType.Recent => "recent",
            UserScoreType.Best => "best",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static string ToDisplay(this Mode mode)
    {
        return mode switch
        {
            Mode.OSU => "vn!standard",
            Mode.Taiko => "vn!taiko",
            Mode.Fruits => "vn!catch",
            Mode.Mania => "vn!mania",
            Mode.RelaxOsu => "rx!standard",
            Mode.RelaxTaiko => "rx!taiko",
            Mode.RelaxFruits => "rx!catch",
            Mode.RelaxMania => "rx!mania",
            Mode.AutoPilotOsu => "ap!standard",
            Mode.AutoPilotTaiko => "ap!taiko",
            Mode.AutoPilotFruits => "ap!catch",
            Mode.AutoPilotMania => "ap!mania",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
    
    public static int ToNum(this Mode mode)
    {
        return mode switch
        {
            Mode.OSU => 0,
            Mode.Taiko => 1,
            Mode.Fruits => 2,
            Mode.Mania => 3,
            Mode.RelaxOsu => 4,
            Mode.RelaxTaiko => 5,
            Mode.RelaxFruits => 6,
            Mode.RelaxMania => 7,
            Mode.AutoPilotOsu => 8,
            Mode.AutoPilotTaiko => 9,
            Mode.AutoPilotFruits => 10,
            Mode.AutoPilotMania => 11,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool IsSupported(this Mode mode)
    {
        return mode switch
        {
            Mode.OSU => true,
            Mode.Taiko => true,
            Mode.Fruits => true,
            Mode.Mania => true,
            Mode.RelaxOsu => true,
            Mode.RelaxTaiko => true,
            Mode.RelaxFruits => true,
            Mode.RelaxMania => false,
            Mode.AutoPilotOsu => true,
            Mode.AutoPilotTaiko => false,
            Mode.AutoPilotFruits => false,
            Mode.AutoPilotMania => false,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static Mode? ParsePpysbMode(this string value)
    {
        value = value.ToLower(); // 大写字符转小写
        return value switch
        {
            "0" or "osu" or "std" => Mode.OSU,
            "1" or "taiko" or "tko" => Mode.Taiko,
            "2" or "fruits" or "catch" or "ctb" => Mode.Fruits,
            "3" or "mania" or "m" => Mode.Mania,
            "4" or "rx0" or "rxosu" or "rxstd" => Mode.RelaxOsu,
            "5" or "rx1" or "rxtaiko" or "rxtko" => Mode.RelaxTaiko,
            "6" or "rx2" or "rxfruits" or "rxcatch" or "rxctb" => Mode.RelaxFruits,
            "7" or "rx3" or "rxmania" or "rxm" => Mode.RelaxMania,
            "8" or "ap0" or "aposu" or "apstd" => Mode.AutoPilotOsu,
            "9" or "ap1" or "aptaiko" or "aptko" => Mode.AutoPilotTaiko,
            "10" or "ap2" or "apfruits" or "apcatch" or "apctb" => Mode.AutoPilotFruits,
            "11" or "ap3" or "apmania" or "apm" => Mode.AutoPilotMania,
            _ => null
        };
    }

    public static Mode? ToPpysbMode(this int value)
    {
         return value switch
        {
            0 => Mode.OSU,
            1 => Mode.Taiko,
            2 => Mode.Fruits,
            3 => Mode.Mania,
            4 => Mode.RelaxOsu,
            5 => Mode.RelaxTaiko,
            6 => Mode.RelaxFruits,
            7 => Mode.RelaxMania,
            8 => Mode.AutoPilotOsu,
            9 => Mode.AutoPilotTaiko,
            10 => Mode.AutoPilotFruits,
            11 => Mode.AutoPilotMania,
            _ => null
        };
    }

}