using System.IO;
using System.Net;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using Flurl;
using Flurl.Http;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Message;
using RosuPP;

namespace KanonBot.Functions.OSUBot
{
    public class LeewayCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "leeway",
                Description = "Calculate pp leeway for an osu! score",
                Aliases =  [ "lc" ],
                Args =
                [
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "osu_mods", Description = "osu! mods", Prefix = ArgPrefix.Plus },
                ],
                Flags =  [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            API.OSU.Models.User? OnlineOsuInfo;

            // 解析模式
            var osuMode = cmd.GetString("osu_mode")?.ParseMode() ?? API.OSU.Mode.OSU;
            if (osuMode is not API.OSU.Mode.OSU)
            {
                await target.Treply("osu.leeway_std_only");
                return;
            }

            // 验证账户 (via IAM)
            var AccInfo = Accounts.GetAccInfo(target);
            string provider;
            try
            {
                provider = API.IAM.Client.PlatformToProvider(AccInfo.platform);
            }
            catch (NotSupportedException)
            {
                await target.Treply("account.platform_unsupported");
                return;
            }

            var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, AccInfo.uid);
            if (iamUserId == null)
            {
                await target.Treply("account.not_bound");
                return;
            }

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
            {
                await target.Treply("account.fetch_failed");
                return;
            }

            var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
            if (!osuUid.HasValue)
            {
                await target.Treply("account.osu_not_bound");
                return;
            }

            OnlineOsuInfo = await API.OSU.Client.GetUser(osuUid.Value);
            if (OnlineOsuInfo == null)
            {
                await target.Treply("account.banned");
                return;
            }

            long bid;
            var parsedBid = cmd.Get<int>("bid");
            if (parsedBid == 0) // 检查玩家是否指定bid
            {
                var scoreInfos = await API.OSU
                    .Client
                    .GetUserScores(
                        OnlineOsuInfo.Id,
                        API.OSU.UserScoreType.Recent,
                        osuMode,
                        1,
                        0,
                        true
                    );
                if (scoreInfos == null)
                {
                    await target.Treply("osu.scores_error");
                    return;
                }
                ; // 正常是找不到玩家，但是上面有验证，这里做保险
                if (scoreInfos!.Length > 0)
                {
                    bid = scoreInfos[0].Beatmap!.BeatmapId;
                }
                else
                {
                    await target.Treply("osu.leeway_recent_not_found");
                    return;
                }
            }
            else
            {
                bid = parsedBid;
            }

            // 尝试寻找玩家在该谱面的最高成绩
            long score = 0;
            var empty_mods = System.Array.Empty<string>(); // 要的是最高分，直接给传一个空集合得了
            var scoreData = await API.OSU
                .Client
                .GetUserBeatmapScore(OnlineOsuInfo.Id, bid, empty_mods, osuMode);
            if (scoreData != null)
            {
                score = scoreData.Score.ScoreAuto;
                if (scoreData.Score.Mode is not API.OSU.Mode.OSU)
                {
                    await target.Treply("osu.leeway_std_only");
                    return;
                } // 检查谱面是否是std
            }

            // LeewayCalculator
            string beatmap;

            try
            {
                // 下载谱面
                await API.OSU.Client.DownloadBeatmapFile(bid);
                beatmap = File.ReadAllText($"./work/beatmap/{bid}.osu");
            }
            catch (Exception)
            {
                // 加载失败
                File.Delete($"./work/beatmap/{bid}.osu");
                await target.Treply("osu.beatmap_load_failed");
                return;
            }

            LeewayCalculator lc = new(); // 实例化

            string[] mods = lc.GetMods((cmd.GetString("osu_mods") ?? "").ToUpper()); // 获取mods
            int maxScore = lc.CalculateMaxScore(beatmap, mods); // 计算理论值
            string modsString = lc.GetModsString(mods); // 获取模式字符串

            string str = "";
            str += string.Concat(
                new string[]
                {
                    bid.ToString(),
                    " ",
                    lc.GetArtist(beatmap),
                    " - ",
                    lc.GetTitle(beatmap),
                    " (",
                    lc.GetDifficultyName(beatmap),
                    ")"
                }
            );

            if (scoreData != null)
            {
                string scoreModsString = lc.GetModsString(
                    scoreData.Score.Mods.Map(m => m.Acronym).ToArray()
                );
                int scoreAdvantage = (int)score - maxScore;
                str += string.Format(
                    "\n你的成绩：{0:n0} ({1}) [{2}]",
                    (int)score,
                    scoreModsString != "" ? $"+{scoreModsString}" : "None",
                    scoreAdvantage < 0 ? scoreAdvantage : $"+{scoreAdvantage}"
                );
            }
            else
            {
                str += "\n你的成绩：从未玩过";
            }

            str += string.Format("\n理论值：{0:n0} (+{1})", maxScore, modsString);

            List<int[]> spinners = lc.GetSpinners(beatmap); // 获取转盘
            double adjustTime = lc.GetAdjustTime(mods); // 获取 adjustTime(?) | idk what the fuck is adjustTime lol
            double od = lc.GetOD(beatmap); // 获取 OD
            int difficultyModifier = lc.GetDifficultyModifier(mods); // 计算分数增益

            if (spinners.Count > 0)
            {
                for (int i = 0; i < spinners.Count; i++)
                {
                    int length = spinners[i][1];
                    int combo = spinners[i][0];
                    double rotations = lc.CalcRotations(length, adjustTime);
                    int rotReq = lc.CalcRotReq(length, (double)od, difficultyModifier);
                    string amount = lc.CalcAmount((int)rotations, rotReq);
                    double leeway = lc.CalcLeeway(
                        length,
                        adjustTime,
                        (double)od,
                        difficultyModifier
                    );
                    string spinner = string.Format(
                        "\n#{0} | 长度：{1} | Combo：{2} | 分数：{3} | 圈数：{4} | Leeway：{5}",
                        i + 1,
                        length,
                        combo,
                        amount,
                        string.Format("{0:0.00000}", rotations),
                        string.Format("{0:0.00000}", leeway)
                    );
                    str += spinner;
                }
            }
            else
            {
                str += "\n该难度没有转盘";
            }
            await target.reply(new Chain().msg(str));
        }
    }
}
