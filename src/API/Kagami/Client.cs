namespace KanonBot.API.Kagami;

public static class Client
{
    private static readonly Config.Base config = Config.inner!;

    private static string BaseUrl => config.kagami?.baseUrl ?? "https://hub.kagamistudio.com";

    private static IFlurlRequest Http() =>
        BaseUrl
            .WithHeader("X-Api-Key", config.kagami?.apiKey ?? "")
            .AllowHttpStatus("400,401,404");

    /// <summary>
    /// Convert a Kagami asset URL to absolute form.
    /// API may return relative paths like /uploads/xxx.webp.
    /// </summary>
    public static string? NormalizeAssetUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;

        var baseUri = new Uri(BaseUrl.EndsWith("/") ? BaseUrl : BaseUrl + "/");
        return new Uri(baseUri, url.TrimStart('/')).ToString();
    }

    /// <summary>
    /// Get KanonBot profile including worn badges and panel settings.
    /// GET /api/integrations/kanonbot/users/{userId}/profile
    /// </summary>
    public static async Task<KanonBotProfile?> GetPublicKanonBotProfile(string userId)
    {
        return await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "profile")
            .TryGetJsonAsync<KanonBotProfile>();
    }

    /// <summary>
    /// Get all kanon image slot URLs for a user.
    /// GET /api/integrations/kanonbot/users/{userId}/images
    /// </summary>
    public static async Task<KanonImages?> GetKanonImages(string userId)
    {
        return await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "images")
            .TryGetJsonAsync<KanonImages>();
    }

    /// <summary>
    /// Get all user's badges.
    /// GET /api/integrations/kanonbot/users/{userId}/badges
    /// </summary>
    public static async Task<List<UserBadgeResponse>?> GetUserBadges(string userId)
    {
        return await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "badges")
            .TryGetJsonAsync<List<UserBadgeResponse>>();
    }

    /// <summary>
    /// Get user's worn badges.
    /// GET /api/integrations/kanonbot/users/{userId}/badges/wears
    /// </summary>
    public static async Task<List<UserBadgeResponse>?> GetUserWearBadges(string userId)
    {
        return await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "badges", "wears")
            .TryGetJsonAsync<List<UserBadgeResponse>>();
    }

    /// <summary>
    /// Get a user's role/permission snapshot for KanonBot integration.
    /// GET /api/integrations/kanonbot/users/{userId}/permissions
    /// </summary>
    public static async Task<UserPermissionsResponse?> GetUserPermissions(string userId)
    {
        return await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "permissions")
            .TryGetJsonAsync<UserPermissionsResponse>();
    }

    /// <summary>
    /// Set a user's osu! preferred game mode via bot integration.
    /// PUT /api/integrations/kanonbot/users/{userId}/game-mode
    /// </summary>
    public static async Task<bool> SetGameMode(string userId, string gameMode)
    {
        var status = await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "game-mode")
            .TryPutJsonGetStatusAsync(new { gameMode });

        return status == 200;
    }

    /// <summary>
    /// Set a user's ppy.sb preferred game mode via bot integration.
    /// PUT /api/integrations/kanonbot/users/{userId}/ppysb-game-mode
    /// </summary>
    public static async Task<bool> SetPpySbGameMode(string userId, string gameMode)
    {
        var status = await Http()
            .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "ppysb-game-mode")
            .TryPutJsonGetStatusAsync(new { gameMode });

        return status == 200;
    }
}
