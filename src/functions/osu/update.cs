using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;
using KanonBot.Functions.OSU;
using System.IO;
using LanguageExt.UnsafeValueAccess;
using KanonBot.API.OSU;

namespace KanonBot.Functions.OSUBot
{
    public class Update
    {
        async public static Task Execute(Target target, string cmd)
        {
            #region 验证
            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Info);
            var resolved = await Accounts.ResolveCommandUser(target, command);
            if (resolved == null) return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID, mode!.Value);
            if (OnlineOsuInfo == null)
            {
                await target.reply("猫猫没有找到此用户。");
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;
            #endregion

            await target.reply("少女祈祷中...");

            if (resolved.IamUserId is not null) {
                var bindings = await API.IAM.Client.GetUserBindings(resolved.IamUserId);
                if (bindings is not null) {
                    var ppysbUid = API.IAM.Client.ExtractPpysbUid(bindings);
                    if (ppysbUid.HasValue) {
                        try { File.Delete($"./work/avatar/sb-{ppysbUid.Value}.png"); } catch { }
                    }
                }
            }

            //try { File.Delete($"./work/v1_cover/{OnlineOsuInfo!.Id}.png"); } catch { }
            try { File.Delete($"./work/avatar/{OnlineOsuInfo!.Id}.png"); } catch { }
            try { File.Delete($"./work/legacy/v1_cover/osu!web/{OnlineOsuInfo!.Id}.png"); } catch { }
            await target.reply("主要数据已更新完毕，pp+数据正在后台更新，请稍后使用info功能查看结果。");

            _ = Task.Run(async () => {
                try
                {
                    await Client.PPlus.UpdateUserPlusDataNext(OnlineOsuInfo!.Id);
                }
                catch { }//更新pp+失败，不返回信息
            });

        }
    }
}
