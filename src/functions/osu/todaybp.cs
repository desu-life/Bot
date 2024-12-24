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
    public class TodayBP
    {
        public static async Task Execute(Target target, string cmd, bool includeFails = false)
        {
            long? osuID = null;
            API.OSU.Mode? mode;
            API.PPYSB.Mode? sbmode;
            Database.Model.User? DBUser = null;
            Database.Model.UserOSU? DBOsuInfo = null;
            bool is_ppysb = false;

            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Info);
            mode = command.osu_mode;
            sbmode = command.sb_osu_mode;
            bool is_query_sb = command.server == "sb";

            // 解析指令
            if (command.self_query)
            {
                // 验证账户
                var AccInfo = Accounts.GetAccInfo(target);
                DBUser = await Accounts.GetAccount(AccInfo.uid, AccInfo.platform);
                if (DBUser == null)
                {
                    await target.reply("你还没有绑定 desu.life 账户，先使用 !reg 你的邮箱 来进行绑定或注册哦。");
                    return;
                }
                // 验证账号信息
                DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                if (DBOsuInfo == null)
                {
                    await target.reply("你还没有绑定osu账户，请使用 !bind osu 你的osu用户名 来绑定你的osu账户喵。");
                    return;
                }

                if (is_query_sb) {
                    var sbdbinfo = await Accounts.CheckPpysbAccount(DBUser.uid);
                    if (sbdbinfo == null)
                    {
                        await target.reply("请先绑定sb服。");
                        return;
                    }
                    sbmode ??= sbdbinfo.mode?.ToPpysbMode();
                    osuID = sbdbinfo.osu_uid;
                    is_ppysb = true;
                } else {
                    mode ??= DBOsuInfo.osu_mode?.ToMode(); // 从数据库解析，理论上不可能错
                    osuID = DBOsuInfo.osu_uid;
                }
            }
            else
            {
                // 查询用户是否绑定
                // 这里先按照at方法查询，查询不到就是普通用户查询
                var (atOSU, atDBUser) = await Accounts.ParseAtOsu(command.osu_username);
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
                    if (is_query_sb) {
                        var OnlineOsuInfo = await API.PPYSB.Client.GetUser(
                            command.osu_username
                        );
                        if (OnlineOsuInfo != null)
                        {
                            var sbdbinfo = await Database.Client.GetPpysbUser(OnlineOsuInfo.Info.Id);
                            if (sbdbinfo != null)
                            {
                                DBUser = await Accounts.GetAccountByPpysbUid(OnlineOsuInfo.Info.Id);
                                if (DBUser != null) {
                                    DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                                }
                                sbmode ??= sbdbinfo.mode?.ToPpysbMode();
                            }

                            sbmode ??= OnlineOsuInfo.Info.PreferredMode;
                            osuID = OnlineOsuInfo.Info.Id;
                            is_ppysb = true;
                        }
                        else
                        {
                            // 直接取消查询，简化流程
                            await target.reply("猫猫没有找到此用户。");
                            return;
                        }
                    } else {
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
            }

            // 验证osu信息
            API.OSU.Models.UserExtended? tempOsuInfo = null;
            API.PPYSB.Models.User? sbinfo = null;
            if (is_ppysb) {
                sbinfo = await API.PPYSB.Client.GetUser(osuID!.Value);
                tempOsuInfo = sbinfo?.ToOsu(sbmode);
            } else {
                tempOsuInfo = await API.OSU.Client.GetUser(osuID!.Value, mode!.Value);
            }
            if (tempOsuInfo == null)
            {
                await target.reply("猫猫没有找到此用户。");
                // 中断查询
                return;
            }

            API.OSU.Models.ScoreLazer[]? scoreInfos = null;

            if (is_ppysb) {
                var ss = await API.PPYSB.Client.GetUserScores(
                    osuID!.Value,
                    API.PPYSB.UserScoreType.Best,
                    sbmode!.Value,
                    100,
                    0,
                    includeFails
                );
                scoreInfos = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            } else {
                scoreInfos = await API.OSU.Client.GetUserScores(
                    osuID!.Value,
                    API.OSU.UserScoreType.Best,
                    mode!.Value,
                    100,
                    0,
                    includeFails
                );
            }

            if (scoreInfos == null)
            {
                await target.reply("查询成绩时出错。");
                return;
            }
            // 正常是找不到玩家，但是上面有验证，这里做保险
            if (scoreInfos.Length > 0)
            {
                List<image.ScoreList.ScoreRank> scores = [];
                var now = DateTime.Now;
                var t = now.Hour < 4 ? now.Date.AddDays(-1).AddHours(4) : now.Date.AddHours(4);

                t = t.AddDays(-command.order_number);

                for (int i = 0; i < scoreInfos.Length; i++)
                {
                    var item = scoreInfos[i];
                    var bp_time = item.EndedAt.ToLocalTime();

                    if (bp_time >= t)
                    {
                        scores.Add(new image.ScoreList.ScoreRank {
                            Score = item,
                            Rank = i + 1
                        });
                    }
                }

                if (scores.Count == 0) {
                    if (command.self_query) {
                        await target.reply($"你今天在 {tempOsuInfo.Mode.ToStr()} 模式上还没有新bp呢。。");
                    } else {
                        await target.reply(
                            $"{tempOsuInfo.Username} 今天在 {tempOsuInfo.Mode.ToStr()} 模式上还没有新bp呢。。"
                        );
                    }
                    return;
                }

                await Parallel.ForEachAsync(scores, async (s, _) => {
                    var b = await Utils.LoadOrDownloadBeatmap(s.Score.Beatmap!);
                    s.PPInfo = UniversalCalculator.CalculateData(b, s.Score, command.lazer ? is_ppysb ? CalculatorKind.Sb : CalculatorKind.Oppai : CalculatorKind.Unset);
                });
                
                scores.Sort((a, b) => b.PPInfo!.ppStat.total > a.PPInfo!.ppStat.total ? 1 : -1);

                using var img = await KanonBot.image.ScoreList.Draw(
                    KanonBot.image.ScoreList.Type.TODAYBP,
                    scores,
                    tempOsuInfo
                );

                using var stream = new MemoryStream();
                await img.SaveAsync(stream, new PngEncoder());
                await target.reply(
                    new Chain().image(
                        Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                        ImageSegment.Type.Base64
                    )
                );
            }
            else
            {
                if (command.self_query) {
                    await target.reply($"你在 {tempOsuInfo.Mode.ToStr()} 模式上还没有bp呢。。");
                } else {
                    await target.reply(
                        $"{tempOsuInfo.Username} 在 {tempOsuInfo.Mode.ToStr()} 模式上还没有bp呢。。"
                    );
                }
                return;
            }
        }

    }
}
    