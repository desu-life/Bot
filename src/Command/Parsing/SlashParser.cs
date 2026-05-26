using CommandSystem.Definition;
using System.Text;

namespace CommandSystem.Parsing;

/// <summary>
/// 解析 /cmd username=zhjk date=10 is_sb=true 格式
/// </summary>
public class SlashParser
{
    /// <summary>
    /// options = Discord 传来的 key-value 字典
    /// </summary>
    public ParsedCommand Parse(string cmdName, Dictionary<string, string> options, CommandDef def)
    {
        var result = new ParsedCommand
        {
            CommandName = cmdName,
            RawArgs = BuildRawArgs(options, def),
            Parse = def.Args.DistinctBy(a => a.Name).ToDictionary(a => a.Name, a => a.Parse)
        };

        // 初始化所有 flag 为 false
        foreach (var f in def.Flags)
            result.Flags[f.Name] = false;

        foreach (var (key, rawValue) in options)
        {
            // 先查 ArgDef（用 Name 匹配）
            var argDef = def.Args.FirstOrDefault(a => a.Name == key);
            if (argDef != null)
            {
                result.Args[argDef.Name] = rawValue;

                if (argDef.Name == "username")
                    result.SelfQuery = string.IsNullOrEmpty(rawValue);

                continue;
            }

            // 再查 FlagDef（用 SlashName 匹配）
            var flagDef = def.Flags.FirstOrDefault(f => f.SlashName == key);
            if (flagDef != null)
            {
                result.Flags[flagDef.Name] = rawValue.ToLower() is "true" or "1";
            }
        }

        // 没有提供 username 时，默认 self_query
        var userArg = def.Args.FirstOrDefault(a => a.Name == "username");
        if (userArg != null && !result.Args.ContainsKey("username"))
            result.SelfQuery = true;

        return result;
    }

    private static string BuildRawArgs(Dictionary<string, string> options, CommandDef def)
    {
        var raw = new StringBuilder();
        var added = new System.Collections.Generic.HashSet<string>();

        foreach (var argDef in def.Args)
        {
            if (!added.Add(argDef.Name))
                continue;

            if (!options.TryGetValue(argDef.Name, out var value) || string.IsNullOrWhiteSpace(value))
                continue;

            AppendRawArg(raw, argDef.Prefix, value);
        }

        foreach (var flagDef in def.Flags)
        {
            if (
                options.TryGetValue(flagDef.SlashName, out var value)
                && value.ToLowerInvariant() is "true" or "1"
            )
            {
                AppendRawArg(raw, ArgPrefix.And, flagDef.Value);
            }
        }

        return raw.ToString().Trim();
    }

    private static void AppendRawArg(StringBuilder raw, ArgPrefix prefix, string value)
    {
        if (raw.Length > 0)
            raw.Append(' ');

        var prefixChar = prefix switch
        {
            ArgPrefix.Colon => ':',
            ArgPrefix.Hash => '#',
            ArgPrefix.Plus => '+',
            ArgPrefix.And => '&',
            _ => '\0'
        };

        if (prefixChar != '\0')
            raw.Append(prefixChar);

        raw.Append(value);
    }
}
