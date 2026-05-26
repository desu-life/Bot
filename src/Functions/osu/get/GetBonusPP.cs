using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions
{
    public class GetBonusPpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get bonuspp",
                Description = "Show osu! bonus pp information",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Alternative pp calculater", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;
            bool special_version_pp = cmd.Flag("special_pp");

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID, mode!.Value);
            if (OnlineOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;

            // 计算bonuspp
            if (OnlineOsuInfo!.Statistics.PP == 0)
            {
                await target.Treply("osu.no_scores_mode", OnlineOsuInfo.Mode.ToStr());
                return;
            }

            var allBPList = await Task.WhenAll(
                [
                    API.OSU.Client.GetUserScores(
                        OnlineOsuInfo.Id,
                        API.OSU.UserScoreType.Best,
                        mode!.Value,
                        100,
                        0,
                        LegacyOnly: special_version_pp
                    ),
                    API.OSU.Client.GetUserScores(
                        OnlineOsuInfo.Id,
                        API.OSU.UserScoreType.Best,
                        mode!.Value,
                        100,
                        100,
                        LegacyOnly: special_version_pp
                    )
                ]
            );
            var allBP = allBPList.Flatten();
            if (allBP == null)
            {
                await target.Treply("error.query_scores_failed");
                return;
            }
            if (allBP!.Length == 0)
            {
                await target.Treply("osu.no_scores_in_mode");
                return;
            }

            var (scorePP, finalBonusPP, rankedScores) = Utils.CalculateBonusPP(
                allBP,
                OnlineOsuInfo
            );
            var str =
                $"{OnlineOsuInfo.Username} ({OnlineOsuInfo.Mode.ToStr()})\n"
                + $"总PP：{OnlineOsuInfo.Statistics.PP:0.##}pp\n"
                + $"原始PP：{scorePP:0.##}pp\n"
                + $"Bonus PP：{finalBonusPP:0.##}pp\n"
                + $"共计算出 {rankedScores} 个被记录的ranked谱面成绩。";
            await target.reply(str);
        }
    }
}
