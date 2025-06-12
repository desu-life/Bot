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
            long? osuID = null;
            API.OSU.Mode? mode;
            Database.Model.User? DBUser = null;
            Database.Model.UserOSU? DBOsuInfo = null;

            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Info);
            mode = command.osu_mode;

            // 解析指令
            if (command.self_query)
            {
                // 验证账户
                var AccInfo = Accounts.GetAccInfo(target);
                DBUser = await Accounts.GetAccount(AccInfo.uid, AccInfo.platform);
                if (DBUser == null)
                { await target.reply("你还没有绑定 desu.life 账户，先使用 !reg 你的邮箱 来进行绑定或注册哦。"); return; }
                // 验证账号信息
                DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                if (DBOsuInfo == null)
                { await target.reply("你还没有绑定osu账户，请使用 !bind osu 你的osu用户名 来绑定你的osu账户喵。"); return; }
                mode ??= DBOsuInfo.osu_mode?.ToMode()!.Value;    // 从数据库解析，理论上不可能错
                osuID = DBOsuInfo.osu_uid;
            }
            else
            {
                // 查询用户是否绑定
                var (atOSU, atDBUser) = await Accounts.ParseAtOsu(command.osu_username);
                if (atOSU.IsNone && !atDBUser.IsNone) {
                    DBUser = atDBUser.ValueUnsafe();
                    DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                    if (DBOsuInfo == null)
                    {
                        await target.reply("ta还没有绑定osu账户呢。");
                    }
                    else
                    {
                        await target.reply("被办了。");
                    }
                    return;
                } else if (!atOSU.IsNone && atDBUser.IsNone) {
                    var _osuinfo = atOSU.ValueUnsafe();
                    mode ??= _osuinfo.Mode;
                    osuID = _osuinfo.Id;
                } else if (!atOSU.IsNone && !atDBUser.IsNone) {
                    DBUser = atDBUser.ValueUnsafe();
                    DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                    var _osuinfo = atOSU.ValueUnsafe();
                    mode ??= DBOsuInfo!.osu_mode?.ToMode()!.Value;
                    osuID = _osuinfo.Id;
                } else {
                    // 普通查询
                    var tempOsuInfo = await API.OSU.Client.GetUser(command.osu_username, command.osu_mode ?? API.OSU.Mode.OSU);
                    if (tempOsuInfo != null)
                    {
                        DBOsuInfo = await Database.Client.GetOsuUser(tempOsuInfo.Id);
                        if (DBOsuInfo != null)
                        {
                            DBUser = await Accounts.GetAccountByOsuUid(tempOsuInfo.Id);
                            mode ??= DBOsuInfo.osu_mode?.ToMode()!.Value;
                        }
                        mode ??= tempOsuInfo.Mode;
                        osuID = tempOsuInfo.Id;
                    }
                    else
                    {
                        // 直接取消查询，简化流程
                        await target.reply("猫猫没有找到此用户。");
                        return;
                    }
                }
            }

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID!.Value, mode!.Value);
            if (OnlineOsuInfo == null)
            {
                await target.reply("猫猫没有找到此用户。");
                // 中断查询
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;
            #endregion

            await target.reply("少女祈祷中...");

            if (DBUser is not null) {
                var sbdbinfo = await Accounts.CheckPpysbAccount(DBUser.uid);
                if (sbdbinfo is not null) {
                    try { File.Delete($"./work/avatar/sb-{sbdbinfo.osu_uid}.png"); } catch { }
                }
            }

            //try { File.Delete($"./work/v1_cover/{OnlineOsuInfo!.Id}.png"); } catch { }
            try { File.Delete($"./work/avatar/{OnlineOsuInfo!.Id}.png"); } catch { }
            try { File.Delete($"./work/legacy/v1_cover/osu!web/{OnlineOsuInfo!.Id}.png"); } catch { }
            await target.reply("主要数据已更新完毕，pp+数据正在后台更新，请稍后使用info功能查看结果。");

            _ = Task.Run(async () => {
                try
                {
                    var data = await Client.PPlus.UpdateUserPlusDataNext(OnlineOsuInfo!.Id);
                    await Database.Client.UpdateOsuPPlusDataNext(data!);   
                }
                catch { }//更新pp+失败，不返回信息
            });

        }
    }
}
