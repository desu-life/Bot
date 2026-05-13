namespace CommandSystem.Parsing;

/// <summary>
/// 解析结果，平台无关，Executor 直接消费
/// </summary>
public class ParsedCommand
{
    public string CommandName { get; init; } = "";

    /// <summary>
    /// 普通参数，key = ArgDef.Name
    /// </summary>
    public Dictionary<string, string?> Args { get; init; } = new();

    /// <summary>
    /// Flag 参数，key = FlagDef.Name，value = bool（或带值的 flag 为 string）
    /// </summary>
    public Dictionary<string, bool?> Flags { get; init; } = new();

    /// <summary>
    /// 是否查询自己（username 为空时为 true）
    /// </summary>
    public bool SelfQuery { get; set; } = false;

    public Dictionary<string, Func<string, object?>> Parse { private get; init; } = [ ];

    // ── 快捷取值 ──────────────────────────────────

    public bool Has(string key) => Args.ContainsKey(key);

    public T? Get<T>(string key)
    {
        Args.TryGetValue(key, out var val);
        Parse.TryGetValue(key, out var parser);
        if (val is null || parser is null)
            return default;

        return parser(val) is T parsed ? parsed : default;
    }

    public bool Flag(string key)
    {
        Flags.TryGetValue(key, out var val);
        return val is true;
    }
}
