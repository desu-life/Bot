using System.Collections.Concurrent;
using KanonBot.Drivers;

namespace KanonBot.I18n;

/// <summary>
/// 语言决策逻辑：用户偏好 → 平台默认
/// </summary>
public class LocaleResolver
{
    private readonly IUserLocaleProvider _provider;
    private readonly ConcurrentDictionary<string, (Locale locale, DateTime cachedAt)> _cache = new();
    private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(10);

    public LocaleResolver(IUserLocaleProvider? provider = null)
    {
        _provider = provider ?? new NullUserLocaleProvider();
    }

    /// <summary>
    /// 获取平台默认语言
    /// </summary>
    public static Locale GetPlatformDefault(Platform platform) => platform switch
    {
        Platform.Discord => Locale.En,
        Platform.OneBot => Locale.ZhCN,
        Platform.Guild => Locale.ZhCN,
        _ => Locale.ZhCN,
    };

    /// <summary>
    /// 获取平台的命令前缀
    /// </summary>
    public static string GetPlatformPrefix(Platform platform) => platform switch
    {
        Platform.Discord => "/",
        Platform.OneBot => "!",
        Platform.Guild => "!",
        _ => "!",
    };

    /// <summary>
    /// 解析目标语言：用户偏好 > 平台默认
    /// </summary>
    public async Task<Locale> ResolveAsync(string? userId, Platform platform)
    {
        if (userId is null)
            return GetPlatformDefault(platform);

        var cacheKey = $"{platform}:{userId}";

        // 检查缓存
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.cachedAt < CacheTTL)
                return cached.locale;
            _cache.TryRemove(cacheKey, out _);
        }

        // 查询用户偏好
        var userLocale = await _provider.GetUserLocaleAsync(userId, platform);
        var resolved = userLocale ?? GetPlatformDefault(platform);

        // 写入缓存
        _cache[cacheKey] = (resolved, DateTime.UtcNow);
        return resolved;
    }

    /// <summary>
    /// 同步版本（使用缓存或平台默认，不阻塞）
    /// </summary>
    public Locale Resolve(string? userId, Platform platform)
    {
        if (userId is null)
            return GetPlatformDefault(platform);

        var cacheKey = $"{platform}:{userId}";
        if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.cachedAt < CacheTTL)
            return cached.locale;

        return GetPlatformDefault(platform);
    }

    /// <summary>
    /// 使缓存失效（用户更改语言设置后调用）
    /// </summary>
    public void InvalidateCache(string userId, Platform platform)
    {
        var cacheKey = $"{platform}:{userId}";
        _cache.TryRemove(cacheKey, out _);
    }
}
