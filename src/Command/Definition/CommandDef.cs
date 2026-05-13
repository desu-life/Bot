using CommandSystem.Parsing;

namespace CommandSystem.Definition;

/// <summary>
/// 指令定义
/// </summary>
public class CommandDef
{
    public string Name { get; init; } = "";
    public List<string> Aliases { get; init; } = [];
    public bool LegacyStartsWithMatch { get; init; } = false;
    public List<string> ExcludePrefixes { get; init; } = [];
    public List<ArgDef> Args { get; init; } = [];
    public List<FlagDef> Flags { get; init; } = [];
}

/// <summary>
/// 指令注册表，存储 ICommand 实例
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new();

    public void Register(ICommand command)
    {
        var def = command.Definition;
        _commands[def.Name] = command;
        foreach (var alias in def.Aliases)
            _commands[alias] = command;
    }

    public bool TryGet(string name, out ICommand? command) => _commands.TryGetValue(name, out command);

    public (ICommand? command, string rawArgs) MatchCommand(string body)
    {
        var match = _commands
            .Where(kv => body.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kv => kv.Key.Length)
            .Select(kv => (kv.Value, kv.Key))
            .FirstOrDefault(pair =>
            {
                var (cmd, key) = pair;
                var def = cmd.Definition;

                // 检查排除前缀
                if (def.ExcludePrefixes.Any(ex => body.StartsWith(ex, StringComparison.OrdinalIgnoreCase)))
                    return false;

                // StartsWith 匹配或精确匹配（name 后必须是结尾或空白）
                return def.LegacyStartsWithMatch
                    || body.Length == key.Length
                    || char.IsWhiteSpace(body[key.Length]);
            });

        if (match == default)
            return (null, "");

        var (command, matchedKey) = match;
        var rawArgs = body[matchedKey.Length..].TrimStart();
        return (command, rawArgs);
    }
}
