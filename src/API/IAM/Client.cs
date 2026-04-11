using KanonBot.Drivers;

namespace KanonBot.API.IAM;

public enum VerifyResult
{
    Success,
    InvalidCode,
    InvalidApiKey,
    Misconfigured,
    Error
}

public static class Client
{
    private static readonly Config.Base config = Config.inner!;

    private static string BaseUrl => config.iam?.baseUrl ?? "https://iam.neonprizma.com";

    public static string PlatformToProvider(Platform platform) => platform switch
    {
        Platform.OneBot => "qq",
        Platform.Guild => "qq-guild",
        Platform.Discord => "discord",
        _ => throw new NotSupportedException($"Platform {platform} is not supported for IAM integration")
    };

    private static IFlurlRequest Http() =>
        BaseUrl.WithHeader("X-Api-Key", config.iam?.apiKey ?? "").AllowHttpStatus("400,401,404,500");

    /// <summary>
    /// Reverse lookup: get IAM user UUID from platform external ID.
    /// GET /api/integrations/bot/users/by-{provider}/{externalId}
    /// </summary>
    public static async Task<string?> GetIamUserIdByExternalId(string provider, string externalId)
    {
        // Map provider to URL segment
        var segment = provider switch
        {
            "qq" => "by-qq",
            "qq-guild" => "by-qq-guild",
            "discord" => "by-discord",
            _ => throw new NotSupportedException($"Unknown provider: {provider}")
        };

        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "users", segment, externalId)
                .GetAsync();

            if (resp.StatusCode == 200)
            {
                var result = await resp.GetJsonAsync<BoundUserLookupResponse>();
                return result?.UserId;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetIamUserIdByExternalId failed for {Provider}/{ExternalId}", provider, externalId);
            return null;
        }
    }

    /// <summary>
    /// Submit verification code from bot user.
    /// POST /api/integrations/bot/verify
    /// </summary>
    public static async Task<VerifyResult> SubmitVerification(string provider, string code, string externalId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "verify")
                .PostJsonAsync(new SubmitVerificationRequest
                {
                    Code = code,
                    ExternalId = externalId
                });

            return resp.StatusCode switch
            {
                200 => VerifyResult.Success,
                404 => VerifyResult.InvalidCode,
                401 => VerifyResult.InvalidApiKey,
                500 => VerifyResult.Misconfigured,
                _ => VerifyResult.Error
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM SubmitVerification failed for {Provider}/{ExternalId}", provider, externalId);
            return VerifyResult.Error;
        }
    }

    /// <summary>
    /// Get all bound osu UIDs.
    /// GET /api/integrations/bot/users/osu-bindings
    /// </summary>
    public static async Task<OsuBindingsResponse?> GetOsuBindings()
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "users", "osu-bindings")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<OsuBindingsResponse>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetOsuBindings failed");
            return null;
        }
    }

    /// <summary>
    /// Get all bound ppy.sb UIDs.
    /// GET /api/integrations/bot/users/ppy-sb-bindings
    /// </summary>
    public static async Task<PpySbBindingsResponse?> GetPpySbBindings()
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "users", "ppy-sb-bindings")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<PpySbBindingsResponse>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetPpySbBindings failed");
            return null;
        }
    }

    /// <summary>
    /// Get user bindings overview including osu/ppysb/qq/discord bindings.
    /// GET /api/integrations/bot/users/{userId}/bindings
    /// </summary>
    public static async Task<UserBindingsResponse?> GetUserBindings(string userId)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "users", userId, "bindings")
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<UserBindingsResponse>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetUserBindings failed for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Extract osu UID from user bindings.
    /// </summary>
    public static long? ExtractOsuUid(UserBindingsResponse bindings)
    {
        if (bindings.Bindings.Osu != null && long.TryParse(bindings.Bindings.Osu, out var osuUid))
            return osuUid;
        return null;
    }

    /// <summary>
    /// Extract ppy.sb UID from user bindings.
    /// </summary>
    public static long? ExtractPpysbUid(UserBindingsResponse bindings)
    {
        if (bindings.Bindings.PpySb != null && long.TryParse(bindings.Bindings.PpySb, out var ppysbUid))
            return ppysbUid;
        return null;
    }

    /// <summary>
    /// Reverse lookup: get IAM user ID from osu! UID.
    /// GET /api/integrations/bot/users/by-osu/{osuUserId}
    /// </summary>
    public static async Task<string?> GetIamUserIdByOsuUid(long osuUid)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "users", "by-osu", osuUid.ToString())
                .GetAsync();

            if (resp.StatusCode == 200)
            {
                var result = await resp.GetJsonAsync<BoundUserLookupResponse>();
                return result?.UserId;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetIamUserIdByOsuUid failed for {OsuUid}", osuUid);
            return null;
        }
    }

    /// <summary>
    /// Reverse lookup: get IAM user IDs from ppy.sb UID.
    /// GET /api/integrations/bot/users/by-ppy-sb/{ppySbUserId}
    /// Returns a list because ppy.sb allows multiple users to bind the same UID.
    /// </summary>
    public static async Task<List<string>?> GetIamUserIdsByPpysbUid(long ppysbUid)
    {
        try
        {
            var resp = await Http()
                .AppendPathSegments("api", "integrations", "bot", "users", "by-ppy-sb", ppysbUid.ToString())
                .GetAsync();

            if (resp.StatusCode == 200)
            {
                var result = await resp.GetJsonAsync<PpysbBoundUsersLookupResponse>();
                return result?.UserIds;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetIamUserIdsByPpysbUid failed for {PpysbUid}", ppysbUid);
            return null;
        }
    }
}
