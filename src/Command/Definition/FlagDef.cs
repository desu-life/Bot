namespace CommandSystem.Definition;

/// <summary>
/// Flag 定义，对应 & 后面的内容
/// </summary>
public class FlagDef
{
    private string? _slashName;

    /// <summary>
    /// 字段名，用于 ParsedCommand.Flags 的 key
    /// </summary>
    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    /// <summary>
    /// & 后面跟的字符串，空字符串表示单独的 &
    /// 例："sb" → &sb，"" → &
    /// </summary>
    public string Value { get; init; } = "";

    /// <summary>
    /// Slash 平台对应的参数名，例："is_sb"
    /// </summary>
    public string SlashName
    {
        get => string.IsNullOrWhiteSpace(_slashName) ? Name : _slashName;
        init => _slashName = value;
    }
}
