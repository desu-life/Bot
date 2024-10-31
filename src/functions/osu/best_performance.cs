using System.IO;
using System.Security.Cryptography;
using KanonBot.API;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using RosuPP;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions.OSUBot
{
    public class BestPerformance
    {
        public static async Task Execute(Target target, string cmd)
        {
            #region 验证
            long? osuID = null;
            API.OSU.Mode? mode;
            Database.Model.User? DBUser = null;
            Database.Model.UserOSU? DBOsuInfo = null;

            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.BestPerformance);
            mode = command.osu_mode;

            // 解析指令
            if (command.self_query)
            {
                // 验证账户
                var AccInfo = Accounts.GetAccInfo(target);
                DBUser = await Accounts.GetAccount(AccInfo.uid, AccInfo.platform);
                if (DBUser == null)
                // { await target.reply("您还没有绑定Kanon账户，请使用!reg 您的邮箱来进行绑定或注册。"); return; }    // 这里引导到绑定osu
                {
                    await target.reply("您还没有绑定osu账户，请使用!bind osu 您的osu用户名 来绑定您的osu账户。");
                    return;
                }
                // 验证账号信息
                DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                if (DBOsuInfo == null)
                {
                    await target.reply("您还没有绑定osu账户，请使用!bind osu 您的osu用户名 来绑定您的osu账户。");
                    return;
                }

                mode ??= DBOsuInfo.osu_mode?.ToMode()!.Value; // 从数据库解析，理论上不可能错
                osuID = DBOsuInfo.osu_uid;
            }
            else
            {
                // 查询用户是否绑定
                // 这里先按照at方法查询，查询不到就是普通用户查询
                var (atOSU, atDBUser) = await Accounts.ParseAt(command.osu_username);
                if (atOSU.IsNone && !atDBUser.IsNone)
                {
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
                }
                else if (!atOSU.IsNone && atDBUser.IsNone)
                {
                    var _osuinfo = atOSU.ValueUnsafe();
                    mode ??= _osuinfo.Mode;
                    osuID = _osuinfo.Id;
                }
                else if (!atOSU.IsNone && !atDBUser.IsNone)
                {
                    DBUser = atDBUser.ValueUnsafe();
                    DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                    var _osuinfo = atOSU.ValueUnsafe();
                    mode ??= DBOsuInfo!.osu_mode?.ToMode()!.Value;
                    osuID = _osuinfo.Id;
                }
                else
                {
                    // 普通查询
                    var OnlineOsuInfo = await API.OSU.Client.GetUser(
                        command.osu_username,
                        command.osu_mode ?? API.OSU.Mode.OSU
                    );
                    if (OnlineOsuInfo != null)
                    {
                        DBOsuInfo = await Database.Client.GetOsuUser(OnlineOsuInfo.Id);
                        if (DBOsuInfo != null)
                        {
                            DBUser = await Accounts.GetAccountByOsuUid(OnlineOsuInfo.Id);
                            mode ??= DBOsuInfo.osu_mode?.ToMode()!.Value;
                        }
                        mode ??= OnlineOsuInfo.Mode;
                        osuID = OnlineOsuInfo.Id;
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
            var tempOsuInfo = await API.OSU.Client.GetUser(osuID!.Value, mode!.Value);
            if (tempOsuInfo == null)
            {
                await target.reply("猫猫没有找到此用户。");
                // 中断查询
                return;
            }

            #endregion

            // 输入检查
            if (command.order_number < 1 && command.order_number > 100)
            {
                command.order_number = 1;
            }

            var scores = await API.OSU.Client.GetUserScores(
                osuID!.Value,
                API.OSU.UserScoreType.Best,
                mode!.Value,
                1,
                command.order_number - 1
            );

            if (scores == null)
            {
                await target.reply("查询成绩时出错。");
                return;
            }
            if (scores!.Length > 0)
            {
                LegacyImage.Draw.ScorePanelData data;
                if (command.lazer)
                {
                    data = await RosuCalculator.CalculatePanelData(scores[0]);
                }
                else
                {
                    data = await UniversalCalculator.CalculatePanelDataAuto(scores[0]);
                }
                using var stream = new MemoryStream();

                using var img =
                    (Config.inner != null && Config.inner.debug)
                        ? await DrawV3.OsuScorePanelV3.Draw(data)
                        : await LegacyImage.Draw.DrawScore(data);

                await img.SaveAsync(stream, new JpegEncoder());
                await target.reply(
                    new Chain().image(
                        Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                        ImageSegment.Type.Base64
                    )
                );

                _ = Task.Run(() => BeatmapTechDataProcess(scores[0], data));
            }
            else
            {
                await target.reply("猫猫找不到该BP。");
                return;
            }
        }

        private static async Task BeatmapTechDataProcess(
            Models.ScoreLazer score,
            LegacyImage.Draw.ScorePanelData data
        )
        {
            if (Config.inner!.dev) return;
            if (score.Mode == API.OSU.Mode.OSU)
            {
                if (
                    score.Beatmap!.Status == API.OSU.Models.Status.ranked
                    || score.Beatmap!.Status == API.OSU.Models.Status.approved
                )
                {
                    await Database.Client.InsertOsuStandardBeatmapTechData(
                        score.Beatmap!.BeatmapId,
                        data.ppInfo.star,
                        (int)data.ppInfo.ppStats![0].total,
                        (int)data.ppInfo.ppStats![0].acc!,
                        (int)data.ppInfo.ppStats![0].speed!,
                        (int)data.ppInfo.ppStats![0].aim!,
                        (int)data.ppInfo.ppStats![1].total,
                        (int)data.ppInfo.ppStats![2].total,
                        (int)data.ppInfo.ppStats![3].total,
                        (int)data.ppInfo.ppStats![4].total,
                        score.Mods.Map(m => m.Acronym).ToArray()
                    );
                }
            }
        }
    }
}
