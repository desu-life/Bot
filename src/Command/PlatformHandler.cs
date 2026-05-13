using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;

namespace CommandSystem;

/// <summary>
/// 平台适配层示例
/// Legacy（!cmd）和 Slash（/cmd）统一输出 ParsedCommand
/// </summary>
public class PlatformHandler
{
    private readonly CommandRegistry _registry = CommandDefs.BuildRegistry();
    private readonly LegacyParser _legacy = new();
    private readonly SlashParser _slash = new();

    // ── Legacy 入口（!info zhjk #10 :3 &sb）────────────
    public ParsedCommand? HandleLegacy(string message)
    {
        if (!message.StartsWith('!'))
            return null;

        var body = message[1..]; // 去掉 !

        var (matchedDef, matchedRawArgs) = _registry.MatchCommand(body);
        if (matchedDef is not null)
            return _legacy.Parse(matchedDef.Name, matchedRawArgs, matchedDef);
        return null;
    }

    // ── Slash 入口（Discord 传来的已解析 key-value）──────
    public ParsedCommand? HandleSlash(string cmdName, Dictionary<string, string> options)
    {
        if (!_registry.TryGet(cmdName, out var def) || def is null)
            return null;

        return _slash.Parse(cmdName, options, def);
    }
}
