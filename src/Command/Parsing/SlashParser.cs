using CommandSystem.Definition;

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
}
