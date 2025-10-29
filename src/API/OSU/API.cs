// Flurl.Http.FlurlHttpTimeoutException
using Newtonsoft.Json.Linq;
using System.Net;
using KanonBot.Serializer;
using System.IO;
using KanonBot.Database;

namespace KanonBot.API.OSU
{
    // 成绩类型，用作API查询
    // 共可以是 best, firsts, recent
    // 默认为best（bp查询）
    public enum UserScoreType
    {
        Best,
        Firsts,
        Recent,
    }

    public partial class Client
    {
        private static readonly Config.Base config = Config.inner!;
        private static string Token = "";
        private static long TokenExpireTime = 0;
        private static readonly string EndPointV1 = "https://osu.ppy.sh/api/";
        private static readonly string EndPointV2 = "https://osu.ppy.sh/api/v2/";
        static IFlurlRequest http()
        {
            CheckToken().Wait();
            var ep = config.osu?.v2EndPoint ?? EndPointV2;
            return ep.WithHeader("Authorization", $"Bearer {Token}").AllowHttpStatus("404");
        }
        static IFlurlRequest httpV1()
        {
            var ep = EndPointV1;
            return ep.SetQueryParam("k", config.osu!.v1key).AllowHttpStatus("404");
        }

        static IFlurlRequest withLazerScore(IFlurlRequest req) {
            return req.WithHeader("x-api-version", "20220705");
        }

        async private static Task<bool> GetToken()
        {
            JObject j = new()
            {
                { "grant_type", "client_credentials" },
                { "client_id", config.osu?.clientId },
                { "client_secret", config.osu?.clientSecret },
                { "scope", "public" },
                { "code", "kanon" },
            };

            var result = await "https://osu.ppy.sh/oauth/token".PostJsonAsync(j);
            var body = await result.GetJsonAsync<JObject>();
            try
            {
                Token = ((string?)body["access_token"]) ?? "";
                TokenExpireTime = DateTimeOffset.Now.ToUnixTimeSeconds() + long.Parse(((string?)body["expires_in"]) ?? "0");
                return true;
            }
            catch
            {
                Log.Error("获取token失败, 返回Body: \n({})", body.ToString());
                return false;
            }
        }

        async public static Task CheckToken()
        {
            if (TokenExpireTime == 0)
            {
                Log.Information("正在获取OSUApiV2_Token");
                if (await GetToken())
                {
                    Log.Information(string.Concat("获取成功, Token: ", Token.AsSpan(Utils.TryGetConsoleWidth() - 38), "..."));
                    Log.Information($"Token过期时间: {DateTimeOffset.FromUnixTimeSeconds(TokenExpireTime).DateTime.ToLocalTime()}");
                }
            }
            else if (TokenExpireTime <= DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                Log.Information("OSUApiV2_Token已过期, 正在重新获取");
                if (await GetToken())
                {
                    Log.Information(string.Concat("获取成功, Token: ", Token.AsSpan(Utils.TryGetConsoleWidth() - 38), "..."));
                    Log.Information($"Token过期时间: {DateTimeOffset.FromUnixTimeSeconds(TokenExpireTime).DateTime.ToLocalTime()}");
                }
            }
        }


        // 获取特定谱面信息
        async public static Task<Models.BeatmapSearchResult?> SearchBeatmap(string filters, Mode? mode = null, bool has_leaderboard = true)
        {
            var q = http()
                .AppendPathSegments(["beatmapsets", "search"])
                .SetQueryParams(new
                {
                    q = filters,
                    nsfw = 1,
                    // limit = 1,
                    // offset = 0,
                    // sort = "ranked",
                    // m = 0,
                    // s = "favourite",
                    // a = "false"
                });

            if (mode != null)
                q = q.SetQueryParam("m", mode.Value.ToNum());
            
            if (!has_leaderboard)
                q = q.SetQueryParam("s", "any");
                

            var res = await q.GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.BeatmapSearchResult>();
        }

        // 获取特定谱面信息
        async public static Task<Models.Beatmap?> GetBeatmap(long bid)
        {
            var res = await http()
                .AppendPathSegments(["beatmaps", bid])
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.Beatmap>();
        }

        // 获取特定谱面信息
        async public static Task<Models.Beatmap[]?> GetBeatmaps(IEnumerable<long> bids)
        {
            IEnumerable<Models.Beatmap> beatmaps = [];

            foreach (var b in bids.Chunk(50)) {
                var res = await http()
                    .AppendPathSegment("beatmaps")
                    .SetQueryParam("ids[]", b)
                    .GetAsync();

                if (res.StatusCode == 404)
                    return null;

                var bms =  await res.GetJsonAsync<Models.BeatmapList>();
                beatmaps = beatmaps.Append(bms.Beatmaps);
            }
            return beatmaps.ToArray();
        }


        // 获取用户成绩
        // Score type. Must be one of these: best, firsts, recent.
        // 默认 best
        async public static Task<Models.ScoreV1[]?> GetUserBestsV1(long userId, Mode mode = Mode.OSU, int limit = 1, int offset = 0)
        {
            var res = await httpV1()
                .AppendPathSegment("get_user_best")
                .SetQueryParams(new
                {
                    u = userId,
                    limit = 100,
                    mode = mode.ToNum(),
                    type = "id"
                })
                .GetAsync();

            if (res.StatusCode == 404) {
                return null;
            } else {
                var j = await res.GetJsonAsync<Models.ScoreV1[]>();
                return j?.Skip(offset).Take(limit).ToArray();
            }
        }

        async public static Task<Models.ScoreLazer[]?> GetUserScoresPage(long userId, UserScoreType scoreType = UserScoreType.Best, Mode mode = Mode.OSU, int limit = 1, int offset = 0, bool includeFails = true, bool LegacyOnly = false)
        {
            const int MAX_LIMIT = 100;  // API单次请求最大限制
            List<Models.ScoreLazer> allScores = [];
            int remaining = limit;
            int currentOffset = offset;

            while (remaining > 0)
            {
                int currentLimit = Math.Min(remaining, MAX_LIMIT);
                
                var scores = await GetUserScores(userId, scoreType, mode, currentLimit, currentOffset, includeFails, LegacyOnly);
                if (scores == null || scores.Length == 0)
                    break;

                allScores.AddRange(scores);
                remaining -= scores.Length;
                currentOffset += scores.Length;

                // 如果返回数量不足请求量，说明没有更多数据
                if (scores.Length < currentLimit)
                    break;
            }

            return [.. allScores];
        }

        // 获取用户成绩
        // Score type. Must be one of these: best, firsts, recent.
        // 默认 best
        async public static Task<Models.ScoreLazer[]?> GetUserScores(long userId, UserScoreType scoreType = UserScoreType.Best, Mode mode = Mode.OSU, int limit = 1, int offset = 0, bool includeFails = true, bool LegacyOnly = false)
        {
            var res = await withLazerScore(http())
                .AppendPathSegments(["users", userId, "scores", scoreType.ToStr()])
                .SetQueryParams(new
                {
                    include_fails = includeFails ? 1 : 0,
                    limit,
                    offset,
                    mode = mode.ToStr(),
                    legacy_only = LegacyOnly ? 1 : 0
                })
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.ScoreLazer[]>();
        }

        // 获取用户成绩
        // Score type. Must be one of these: best, firsts, recent.
        // 默认 best
        async public static Task<Models.Score[]?> GetUserScoresLeagcy(long userId, UserScoreType scoreType = UserScoreType.Best, Mode mode = Mode.OSU, int limit = 1, int offset = 0, bool includeFails = true)
        {
            var res = await http()
                .AppendPathSegments(["users", userId, "scores", scoreType.ToStr()])
                .SetQueryParams(new
                {
                    include_fails = includeFails ? 1 : 0,
                    limit,
                    offset,
                    mode = mode.ToStr(),
                    legacy_only = 1
                })
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.Score[]>();
        }

        // 获取用户在特定谱面上的成绩
        async public static Task<Models.BeatmapScoreLazer?> GetUserBeatmapScore(long UserId, long bid, IEnumerable<string> mods, Mode mode = Mode.OSU, bool LegacyOnly = false)
        {
            var res = await withLazerScore(http())
                .AppendPathSegments(["beatmaps", bid, "scores", "users", UserId])
                .SetQueryParam("mode", mode.ToStr())
                .SetQueryParam("legacy_only", LegacyOnly ? 1 : 0)
                .SetQueryParam("mods[]", mods)
                .GetAsync();

            if (res.StatusCode == 404) {
                return null;
            } else {
                return await res.GetJsonAsync<Models.BeatmapScoreLazer>();
            }
        }

        // 获取用户在特定谱面上的成绩
        async public static Task<Models.BeatmapScore?> GetUserBeatmapScoreLeagcy(long UserId, long bid, string[] mods, Mode mode = Mode.OSU)
        {
            var req = http()
                .AppendPathSegments(new object[] { "beatmaps", bid, "scores", "users", UserId })
                .SetQueryParam("mode", mode.ToStr())
                .SetQueryParam("legacy_only", 1);


            req.SetQueryParam("mods[]", mods);
            var res = await req.GetAsync();
            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.BeatmapScore>();
        }

        // 获取用户在特定谱面上的成绩
        // 返回null代表找不到beatmap / beatmap无排行榜
        // 返回[]则用户无在此谱面的成绩
        async public static Task<Models.ScoreLazer[]?> GetUserBeatmapScores(long UserId, long bid, Mode mode = Mode.OSU)
        {
            var res = await withLazerScore(http())
                .AppendPathSegments(["beatmaps", bid, "scores", "users", UserId, "all"])
                .SetQueryParam("mode", mode.ToStr())
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
                return (await res.GetJsonAsync<JObject>())["scores"]!.ToObject<Models.ScoreLazer[]>();
        }

        // 通过osuv1 api osu uid获取用户信息
        async public static Task<Models.UserV1[]?> GetUserWithV1API(long userId, Mode mode = Mode.OSU)
        {
            var res = await httpV1()
                .AppendPathSegment("get_user")
                .SetQueryParam("u", userId)
                .SetQueryParam("m", mode.ToNum())
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.UserV1[]>();
        }
        
        // 通过osu uid获取用户信息
        async public static Task<Models.UserExtended?> GetUser(long userId, Mode mode = Mode.OSU)
        {
            var res = await http()
                .AppendPathSegments(["users", userId, mode.ToStr()])
                .GetAsync();

            //Log.Information(await res.GetStringAsync());
            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.UserExtended>();
        }

        // 通过osu username获取用户信息
        async public static Task<Models.UserExtended?> GetUser(string userName, Mode mode = Mode.OSU)
        {
            var res = await http()
                .AppendPathSegments(["users", userName, mode.ToStr()])
                .SetQueryParam("key", "username")
                .GetAsync();

            //Log.Information(await res.GetStringAsync());
            if (res.StatusCode == 404)
                return null;
            else
                return await res.GetJsonAsync<Models.UserExtended>();
        }

        // 获取谱面参数
        async public static Task<Models.BeatmapAttributes?> GetBeatmapAttributes(long bid, string[] mods, Mode mode = Mode.OSU)
        {
            JObject j = new()
            {
                { "mods", new JArray(mods) },
                { "ruleset", mode.ToStr() },
            };

            var res = await http()
                .AppendPathSegments(["beatmaps", bid, "attributes"])
                .PostJsonAsync(j);

            if (res.StatusCode == 404)
            {
                return null;
            }
            else
            {
                var body = await res.GetJsonAsync<JObject>();
                var beatmap = body["attributes"]!.ToObject<Models.BeatmapAttributes>()!;
                beatmap.Mode = mode;
                return beatmap;
            }
        }

        async public static Task<string?> DownloadBeatmapBackgroundImg(long sid, string folderPath, string? fileName = null)
        {
            try
            {
                return await $"https://assets.ppy.sh/beatmaps/{sid}/covers/fullsize.jpg".DownloadFileAsync(folderPath, fileName);
            }
            catch (Exception ex)
            {
                var msg = $"从OSU API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                Log.Warning(msg);
                return null;
            }
        }

        // 小夜api版（备选方案）
        public static async Task<string?> SayoDownloadBeatmapBackgroundImg(long sid, long bid, string folderPath, string? fileName = null)
        {
            try
            {
                // 获取谱面信息
                var apiUrl = $"https://api.sayobot.cn/v2/beatmapinfo?K={sid}";
                var response = await apiUrl.GetJsonAsync<JObject>();

                if (response?.SelectToken("data.bid_data") is not JArray bidDataArray) return null;

                // 查找目标谱面并获取背景图片文件名
                foreach (var beatmap in bidDataArray)
                {
                    if (beatmap.SelectToken("bid")?.Value<long>() == bid)
                    {
                        var bgFileName = beatmap.SelectToken("bg")?.Value<string>();
                        if (string.IsNullOrEmpty(bgFileName)) return null;

                        // 下载背景图片
                        var downloadUrl = $"https://dl.sayobot.cn/beatmaps/files/{sid}/{bgFileName}";
                        var finalFileName = fileName ?? $"{bid}.png";
                        
                        return await downloadUrl.DownloadFileAsync(folderPath, finalFileName);
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                var msg = $"从Sayo API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                Log.Warning(msg);
                return null;
            }
        }

        // 搜索用户数量 未使用
        async public static Task<JObject?> SearchUser(string userName)
        {
            var body = await http()
                .AppendPathSegment("search")
                .SetQueryParams(new
                {
                    mode = "user",
                    query = userName
                })
                .GetJsonAsync<JObject>();
            return body["user"] as JObject;
        }
     
        // 获取pp+数据
        async public static Task<Models.PPlusData> GetUserPlusData(long uid)
        {
            var res = await $"https://syrin.me/pp+/api/user/{uid}/".GetJsonAsync<JObject>();
            var data = new Models.PPlusData()
            {
                User = res["user_data"]!.ToObject<Models.PPlusData.UserData>()!,
                Performances = res["user_performances"]!["total"]!.ToObject<Models.PPlusData.UserPerformances[]>()
            };
            return data;
        }

        async public static Task<Models.PPlusData> GetUserPlusData(string username)
        {
            var res = await $"https://syrin.me/pp+/api/user/{username}/".GetJsonAsync<JObject>();
            var data = new Models.PPlusData() {
                User = res["user_data"]!.ToObject<Models.PPlusData.UserData>()!,
                Performances = res["user_performances"]!["total"]!.ToObject<Models.PPlusData.UserPerformances[]>()
            };
            return data;
        }

        /// <summary>
        /// deprecated
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        async public static Task<Models.PPlusData?> TryGetUserPlusData(OSU.Models.User user)
        {
            try
            {
                return await GetUserPlusData(user.Id);
            }
            catch
            {
                try
                {
                    return await GetUserPlusData(user.Username);
                }
                catch
                {

                    return null;
                }
            }
        }

        async public static Task DownloadBeatmapFile(long bid)
        {
            var filePath = $"./work/beatmap/{bid}.osu";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            var result = await $"http://osu.ppy.sh/osu/{bid}".GetBytesAsync();
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fs.WriteAsync(result!);
            fs.Close();
        }
    }
}
