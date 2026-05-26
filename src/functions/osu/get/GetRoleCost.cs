using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;
using static KanonBot.API.OSU.Models;
using static KanonBot.API.OSU.Models.PPlusData;

namespace KanonBot.Functions
{

    public class GetRoleCostCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get rolecost",
                Description = "Calculate role cost for a match",
                Args =
                [
                    new() { Name = "match_name", Description = "Match name", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.Hash },
                    new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                ],
                Flags = [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var matchName = (cmd.GetString("match_name") ?? "").ToLower().Trim();

            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID, mode!.Value);
            if (OnlineOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;

            switch (matchName)
            {
                case "occ":
                    try
                    {
                        var pppData = await API.OSU
                            .Client
                            .PPlus
                            .GetUserPlusDataNext(OnlineOsuInfo.Id);
                        await target.Treply(
                            "get.occ_cost_result",
                            OnlineOsuInfo.Username,
                            occost(OnlineOsuInfo, pppData!.Performances)
                        );
                    }
                    catch
                    {
                        await target.Treply("osu.pp_plus_failed");
                        return;
                    }
                    break;
                case "onc":
                    var onc = oncost(OnlineOsuInfo);
                    if (onc == -1)
                        await target.Treply("osu.not_in_range", OnlineOsuInfo.Username);
                    else
                        await target.Treply("osu.cost_result", OnlineOsuInfo.Username, onc);
                    break;
                case "zkfc":
                    var orderNumber = cmd.Get<int>("order_number");
                    if (orderNumber < 1)
                        orderNumber = 1;
                    var scores = await API.OSU
                        .Client
                        .GetUserScoresLeagcy(
                            osuID,
                            API.OSU.UserScoreType.Best,
                            API.OSU.Mode.OSU,
                            1,
                            orderNumber - 1
                        );
                    if (scores == null)
                    {
                        await target.Treply("error.query_scores_failed");
                        return;
                    }
                    if (scores!.Length > 0)
                    {
                        await target.Treply(
                            "get.zkfc_cost_result",
                            OnlineOsuInfo.Username,
                            Math.Round(zkfccost(OnlineOsuInfo, scores[0]), 2)
                        );
                    }
                    break;
                default:
                    await target.Treply("get.cost_help");
                    break;
            }
        }

        private static double occost(User userInfo, UserPerformancesNext pppData)
        {
            double a, c, z, p;
            p = userInfo.Statistics.PP;
            z =
                1.92 * Math.Pow(pppData.JumpAimTotal, 0.953)
                + 69.7 * Math.Pow(pppData.FlowAimTotal, 0.596)
                + 0.588 * Math.Pow(pppData.SpeedTotal, 1.175)
                + 3.06 * Math.Pow(pppData.StaminaTotal, 0.993);
            a = Math.Pow(pppData.AccuracyTotal, 1.2768) * Math.Pow(p, 0.88213);
            c =
                Math.Min(
                    0.00930973 * Math.Pow(p / 1000, 2.64192) * Math.Pow(z / 4000, 1.48422),
                    7
                ) + Math.Min(a / 7554280, 3);
            return Math.Round(c, 2);
        }

        private static double oncost(User userInfo)
        {
            double fx, pp;
            pp = userInfo.Statistics.PP;
            if (pp <= 4000 && pp >= 2000)
            {
                fx = Math.Round(Math.Pow(1.00053, pp) - 2.88, 2);
                return fx;
            }
            else
            {
                return -1;
            }
        }

        private static double zkfccost(User userInfo, API.OSU.Models.Score score)
        {
            double t = 0.0;
            try
            {
                t = (double)score.PP / 125.0;
            }
            catch
            {
                t = 0.0;
            }
            return (double)userInfo.Statistics.PP / 1200.0
                + (double)userInfo.Statistics.TotalHits / 1333333.0
                + t;
        }
    }
}
