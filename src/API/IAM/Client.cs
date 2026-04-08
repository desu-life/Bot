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

    private static string GetApiKey(string provider) => provider switch
    {
        "qq" => config.iam?.qqApiKey ?? "",
        "qq-guild" => config.iam?.qqGuildApiKey ?? "",
        "discord" => config.iam?.discordApiKey ?? "",
        _ => throw new NotSupportedException($"Unknown IAM provider: {provider}")
    };

    public static string PlatformToProvider(Platform platform) => platform switch
    {
        Platform.OneBot => "qq",
        Platform.Guild => "qq-guild",
        Platform.Discord => "discord",
        _ => throw new NotSupportedException($"Platform {platform} is not supported for IAM integration")
    };

    private static IFlurlRequest Http(string provider) =>
        BaseUrl.WithHeader("X-Api-Key", GetApiKey(provider)).AllowHttpStatus("400,401,404,500");

    private static IFlurlRequest HttpAnon() =>
        BaseUrl.AllowHttpStatus("400,404");

    /// <summary>
    /// Reverse lookup: get IAM user UUID from platform external ID.
    /// GET /api/integrations/{provider}/users/by-external/{externalId}
    /// </summary>
    public static async Task<string?> GetIamUserIdByExternalId(string provider, string externalId)
    {
        try
        {
            var resp = await Http(provider)
                .AppendPathSegments("api", "integrations", provider, "users", "by-external", externalId)
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
    /// POST /api/integrations/{provider}/verify
    /// </summary>
    public static async Task<VerifyResult> SubmitVerification(string provider, string code, string externalId)
    {
        try
        {
            var resp = await Http(provider)
                .AppendPathSegments("api", "integrations", provider, "verify")
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
    /// Get public profile of an IAM user (includes externalUids with osu binding).
    /// GET /api/users/{userId}
    /// </summary>
    public static async Task<IamUserProfile?> GetIamUserPublicProfile(string userId)
    {
        try
        {
            var resp = await HttpAnon()
                .AppendPathSegments("api", "users", userId)
                .GetAsync();

            if (resp.StatusCode == 200)
                return await resp.GetJsonAsync<IamUserProfile>();

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IAM GetIamUserPublicProfile failed for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Extract osu UID from IAM user profile's externalUids.
    /// </summary>
    public static long? ExtractOsuUid(IamUserProfile profile)
    {
        var osuLogin = profile.ExternalUids.FirstOrDefault(e =>
            string.Equals(e.Provider, "osu", StringComparison.OrdinalIgnoreCase));
        if (osuLogin != null && long.TryParse(osuLogin.Uid, out var osuUid))
            return osuUid;
        return null;
    }
}
