namespace KanonBot.API.OSU;

public static class OSUExtensions
{
    public static string ToStr(this UserScoreType type)
    {
        return type switch
        {
            UserScoreType.Firsts => "firsts",
            UserScoreType.Recent => "recent",
            UserScoreType.Best => "best",
            _ => throw new ArgumentException(),
        };
    }

    public static string ToStr(this Mode mode)
    {
        return mode switch
        {
            Mode.OSU => "osu",
            Mode.Taiko => "taiko",
            Mode.Fruits => "fruits",
            Mode.Mania => "mania",
            _ => throw new NotSupportedException("未知的模式"),
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
            _ => throw new NotSupportedException("未知的模式")
        };
    }

    public static Mode? ToMode(this string value)
    {
        value = value.ToLower(); // 大写字符转小写
        return value switch
        {
            "osu" => Mode.OSU,
            "taiko" => Mode.Taiko,
            "fruits" => Mode.Fruits,
            "catch" => Mode.Fruits,
            "mania" => Mode.Mania,
            _ => null
        };
    }

    public static Mode? ToMode(this int value)
    {
         return value switch
        {
            0 => Mode.OSU,
            1 => Mode.Taiko,
            2 => Mode.Fruits,
            3 => Mode.Mania,
            _ => null
        };
    }

}