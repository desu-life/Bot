using KanonBot.Drivers;

namespace KanonBot.I18n;

public static class TargetI18nExtensions
{
    public static Locale GetLocale(this Target target) => Localizer.Instance.GetLocale(target);

    public static Task<Locale> GetLocaleAsync(this Target target) =>
        Localizer.Instance.GetLocaleAsync(target);

    /// <summary>
    /// 获取翻译文本
    /// </summary>
    public static string T(this Target target, string key, params object[] args) =>
        Localizer.Instance.Get(key, target, args);

    /// <summary>
    /// 翻译并回复：target.reply(target.T(key)) 的简写
    /// </summary>
    public static Task<bool> Treply(this Target target, string key, params object[] args) =>
        target.reply(Localizer.Instance.Get(key, target, args));

    public static Task<bool> TprivateReply(this Target target, string key, params object[] args) =>
        target.privateReply(Localizer.Instance.Get(key, target, args));

    public static string GetPrefix(this Target target) =>
        LocaleResolver.GetPlatformPrefix(target.platform);
}
