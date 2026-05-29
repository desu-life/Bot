namespace KanonBot.Image.Takumi;

using System.Text.Json;
using System.Text.Json.Serialization;
using global::Takumi.Render.UniFFI;
using KanonBot.API.OSU;
using SixLabors.ImageSharp;

public static class TakumiHelper
{
    public static readonly string workingRoot = Path.Combine(
        Directory.GetCurrentDirectory(),
        "work"
    );

    private static readonly string cacheRoot = Path.Combine(workingRoot, "cache");

    public static readonly JsonSerializerOptions TemplateJsonOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static TemplateEngine CreateTemplateEngine(string templateRoot)
    {
        var templateEngine = new TemplateEngine();
        templateEngine.AddSearchPath(templateRoot);
        return templateEngine;
    }

    public static Renderer CreateTemplateRenderer(string templateRoot, string[] fontPaths)
    {
        var renderer = new Renderer();
        renderer.AddSearchPath(workingRoot);
        renderer.AddSearchPath(templateRoot);

        foreach (var path in fontPaths)
        {
            renderer.AddFontFile(path);
        }

        return renderer;
    }

    public static string AssetPath(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(workingRoot, Path.Combine(parts))).Replace('\\', '/');
    }

    public static string ToCssColor(this Color color)
    {
        return $"#{color.ToHex()}";
    }

    public static async Task<string?> GetOverlayColorAsync(string iconAbsolutePath)
    {
        using var icon = await Utils.TryReadImageRgba(iconAbsolutePath);
        if (icon is null)
        {
            return null;
        }

        return Utils.GetDominantColor(icon).ToCssColor();
    }

    public static async Task<string?> CacheRemoteImage(string url, string key)
    {
        return await Utils.CacheRemoteImage(cacheRoot, url, key);
    }

    public static async Task<string> LoadOrDownloadAvatar(API.OSU.Models.User userInfo)
    {
        var key = userInfo.AvatarUrl.Host == "a.ppy.sb" ? $"sb-{userInfo.Id}" : $"{userInfo.Id}";
        var cachedPath =
            await CacheRemoteImage(userInfo.AvatarUrl.ToString(), key)
            ?? throw new Exception($"下载用户头像失败: {userInfo.AvatarUrl}");
        return cachedPath;
    }

    public static async Task<string?> LoadOrDownloadBackground(long sid, long bid)
    {
        try
        {
            var url = $"https://assets.ppy.sh/beatmaps/{sid}/covers/fullsize.jpg";
            var bgPath = await CacheRemoteImage(url, $"bg-{sid}");
            if (bgPath is not null)
                return bgPath;

            url = await API.OSU.Client.SayoGetBeatmapBackgroundUrl(sid, bid);
            if (url is null)
                return null;

            bgPath = await CacheRemoteImage(url, $"bg-{sid}-{bid}");
            if (bgPath is null)
                return null;

            return bgPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"下载背景图失败");
            return null;
        }
    }
}
