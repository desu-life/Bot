using KanonBot.Drivers;

namespace KanonBot.I18n;

/// <summary>
/// 用户语言偏好提供者接口（预留给服务端实现）
/// </summary>
public interface IUserLocaleProvider
{
    /// <summary>
    /// 获取用户设置的语言偏好
    /// </summary>
    /// <param name="userId">用户平台 ID</param>
    /// <param name="platform">用户所在平台</param>
    /// <returns>用户设置的语言，null 表示未设置</returns>
    Task<Locale?> GetUserLocaleAsync(string userId, Platform platform);
}

/// <summary>
/// 默认实现：始终返回 null（等服务端就绪后替换）
/// </summary>
public class NullUserLocaleProvider : IUserLocaleProvider
{
    public Task<Locale?> GetUserLocaleAsync(string userId, Platform platform) =>
        Task.FromResult<Locale?>(null);
}
