namespace KanonBot.API.Kagami;

public static class Client
{
    private static readonly Config.Base config = Config.inner!;

    private static string BaseUrl => config.kagami?.baseUrl ?? "https://hub.kagamistudio.com";

    private static IFlurlRequest Http() =>
        BaseUrl.AllowHttpStatus("400,404");

    private static IFlurlRequest HttpWithApiKey() =>
        BaseUrl
            .WithHeader("X-Api-Key", config.kagami?.apiKey ?? "")
            .AllowHttpStatus("400,401,404");

    /// <summary>
    /// Get public KanonBot profile including worn badges and panel image URLs.
    /// GET /api/users/{userId}/public-kanonbot
    /// </summary>
    public static async Task<KanonBotProfile?> GetPublicKanonBotProfile(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "users", userId, "public-kanonbot")
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
    /// Get all 4 kanon image slot URLs for a user.
    /// GET /api/users/{userId}/kanon-images
    /// </summary>
    public static async Task<KanonImages?> GetKanonImages(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "users", userId, "kanon-images")
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
    /// Get user's worn badges via badge service.
    /// GET /api/badges/users/{userId}/wears
    /// </summary>
    public static async Task<List<UserBadgeResponse>?> GetUserWearBadges(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "badges", "users", userId, "wears")
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
    /// Set a user's ppy.sb preferred game mode via bot integration.
    /// PUT /api/integrations/kanonbot/users/{userId}/ppysb-game-mode
    /// </summary>
    public static async Task<bool> SetPpySbGameMode(string userId, string gameMode)
    {
        try
        {
            var resp = await HttpWithApiKey()
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
