namespace KanonBot.API.OSU;

public static class OSUExtensions
{
    public static string ToStr(this OSU.Enums.UserScoreType type)
    {
        return Utils.GetObjectDescription(type)!;
    }
    public static string ToStr(this OSU.Enums.Mode mode)
    {
        return Utils.GetObjectDescription(mode)!;
    }

    public static int ToNum(this OSU.Enums.Mode mode)
    {
        return mode switch
        {
            OSU.Enums.Mode.OSU => 0,
            OSU.Enums.Mode.Taiko => 1,
            OSU.Enums.Mode.Fruits => 2,
            OSU.Enums.Mode.Mania => 3,
            _ => throw new ArgumentException("UNKNOWN MODE")
        };
    }
}