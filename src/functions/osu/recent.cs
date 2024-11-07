using System.IO;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions.OSUBot
{
    public class Recent
    {
        public static async Task Execute(Target target, string cmd, bool includeFails = false)
        {
            long? osuID = null;
            API.OSU.Mode? mode;
            Database.Model.User? DBUser = null;
            Database.Model.UserOSU? DBOsuInfo = null;

            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Recent);
            mode = command.osu_mode;

            // 解析指令
            if (command.self_query)
            {
                // 验证账户
                var AccInfo = Accounts.GetAccInfo(target);
                DBUser = await Accounts.GetAccount(AccInfo.uid, AccInfo.platform);
                if (DBUser == null)
                {
                    await target.reply("你还没有绑定desu.life账户，使用 !reg 你的邮箱 来进行绑定或注册喵。");
                    return;
                }
                // 验证账号信息
                DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                if (DBOsuInfo == null)
                {
                    await target.reply("你还没有绑定osu账户，请使用 !bind osu 你的osu用户名 来绑定你的osu账户喵。");
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

            //var scorePanelData = new LegacyImage.Draw.ScorePanelData();
            var scoreInfos = await API.OSU.Client.GetUserScores(
                osuID!.Value,
                API.OSU.UserScoreType.Recent,
                mode!.Value,
                20, //default was 1, due to seasonalpass set it to 50
                command.order_number - 1,
                includeFails
            );
            if (scoreInfos == null)
            {
                await target.reply("查询成绩时出错。");
                return;
            }
            // 正常是找不到玩家，但是上面有验证，这里做保险
            if (scoreInfos.Length > 0)
            {
                LegacyImage.Draw.ScorePanelData data;
                if (command.lazer)
                {
                    data = await RosuCalculator.CalculatePanelData(scoreInfos[0]);
                }
                else
                {
                    data = await UniversalCalculator.CalculatePanelDataAuto(scoreInfos[0]);
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

                _ = Task.Run(() => BeatmapTechDataProcess(scoreInfos, DBOsuInfo?.osu_uid));
            }
            else
            {
                await target.reply("猫猫找不到该玩家最近游玩的成绩。");
                return;
            }
        }

        private static async Task BeatmapTechDataProcess(Models.ScoreLazer[] scoreInfos, long? oid)
        {
            if (Config.inner!.dev) return;
            foreach (var x in scoreInfos)
            {
                //处理谱面数据
                if (x.Rank.ToUpper() != "F")
                {
                    //计算pp数据
                    var data = await RosuCalculator.CalculatePanelData(x);

                    //季票信息
                    if (oid is not null)
                    {
                        bool temp_abletoinsert = true;
                        foreach (var c in x.Mods)
                        {
                            if (c.Acronym.ToUpper() == "AP")
                                temp_abletoinsert = false;
                            if (c.Acronym.ToUpper() == "RX")
                                temp_abletoinsert = false;
                        }
                        if (temp_abletoinsert)
                            await Seasonalpass.Update(oid.Value, data);
                    }
                    //std推图
                    if (x.Mode == API.OSU.Mode.OSU)
                    {
                        if (
                            x.Beatmap!.Status == API.OSU.Models.Status.Ranked
                            || x.Beatmap!.Status == API.OSU.Models.Status.Approved
                        )
                            if (
                                x.Rank.ToUpper() == "XH"
                                || x.Rank.ToUpper() == "X"
                                || x.Rank.ToUpper() == "SH"
                                || x.Rank.ToUpper() == "S"
                                || x.Rank.ToUpper() == "A"
                            )
                            {
                                await Database.Client.InsertOsuStandardBeatmapTechData(
                                    x.Beatmap!.BeatmapId,
                                    data.ppInfo.star,
                                    (int)data.ppInfo.ppStats![0].total,
                                    (int)data.ppInfo.ppStats![0].acc!,
                                    (int)data.ppInfo.ppStats![0].speed!,
                                    (int)data.ppInfo.ppStats![0].aim!,
                                    (int)data.ppInfo.ppStats![1].total,
                                    (int)data.ppInfo.ppStats![2].total,
                                    (int)data.ppInfo.ppStats![3].total,
                                    (int)data.ppInfo.ppStats![4].total,
                                    x.Mods.Map(c => c.Acronym).ToArray()
                                );
                            }
                    }
                }
            }
        }
    }
}
