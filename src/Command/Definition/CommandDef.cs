namespace CommandSystem.Definition;

/// <summary>
/// 指令定义
/// </summary>
public class CommandDef
{
    public string Name { get; init; } = "";
    public bool LegacyStartsWithMatch { get; init; } = false;
    public List<ArgDef> Args { get; init; } = [ ];
    public List<FlagDef> Flags { get; init; } = [ ];
}

/// <summary>
/// 指令注册表
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, CommandDef> _commands = new();

    public void Register(CommandDef def) => _commands[def.Name] = def;

    public bool TryGet(string name, out CommandDef? def) => _commands.TryGetValue(name, out def);

    public (CommandDef? def, string rawArgs) MatchCommand(string body)
    {
        var def = _commands
            .Values
            .Where(c => body.StartsWith(c.Name, StringComparison.Ordinal))
            .OrderByDescending(c => c.Name.Length)
            .FirstOrDefault(
                c =>
                    c.LegacyStartsWithMatch
                    || body.Length == c.Name.Length
                    || char.IsWhiteSpace(body[c.Name.Length])
            );

        var rawArgs = def is null ? "" : body[def.Name.Length..].TrimStart();
        return (def, rawArgs);
    }
}
