using KanonBot.API.OSU;

namespace KanonBot.API.PPYSB;

public static class PPYSBExtensions
{
    public static OSU.Models.Status ToOsu(this Models.Status status)
    {
        return status switch
        {
            Models.Status.NotSubmitted => OSU.Models.Status.Unknown,
            Models.Status.UpdateAvailable => OSU.Models.Status.Unknown,
            Models.Status.Pending => OSU.Models.Status.Pending,
            Models.Status.Ranked => OSU.Models.Status.Ranked,
            Models.Status.Approved => OSU.Models.Status.Approved,
            Models.Status.Qualified => OSU.Models.Status.Qualified,
            Models.Status.Loved => OSU.Models.Status.Loved,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

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
            Mode.OSU => "vn!std",
            Mode.Taiko => "vn!taiko",
            Mode.Fruits => "vn!catch",
            Mode.Mania => "vn!mania",
            Mode.RelaxOsu => "rx!std",
            Mode.RelaxTaiko => "rx!taiko",
            Mode.RelaxFruits => "rx!catch",
            Mode.RelaxMania => "rx!mania",
            Mode.AutoPilotOsu => "ap!std",
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

    public static Mode? ParsePpysbMode(this string value)
    {
        value = value.ToLower(); // 大写字符转小写
        return value switch
        {
            "0" or "osu" or "std" => Mode.OSU,
            "1" or "taiko" or "tko" => Mode.Taiko,
            "2" or "fruits" or "catch" or "ctb" => Mode.Fruits,
            "3" or "mania" or "m" => Mode.Mania,
            "4" or "rxosu" or "rx!std" or "rxstd" => Mode.RelaxOsu,
            "5" or "rxtaiko" or "rx!taiko" => Mode.RelaxTaiko,
            "6" or "rxctb" or "rx!catch" => Mode.RelaxFruits,
            "7" or "rxmania" or "rx!mania" => Mode.RelaxMania,
            "8" or "aposu" or "ap!std" or "apstd" => Mode.AutoPilotOsu,
            "9" or "aptaiko" or "ap!taiko" => Mode.AutoPilotTaiko,
            "10" or "apctb" or "ap!catch" => Mode.AutoPilotFruits,
            "11" or "apmania" or "ap!mania" => Mode.AutoPilotMania,
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