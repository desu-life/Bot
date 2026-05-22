using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.Json;
using KanonBot.Serializer;

namespace KanonBot.I18n;

/// <summary>
/// 翻译数据存储：从嵌入式 JSON 资源加载翻译到内存
/// </summary>
public class TranslationStore
{
    private readonly ConcurrentDictionary<Locale, Dictionary<string, string>> _translations = new();
    private static readonly Locale FallbackLocale = Locale.ZhCN;

    /// <summary>
    /// 从程序集的嵌入式资源加载所有翻译文件
    /// </summary>
    public void LoadFromEmbeddedResources(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var name in resourceNames)
        {
            // 资源名格式: KanonBot.res.i18n.zh-CN.json
            if (!name.Contains("i18n"))
                continue;

            var locale = ExtractLocaleFromResourceName(name);
            if (locale is null)
                continue;

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
                continue;

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var dict = Json.Deserialize<Dictionary<string, string>>(json);
            if (dict is not null)
            {
                _translations[locale.Value] = dict;
                Log.Information("Loaded i18n translations for {Locale} ({Count} keys)", locale.Value.ToCode(), dict.Count);
            }
        }
    }

    /// <summary>
    /// 从文件系统加载翻译（用于开发/测试）
    /// </summary>
    public void LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var locale = LocaleExtensions.ParseLocale(fileName);
            var json = File.ReadAllText(file);
            var dict = Json.Deserialize<Dictionary<string, string>>(json);
            if (dict is not null)
            {
                _translations[locale] = dict;
                Log.Information("Loaded i18n translations from file for {Locale} ({Count} keys)", locale.ToCode(), dict.Count);
            }
        }
    }

    /// <summary>
    /// 直接加载翻译字典（用于测试）
    /// </summary>
    public void LoadDirect(Locale locale, Dictionary<string, string> translations)
    {
        _translations[locale] = translations;
    }

    /// <summary>
    /// 获取翻译文本，找不到则回退
    /// </summary>
    public string? Get(string key, Locale locale)
    {
        // 优先查目标语言
        if (_translations.TryGetValue(locale, out var dict) && dict.TryGetValue(key, out var value))
            return value;

        // 回退到默认语言
        if (locale != FallbackLocale &&
            _translations.TryGetValue(FallbackLocale, out var fallbackDict) &&
            fallbackDict.TryGetValue(key, out var fallbackValue))
        {
            Log.Debug("i18n key '{Key}' missing for {Locale}, falling back to {Fallback}", key, locale.ToCode(), FallbackLocale.ToCode());
            return fallbackValue;
        }

        return null;
    }

    /// <summary>
    /// 检查 key 是否存在于任意语言
    /// </summary>
    public bool HasKey(string key)
        => _translations.Values.Any(d => d.ContainsKey(key));

    /// <summary>
    /// 获取已加载的所有语言
    /// </summary>
    public IEnumerable<Locale> LoadedLocales => _translations.Keys;

    private static Locale? ExtractLocaleFromResourceName(string resourceName)
    {
        // 格式: KanonBot.res.i18n.zh-CN.json 或类似
        var parts = resourceName.Split('.');
        // 找 "i18n" 后面的部分作为 locale code
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "i18n" && i + 1 < parts.Length - 1)
            {
                // 可能是 "zh-CN" 或多段的情况
                var localeCode = parts[i + 1];
                // 处理 zh-CN 被拆分为 zh-CN 的情况（. 不会拆分 -）
                return LocaleExtensions.ParseLocale(localeCode);
            }
        }
        return null;
    }
}
