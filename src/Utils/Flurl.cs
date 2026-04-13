namespace KanonBot;

public static partial class Utils
{
    public static async Task<T?> TryGetJsonAsync<T>(
        this IFlurlRequest request,
        int successStatusCode = 200
    )
        where T : class
    {
        try
        {
            var resp = await request.GetAsync();
            if (resp.StatusCode != successStatusCode) return null;
            return await resp.GetJsonAsync<T>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HTTP GET failed: {Url}", request.Url);
            return null;
        }
    }

    public static async Task<int?> TryPostJsonGetStatusAsync(this IFlurlRequest request, object payload)
    {
        try
        {
            var resp = await request.PostJsonAsync(payload);
            return resp.StatusCode;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HTTP POST failed: {Url}", request.Url);
            return null;
        }
    }

    public static async Task<(int? StatusCode, T? Data)> TryPostJsonAsync<T>(
        this IFlurlRequest request,
        object payload,
        int successStatusCode = 200
    )
        where T : class
    {
        try
        {
            var resp = await request.PostJsonAsync(payload);
            if (resp.StatusCode != successStatusCode)
                return (resp.StatusCode, null);

            var data = await resp.GetJsonAsync<T>();
            return (resp.StatusCode, data);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HTTP POST failed: {Url}", request.Url);
            return (null, null);
        }
    }

    public static async Task<int?> TryPutJsonGetStatusAsync(this IFlurlRequest request, object payload)
    {
        try
        {
            var resp = await request.PutJsonAsync(payload);
            return resp.StatusCode;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HTTP PUT failed: {Url}", request.Url);
            return null;
        }
    }
}