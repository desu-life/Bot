using System.Text.RegularExpressions;
using KanonBot.Drivers;

namespace KanonBot.I18n;

/// <summary>
/// 核心本地化服务：获取翻译文本并进行格式化
/// </summary>
public partial class Localizer
{
    private readonly TranslationStore _store;
    private readonly LocaleResolver _resolver;

    private static Localizer? _instance;
    public static Localizer Instance =>
        _instance
        ?? throw new InvalidOperationException(
            "Localizer has not been initialized. Call Localizer.Initialize() first."
        );

    public TranslationStore Store => _store;
    public LocaleResolver Resolver => _resolver;

    public Localizer(TranslationStore store, LocaleResolver resolver)
    {
        _store = store;
        _resolver = resolver;
    }

    /// <summary>
    /// 初始化全局 Localizer 实例
    /// </summary>
    public static Localizer Initialize(IUserLocaleProvider? userLocaleProvider = null)
    {
        var store = new TranslationStore();
        store.Load();

        var resolver = new LocaleResolver(userLocaleProvider);
        _instance = new Localizer(store, resolver);
        return _instance;
    }

    /// <summary>
    /// 初始化用于测试的实例
    /// </summary>
    public static Localizer InitializeForTest(
        TranslationStore store,
        LocaleResolver? resolver = null
    )
    {
        _instance = new Localizer(store, resolver ?? new LocaleResolver());
        return _instance;
    }

    /// <summary>
    /// 获取翻译文本（按语言）
    /// </summary>
    /// <param name="key">翻译 key，如 "error.user_not_found"</param>
    /// <param name="locale">目标语言</param>
    /// <param name="args">格式化参数，支持 {0}, {1} 位置参数</param>
    public string Get(string key, Locale locale, params object[] args)
    {
        var template = _store.Get(key, locale);
        if (template is null)
        {
            Log.Warning("i18n key '{Key}' not found for any locale", key);
            return key; // 返回 key 本身，便于发现缺失翻译
        }

        if (args.Length == 0)
            return template;

        return FormatTemplate(template, args);
    }

    /// <summary>
    /// 获取翻译文本（根据 Target 自动判定语言，注入平台前缀）
    /// </summary>
    public string Get(string key, Target target, params object[] args)
    {
        var locale = _resolver.Resolve(target.sender, target.platform);
        return GetWithPlatform(key, locale, target.platform, args);
    }

    /// <summary>
    /// 获取翻译文本（异步版本，可查询用户偏好）
    /// </summary>
    public async Task<string> GetAsync(string key, Target target, params object[] args)
    {
        var locale = await _resolver.ResolveAsync(target.sender, target.platform);
        return GetWithPlatform(key, locale, target.platform, args);
    }

    /// <summary>
    /// 获取翻译文本，自动注入平台前缀变量
    /// </summary>
    public string GetWithPlatform(
        string key,
        Locale locale,
        Platform platform,
        params object[] args
    )
    {
        var template = _store.Get(key, locale);
        if (template is null)
        {
            Log.Warning("i18n key '{Key}' not found for any locale", key);
            return key;
        }

        var prefix = LocaleResolver.GetPlatformPrefix(platform);
        // 替换 {prefix} 占位符
        template = template.Replace("{prefix}", prefix);

        if (args.Length == 0)
            return template;

        return FormatTemplate(template, args);
    }

    /// <summary>
    /// 获取 Target 对应的语言（同步）
    /// </summary>
    public Locale GetLocale(Target target) => _resolver.Resolve(target.sender, target.platform);

    /// <summary>
    /// 获取 Target 对应的语言（异步，可查用户偏好）
    /// </summary>
    public Task<Locale> GetLocaleAsync(Target target) =>
        _resolver.ResolveAsync(target.sender, target.platform);

    private static string FormatTemplate(string template, object[] args)
    {
        // 支持 {0}, {1}, ... 位置参数
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            Log.Warning("i18n format error for template: {Template}", template);
            return template;
        }
    }
}
