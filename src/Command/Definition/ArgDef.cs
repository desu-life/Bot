namespace CommandSystem.Definition;

/// <summary>
/// 参数前缀类型（对应 Legacy 的 : # + &）
/// </summary>
public enum ArgPrefix
{
    None, // 普通位置参数（arg1）
    Colon, // :mode
    Hash, // #order / #date / #bid
    Plus, // +mods
    And, // &flag
}

/// <summary>
/// arg1 的解析策略（只对 Prefix=None 的参数生效）
/// </summary>
public enum ParseStrategy
{
    Simple, // 直接取 arg1 字符串
    Ambiguous, // arg1 复合数据，根据具体数据判断
}

/// <summary>
/// 单个参数定义
/// </summary>
public class ArgDef
{
    public string Name { get; init; } = "";
    public ArgPrefix Prefix { get; init; } = ArgPrefix.None;
    public ParseStrategy Strategy { get; init; } = ParseStrategy.Simple;

    /// <summary>
    /// 把原始字符串转成目标类型，默认直接返回字符串
    /// </summary>
    public Func<string, object?> Parse { get; init; } = s => s;
}
