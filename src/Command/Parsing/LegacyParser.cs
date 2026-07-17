using System.Text;
using CommandSystem.Definition;

namespace CommandSystem.Parsing;

/// <summary>
/// 解析 !cmd arg1 :mode #order +mods &flag 格式
/// </summary>
public class LegacyParser
{
    // ── 状态机：把原始字符串切成各前缀的 bucket ──────────

    private static (Dictionary<ArgPrefix, string> Buckets, List<string> AndSegments) Tokenize(
        string cmd
    )
    {
        var buckets = new Dictionary<ArgPrefix, StringBuilder>
        {
            [ArgPrefix.None] = new(),
            [ArgPrefix.Colon] = new(),
            [ArgPrefix.Hash] = new(),
            [ArgPrefix.Plus] = new(),
            [ArgPrefix.And] = new(),
        };

        var andSegments = new List<StringBuilder>();
        var current = ArgPrefix.None;
        StringBuilder? currentAndSegment = null;

        foreach (var ch in cmd)
        {
            ArgPrefix? next = ch switch
            {
                ':' => ArgPrefix.Colon,
                '#' => ArgPrefix.Hash,
                '+' => ArgPrefix.Plus,
                '&' => ArgPrefix.And,
                _ => null,
            };

            if (next.HasValue)
            {
                current = next.Value;
                buckets[current].Append(ch); // 保留前缀字符，方便后续剥离
                currentAndSegment = null;

                if (current == ArgPrefix.And)
                {
                    currentAndSegment = new StringBuilder().Append(ch);
                    andSegments.Add(currentAndSegment);
                }
            }
            else
            {
                buckets[current].Append(ch);
                currentAndSegment?.Append(ch);
            }
        }

        return (
            buckets.ToDictionary(kv => kv.Key, kv => kv.Value.ToString().Trim()),
            andSegments.Select(segment => segment.ToString().Trim()).ToList()
        );
    }

    // ── 主解析 ───────────────────────────────────────────

    public ParsedCommand Parse(string cmdName, string rawArgs, CommandDef def)
    {
        var (buckets, andSegments) = Tokenize(rawArgs);
        var result = new ParsedCommand
        {
            CommandName = cmdName,
            RawArgs = rawArgs,
            Parse = def.Args.DistinctBy(a => a.Name).ToDictionary(a => a.Name, a => a.Parse)
        };

        // 解析带值参数和 flags（& 前缀）
        ParseAndArgs(andSegments, def, result);
        ParseFlags(andSegments, def, result);

        // 解析有明确前缀的 args（: # +）
        foreach (
            var argDef in def.Args.Where(a =>
                a.Prefix is not (ArgPrefix.None or ArgPrefix.And)
            )
        )
        {
            var raw = buckets[argDef.Prefix];
            if (string.IsNullOrWhiteSpace(raw))
            {
                result.Args[argDef.Name] = null;
                continue;
            }

            var stripped = raw[1..].Trim(); // 去掉前缀字符
            result.Args[argDef.Name] = stripped;
        }

        // 解析 arg1（Prefix=None）
        var arg1Raw = buckets[ArgPrefix.None].Trim();
        ParseArg1Args(arg1Raw, def, result);

        return result;
    }

    // ── arg1 解析，按 strategy 路由 ──────────────────────

    private static void ParseArg1Args(string arg1, CommandDef def, ParsedCommand result)
    {
        var noneArgs = def.Args.Where(a => a.Prefix == ArgPrefix.None).ToList();
        if (noneArgs.Count == 0)
            return;

        // 取第一个 arg 的策略决定解析方式
        var strategy = noneArgs[0].Strategy;

        switch (strategy)
        {
            case ParseStrategy.Simple:
                // 直接赋值给第一个 arg
                result.Args[noneArgs[0].Name] = string.IsNullOrEmpty(arg1) ? null : arg1;
                if (noneArgs[0].Name == "username")
                    result.SelfQuery = string.IsNullOrEmpty(arg1);
                break;

            case ParseStrategy.Ambiguous:
                var ambiguousDefs = noneArgs
                    .Where(a => a.Strategy == ParseStrategy.Ambiguous)
                    .ToList();

                if (ambiguousDefs.Count != 2)
                {
                    // 目前只处理 username + 数字 共存的情况，其他歧义场景不处理
                    result.Args[ambiguousDefs[0].Name] = string.IsNullOrEmpty(arg1) ? null : arg1;
                    if (ambiguousDefs[0].Name == "username")
                        result.SelfQuery = string.IsNullOrEmpty(arg1);
                    break;
                }

                var sharedDef = ambiguousDefs.FirstOrDefault(
                    d => result.Args.ContainsKey(d.Name) && result.Args[d.Name] != null
                );

                if (sharedDef != null)
                {
                    // 其中一个参数已经通过 # 解析了
                    // 那么剩下的那个 Ambiguous 参数就占有完整的 arg1
                    var theOtherDef = ambiguousDefs.First(d => d.Name != sharedDef.Name);

                    result.Args[theOtherDef.Name] = string.IsNullOrEmpty(arg1) ? null : arg1;
                    result.SelfQuery = string.IsNullOrEmpty(arg1);
                }
                else
                {
                    // 两个参数都没有通过 Prefix 解析，进入模糊推断
                    // 我们假设第一个是 String(username)，第二个是走自定义解析
                    var stringDef = ambiguousDefs[0];
                    var numericDef = ambiguousDefs[1];

                    var (strVal, numVal) = AmbiguousResolver.Resolve(arg1, numericDef.Parse);

                    result.Args[stringDef.Name] = strVal;
                    if (numVal is not null)
                        result.Args[numericDef.Name] = numVal;

                    result.SelfQuery = string.IsNullOrEmpty(strVal);
                }
                break;
        }
    }

    // ── & 参数和 Flag 解析 ─────────────────────────────────

    private static void ParseAndArgs(
        IReadOnlyList<string> andSegments,
        CommandDef def,
        ParsedCommand result
    )
    {
        var andArgs = def.Args.Where(a => a.Prefix == ArgPrefix.And).ToList();
        foreach (var argDef in andArgs)
            result.Args[argDef.Name] = null;

        foreach (var segment in andSegments)
        {
            var raw = segment[1..].Trim();
            var separator = raw.IndexOf('=');
            if (separator <= 0)
                continue;

            var legacyName = raw[..separator].Trim();
            var argDef = andArgs.FirstOrDefault(a =>
                a.LegacyName.Equals(legacyName, StringComparison.OrdinalIgnoreCase)
            );
            if (argDef is null)
            {
                result.Errors.Add($"Unknown option '&{legacyName}'.");
                continue;
            }

            var value = raw[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Errors.Add($"Option '&{argDef.LegacyName}' requires a value.");
                continue;
            }

            result.Args[argDef.Name] = value;
        }
    }

    private static void ParseFlags(
        IReadOnlyList<string> andSegments,
        CommandDef def,
        ParsedCommand result
    )
    {
        // 初始化所有 flag 为 false
        foreach (var f in def.Flags)
            result.Flags[f.Name] = false;

        foreach (var segment in andSegments)
        {
            var flagValue = segment[1..].Trim();
            if (flagValue.Contains('='))
                continue;

            var matched = def.Flags.FirstOrDefault(f => f.Value == flagValue);
            if (matched is not null)
                result.Flags[matched.Name] = true;
        }
    }
}
