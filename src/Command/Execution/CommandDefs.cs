using CommandSystem.Definition;

namespace CommandSystem.Execution;

/// <summary>
/// 通用 Parse 辅助函数
/// </summary>
public static class CommandDefs
{
    public static int? ParseInt(string s) => int.TryParse(s, out var n) ? n : null;

    public static Range? ParseRange(string s)
    {
        if (string.IsNullOrEmpty(s))
            return null;

        if (ParseInt(s) is int single)
            return new Range(0, single);

        var parts = s.Split(["..", "-"], StringSplitOptions.RemoveEmptyEntries);
        if (
            parts.Length == 2
            && int.TryParse(parts[0], out var start)
            && int.TryParse(parts[1], out var end)
        )
            return new Range(start, end);
        return null;
    }
}
