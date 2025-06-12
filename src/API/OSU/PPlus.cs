using Newtonsoft.Json.Linq;
using System.Net;
using KanonBot.Serializer;
using System.IO;
using KanonBot.Database;

namespace KanonBot.API.OSU
{
    public partial class Client
    {
        public static class PPlus
        {
            private static readonly Config.Base config = Config.inner!;
            private static string Token = "";
            private static long TokenExpireTime = 0;
            private static readonly string pppEndPoint = "http://localhost:9001/";
            private static readonly object tokenLock = new object();

            static IFlurlRequest pplus()
            {
                var ep = config.osu?.pppEndPoint ?? pppEndPoint;
                return ep.AllowHttpStatus(404, 401);
            }

            // 检查token是否有效（基于时间）
            private static bool IsTokenValid()
            {
                return !string.IsNullOrEmpty(Token) && 
                       DateTimeOffset.UtcNow.ToUnixTimeSeconds() < TokenExpireTime;
            }

            // 确保有有效的token
            private static async Task<bool> EnsureValidToken()
            {
                lock (tokenLock)
                {
                    if (IsTokenValid())
                        return true;
                }

                return await RefreshToken();
            }

            // 刷新token
            private static async Task<bool> RefreshToken()
            {
                try
                {
                    var result = await pplus()
                        .AppendPathSegments("auth", "token")
                        .SetQueryParam("clientId", config.osu?.pppClientId)
                        .SetQueryParam("clientSecret", config.osu?.pppClientSecret)
                        .PostAsync();

                    var body = await result.GetJsonAsync<JObject>();
                    
                    if (body["data"] != null)
                    {
                        lock (tokenLock)
                        {
                            Token = body["data"]?.ToString() ?? "";
                            // 设置token过期时间（假设token有效期1小时，提前5分钟刷新）
                            TokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3300; // 55分钟
                        }
                        return true;
                    }
                    else
                    {
                        Log.Error("获取token失败, 返回Body: \n{0}", body.ToString());
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("刷新token时发生异常: {0}", ex.Message);
                    return false;
                }
            }

            // 执行带token的请求，自动处理401重试
            private static async Task<IFlurlResponse> ExecuteRequestWithToken(Func<IFlurlRequest> requestBuilder)
            {
                // 确保有有效token
                if (!await EnsureValidToken())
                {
                    throw new InvalidOperationException("无法获取有效的访问token");
                }

                var request = requestBuilder();
                if (!string.IsNullOrEmpty(Token))
                {
                    request = request.WithHeader("Authorization", $"Bearer {Token}");
                }

                var response = await request.GetAsync();

                // 如果收到401，尝试刷新token并重试一次
                if (response.StatusCode == 401)
                {
                    Log.Warning("收到401响应，尝试刷新token并重试");
                    
                    if (await RefreshToken())
                    {
                        var retryRequest = requestBuilder();
                        if (!string.IsNullOrEmpty(Token))
                        {
                            retryRequest = retryRequest.WithHeader("Authorization", $"Bearer {Token}");
                        }
                        response = await retryRequest.GetAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException("token刷新失败，无法完成请求");
                    }
                }

                return response;
            }

            // 执行带token的POST请求，自动处理401重试
            private static async Task<IFlurlResponse> ExecutePostRequestWithToken(Func<IFlurlRequest> requestBuilder)
            {
                // 确保有有效token
                if (!await EnsureValidToken())
                {
                    throw new InvalidOperationException("无法获取有效的访问token");
                }

                var request = requestBuilder();
                if (!string.IsNullOrEmpty(Token))
                {
                    request = request.WithHeader("Authorization", $"Bearer {Token}");
                }

                var response = await request.PostAsync();

                // 如果收到401，尝试刷新token并重试一次
                if (response.StatusCode == 401)
                {
                    Log.Warning("收到401响应，尝试刷新token并重试");
                    
                    if (await RefreshToken())
                    {
                        var retryRequest = requestBuilder();
                        if (!string.IsNullOrEmpty(Token))
                        {
                            retryRequest = retryRequest.WithHeader("Authorization", $"Bearer {Token}");
                        }
                        response = await retryRequest.PostAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException("token刷新失败，无法完成请求");
                    }
                }

                return response;
            }

            public static async Task<Models.PPlusData.UserDataNext?> GetUserPlusDataNext(long uid)
            {
                try
                {
                    var response = await ExecuteRequestWithToken(() => 
                        pplus()
                            .AppendPathSegments("player", "info")
                            .SetQueryParam("id", uid)
                    );

                    if (response.StatusCode == 404)
                    {
                        return null;
                    }

                    var s = await response.GetJsonAsync<JObject>();
                    var data = s["data"]?.ToObject<Models.PPlusData.UserDataNext>();
                    return data;
                }
                catch (Exception ex)
                {
                    Log.Error("获取用户数据失败: {}", ex.Message);
                    return null;
                }
            }

            public static async Task<Models.PPlusData.UserDataNext?> UpdateUserPlusDataNext(long uid)
            {
                try
                {
                    var response = await ExecutePostRequestWithToken(() => 
                        pplus()
                            .AppendPathSegments("player", "update")
                            .SetQueryParam("id", uid)
                    );

                    if (response.StatusCode == 404)
                    {
                        return null;
                    }

                    var s = await response.GetJsonAsync<JObject>();
                    var data = s["data"]?.ToObject<Models.PPlusData.UserDataNext>();
                    return data;
                }
                catch (Exception ex)
                {
                    Log.Error("更新用户数据失败: {}", ex.Message);
                    return null;
                }
            }

            // 保留原有的GetToken方法作为RefreshToken的别名，保持向后兼容
            [Obsolete("请使用RefreshToken方法")]
            private static async Task<bool> GetToken()
            {
                return await RefreshToken();
            }

            // 手动刷新token的公共方法
            public static async Task<bool> ForceRefreshToken()
            {
                return await RefreshToken();
            }

            // 清除token（用于登出或重置）
            public static void ClearToken()
            {
                lock (tokenLock)
                {
                    Token = "";
                    TokenExpireTime = 0;
                }
            }
        }
    }
}