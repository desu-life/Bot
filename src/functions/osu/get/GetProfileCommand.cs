using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetProfileCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get mu",
                Description = "Show an osu! profile link",
                SlashName = "get-profile",
                Aliases = [ "get profile" ],
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags = [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID, API.OSU.Mode.OSU); //取osu模式的值
            if (OnlineOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;

            await target.reply(
                $"{OnlineOsuInfo.Username}\nhttps://osu.ppy.sh/u/{OnlineOsuInfo.Id}"
            );
        }
    }
}
