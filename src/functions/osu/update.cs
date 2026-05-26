using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.API;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using LanguageExt.UnsafeValueAccess;

namespace KanonBot.Functions.OSUBot
{
    public class UpdateCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "update",
                Description = "Refresh cached osu! user data",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                ],
                Flags =  [ new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" }, ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            #region 验证
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
            #endregion

            await target.Treply("osu.update_in_progress");

            if (resolved.IamUserId is not null)
            {
                var bindings = await API.IAM.Client.GetUserBindings(resolved.IamUserId);
                if (bindings is not null)
                {
                    var ppysbUid = API.IAM.Client.ExtractPpysbUid(bindings);
                    if (ppysbUid.HasValue)
                    {
                        try
                        {
                            File.Delete($"./work/avatar/sb-{ppysbUid.Value}.png");
                        }
                        catch { }
                    }
                }
            }

            //try { File.Delete($"./work/v1_cover/{OnlineOsuInfo!.Id}.png"); } catch { }
            try
            {
                File.Delete($"./work/avatar/{OnlineOsuInfo!.Id}.png");
            }
            catch { }
            try
            {
                File.Delete($"./work/legacy/v1_cover/osu!web/{OnlineOsuInfo!.Id}.png");
            }
            catch { }
            await target.Treply("osu.update_done");

            _ = Task.Run(async () =>
            {
                try
                {
                    await Client.PPlus.UpdateUserPlusDataNext(OnlineOsuInfo!.Id);
                }
                catch { } //更新pp+失败，不返回信息
            });
        }
    }
}
