using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;

namespace CommandSystem;

/// <summary>
/// 平台适配层
/// Legacy（!cmd）和 Slash（/cmd）统一输出 (ICommand, ParsedCommand)
/// </summary>
public class PlatformHandler
{
    private readonly CommandRegistry _registry = CommandRegistrar.BuildRegistry();
    private readonly LegacyParser _legacy = new();
    private readonly SlashParser _slash = new();

    // ── Legacy 入口（!info zhjk #10 :3 &sb）────────────
    public (ICommand? command, ParsedCommand? parsed) HandleLegacy(string body)
    {
        var (matchedCmd, matchedRawArgs) = _registry.MatchCommand(body);
        if (matchedCmd is null)
            return (null, null);

        var def = matchedCmd.Definition;
        var parsed = _legacy.Parse(def.Name, matchedRawArgs, def);
        return (matchedCmd, parsed);
    }

    // ── Slash 入口（Discord 传来的已解析 key-value）──────
    public (ICommand? command, ParsedCommand? parsed) HandleSlash(
        string cmdName,
        Dictionary<string, string> options
    )
    {
        if (!_registry.TryGet(cmdName, out var cmd) || cmd is null)
            return (null, null);

        var def = cmd.Definition;
        var parsed = _slash.Parse(cmdName, options, def);
        return (cmd, parsed);
    }
}
