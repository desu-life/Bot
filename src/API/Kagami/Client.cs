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
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "profile")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<KanonBotProfile>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kagami GetPublicKanonBotProfile failed for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get all kanon image slot URLs for a user.
    /// GET /api/integrations/kanonbot/users/{userId}/images
    /// </summary>
    public static async Task<KanonImages?> GetKanonImages(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "images")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<KanonImages>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kagami GetKanonImages failed for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get all user's badges.
    /// GET /api/integrations/kanonbot/users/{userId}/badges
    /// </summary>
    public static async Task<List<UserBadgeResponse>?> GetUserBadges(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "badges")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<List<UserBadgeResponse>>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kagami GetUserBadges failed for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get user's worn badges.
    /// GET /api/integrations/kanonbot/users/{userId}/badges/wears
    /// </summary>
    public static async Task<List<UserBadgeResponse>?> GetUserWearBadges(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "badges", "wears")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<List<UserBadgeResponse>>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kagami GetUserWearBadges failed for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Set a user's osu! preferred game mode via bot integration.
    /// PUT /api/integrations/kanonbot/users/{userId}/game-mode
    /// </summary>
    public static async Task<bool> SetGameMode(string userId, string gameMode)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "game-mode")
                .PutJsonAsync(new { gameMode });

            return resp.StatusCode == 200;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kagami SetGameMode failed for {UserId} mode={Mode}", userId, gameMode);
            return false;
        }
    }

    /// <summary>
    /// Set a user's ppy.sb preferred game mode via bot integration.
    /// PUT /api/integrations/kanonbot/users/{userId}/ppysb-game-mode
    /// </summary>
    public static async Task<bool> SetPpySbGameMode(string userId, string gameMode)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "kanonbot", "users", userId, "ppysb-game-mode")
                .PutJsonAsync(new { gameMode });

            return resp.StatusCode == 200;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kagami SetPpySbGameMode failed for {UserId} mode={Mode}", userId, gameMode);
            return false;
        }
    }
}
