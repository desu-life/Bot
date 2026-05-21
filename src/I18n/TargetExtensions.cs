using KanonBot.Drivers;

namespace KanonBot.I18n;

/// <summary>
/// Target 的 i18n 扩展方法
/// </summary>
public static class TargetI18nExtensions
{
    /// <summary>
    /// 获取 Target 的当前语言（同步，基于缓存/平台默认）
    /// </summary>
    public static Locale GetLocale(this Target target)
        => Localizer.Instance.GetLocale(target);

    /// <summary>
    /// 获取 Target 的当前语言（异步，可查用户偏好）
    /// </summary>
    public static Task<Locale> GetLocaleAsync(this Target target)
        => Localizer.Instance.GetLocaleAsync(target);

    /// <summary>
    /// 获取翻译文本
    /// </summary>
    public static string T(this Target target, string key, params object[] args)
        => Localizer.Instance.Get(key, target, args);

    /// <summary>
    /// 获取翻译文本（异步版本）
    /// </summary>
    public static Task<string> TAsync(this Target target, string key, params object[] args)
        => Localizer.Instance.GetAsync(key, target, args);

    /// <summary>
    /// 获取当前平台的命令前缀
    /// </summary>
    public static string GetPrefix(this Target target)
        => LocaleResolver.GetPlatformPrefix(target.platform);
}
