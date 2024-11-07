// Flurl.Http.FlurlHttpTimeoutException
using System.IO;
using System.Net;
using KanonBot.Database;
using KanonBot.Serializer;
using Newtonsoft.Json.Linq;

namespace KanonBot.API.PPYSB
{
    // 成绩类型，用作API查询
    // 共可以是 best, firsts, recent
    // 默认为best（bp查询）
    public enum UserScoreType
    {
        Best,
        Recent,
    }

    public static class Client
    {
        private static readonly Config.Base config = Config.inner!;
        private static readonly string EndPointV1 = "https://api.ppy.sb/v1/";
        private static readonly string EndPointV2 = "https://api.ppy.sh/v2/";

        static IFlurlRequest http()
        {
            var ep = EndPointV2;
            return ep.AllowHttpStatus("404");
        }

        static IFlurlRequest httpV1()
        {
            var ep = EndPointV1;
            return ep.AllowHttpStatus("404");
        }

        public static async Task<Models.User?> GetUser(string userName)
        {
            var res = await httpV1()
                .AppendPathSegment("get_player_info")
                .SetQueryParam("scope", "all")
                .SetQueryParam("name", userName.Replace(" ", "_"))
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
            {
                var u = await res.GetJsonAsync<Models.UserResponse>();
                return u?.Player;
            }
        }

        public static async Task<Models.User?> GetUser(long uid)
        {
            var res = await httpV1()
                .AppendPathSegment("get_player_info")
                .SetQueryParam("scope", "all")
                .SetQueryParam("id", uid)
                .GetAsync();

            if (res.StatusCode == 404)
                return null;
            else
            {
                var u = await res.GetJsonAsync<Models.UserResponse>();
                return u?.Player;
            }
        }

        async public static Task<Models.Score[]?> GetUserScores(long userId, UserScoreType scoreType = UserScoreType.Best, Mode mode = Mode.OSU, int limit = 1, int offset = 0, bool includeFails = true, bool includeLoved = false)
        {
            if (mode.IsSupported() == false) return null;
            var req = httpV1()
                .AppendPathSegment("get_player_scores")
                .SetQueryParam("scope", scoreType.ToStr())
                .SetQueryParam("id", userId)
                .SetQueryParam("mode", mode.ToNum());

            if (offset == 0) {
                req.SetQueryParam("limit", limit);
            } else {
                req.SetQueryParam("limit", 100);
            }

            var res = await req.GetAsync();
            if (res.StatusCode == 404)
                return null;
            else
            {
                var u = await res.GetJsonAsync<Models.ScoreResponse>();
                return u?.Scores.Skip(offset).Take(limit).ToArray();
            }
        }

    }
}
