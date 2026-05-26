using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetSeasonalPassCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get seasonalpass",
                Description = "Show seasonal pass information",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                ],
                Flags = [ new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" } ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
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

            var seasonalpassinfo = await Database
                .Client
                .GetSeasonalPassInfo(OnlineOsuInfo!.Id, OnlineOsuInfo!.Mode!.ToStr())!;
            if (seasonalpassinfo == null)
            {
                await target.Treply("osu.no_seasonal_pass");
                return;
            }

            //100point一级，每升1级所需point+20
            long temppoint = seasonalpassinfo.point;
            int levelcount = 0;
            while (true)
            {
                temppoint -= (100 + levelcount * 20);
                if (temppoint > 0)
                    levelcount++;
                else
                    break;
            }
            int tt = 0;
            for (int i = 0; i < levelcount; ++i)
            {
                tt += 100 + i * 20;
            }
            double t = Math.Round(
                Math.Round(
                    (
                        (double)((seasonalpassinfo.point - tt) * 100)
                        / (double)(100 + levelcount * 20)
                    ),
                    4
                ),
                4
            );

            string str;
            str =
                $"{OnlineOsuInfo.Username}\n自2023年7月15日以来\n您在{OnlineOsuInfo!.Mode!.ToStr()}模式下的等级为{levelcount}级 "
                + $"({t}%)"
                + $"\n共获得了了{seasonalpassinfo.point}pt\n距离升级大约还需要{Math.Abs(temppoint)}pt";
            await target.reply(str);
        }
    }
}
