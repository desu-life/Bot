namespace CommandSystem.Parsing;

/// <summary>
/// 处理 arg1 里 username + 数字共存的歧义情况
/// 逻辑来自原 BotCmdHelper.ParseArg1
/// </summary>
public static class AmbiguousResolver
{
    /// <summary>
    /// 通用解析器
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="parser">解析函数：尝试将串转换为 object，失败返回 null</param>
    /// <returns>(用户名字符串, 解析结果的字符串表达)</returns>
    public static (string? username, string? result) Resolve(
        string? input,
        Func<string, object?> parser
    )
    {
        input = input?.Trim();
        if (string.IsNullOrEmpty(input))
            return (null, null);

        // 1. 整体匹配
        var fullMatch = parser(input);
        if (fullMatch != null)
            return (null, fullMatch.ToString());

        // 2. 引号处理逻辑
        var startQ = input.IndexOf('"');
        if (startQ >= 0 && startQ < input.Length - 1)
        {
            var endQ = input.LastIndexOf('"');
            if (endQ > startQ)
            {
                var contentInQuotes = input[(startQ + 1)..endQ];
                object? foundObj = null;

                var prefix = input[..startQ].Trim();
                var suffix = input[(endQ + 1)..].Trim();

                var pMatch = parser(prefix);
                if (pMatch != null)
                    foundObj = pMatch;
                else
                    foundObj = parser(suffix);

                return (contentInQuotes, foundObj?.ToString());
            }
        }

        // 3. 空格分隔处理
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            // 尝试首部匹配
            var r0 = parser(parts[0]);
            if (r0 != null)
            {
                var u0 = string.Join(' ', parts.Skip(1));
                return (u0, r0.ToString());
            }

            // 尝试尾部匹配
            var lastIndex = parts.Length - 1;
            var rLast = parser(parts[lastIndex]);
            if (rLast != null)
            {
                var uLast = string.Join(' ', parts.Take(lastIndex));
                return (uLast, rLast.ToString());
            }
        }

        // 4. 默认当 username
        return (input, null);
    }
}
