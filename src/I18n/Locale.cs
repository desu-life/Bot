namespace KanonBot.I18n;

/// <summary>
/// 支持的语言区域
/// </summary>
public enum Locale
{
    /// <summary>简体中文</summary>
    ZhCN,
    /// <summary>English</summary>
    En,
}

public static class LocaleExtensions
{
    public static string ToCode(this Locale locale) => locale switch
    {
        Locale.ZhCN => "zh-CN",
        Locale.En => "en",
        _ => "zh-CN",
    };

    public static Locale ParseLocale(string code) => code.ToLowerInvariant() switch
    {
        "zh-cn" or "zh" or "zh_cn" => Locale.ZhCN,
        "en" or "en-us" or "en-gb" or "en_us" => Locale.En,
        _ => Locale.ZhCN,
    };
}
