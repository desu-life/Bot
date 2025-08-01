using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using KanonBot.Functions.OSU;
using System.IO;
using LanguageExt.UnsafeValueAccess;
using KanonBot.API.OSU;
using KanonBot.OsuPerformance;

namespace KanonBot.Functions.OSUBot
{
    public class Score
    {
        async public static Task Execute(Target target, string cmd, bool ppFirst = false)
        {
            #region 验证
            long? osuID = null;
            API.OSU.Mode? mode;
            API.PPYSB.Mode? sbmode;
            Database.Model.User? DBUser = null;
            Database.Model.UserOSU? DBOsuInfo = null;
            bool is_ppysb = false;

            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Score);
            mode = command.osu_mode;
            sbmode = command.sb_osu_mode;
            bool is_query_sb = command.sb_server;

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
                    mode ??= DBOsuInfo!.osu_mode?.ToMode()!.Value ;
                    osuID = _osuinfo.Id;
                } else {
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

            #endregion

            // 解析Mod
            List<string> mods = new();
            try
            {
                mods = Enumerable
                    .Range(0, command.osu_mods.Length / 2)
                    .Select(p => new string(command.osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper())
                    .ToList();
            }
            catch { }

            // 判断是否给定了bid
            if (command.order_number == -1)
            {
                await target.reply("请提供谱面bid。");
                return;
            }

            API.OSU.Models.ScoreLazer? scoreData = null;

            if (is_ppysb) {
                // config mods
                if (mods.Find(x => x == "RX") != null) {
                    sbmode = sbmode?.ToRx();
                    if (mods.Count == 1) { mods = []; }
                }

                if (mods.Find(x => x == "AP") != null) {
                    sbmode = sbmode?.ToAp();
                    if (mods.Count == 1) { mods = []; }
                }

                using var rmods = RosuPP.Mods.FromAcronyms(string.Concat(mods), sbmode!.Value.ToOsu().ToRosu());
                var tmpScore = await API.PPYSB.Client.GetMapScore(
                    userId: osuID!.Value,
                    command.order_number,
                    sbmode.Value,
                    rmods.Bits(),
                    ppFirst
                );

                if (tmpScore is not null) {
                    scoreData = tmpScore.ToOsu(sbinfo!, sbmode!.Value);
                }
            } else {
                var scoreDatas = await API.OSU.Client.GetUserBeatmapScores(
                    osuID!.Value,
                    command.order_number,
                    mode!.Value
                );
                if (ppFirst) {
                    if (mods.Count > 0) {
                        scoreData = Utils.FilterMods(scoreDatas, mods)?.OrderByDescending(s => s.pp).FirstOrDefault();
                    } else {
                        scoreData = scoreDatas?.OrderByDescending(s => s.pp).FirstOrDefault();
                    }
                } else {
                    if (scoreDatas != null && scoreDatas.All(s => s.IsClassic)) {
                        if (mods.Count > 0) {
                            scoreData = Utils.FilterMods(scoreDatas, mods)?.OrderByDescending(s => s.ScoreAuto).FirstOrDefault();
                        } else {
                            scoreData = scoreDatas?.OrderByDescending(s => s.ScoreAuto).FirstOrDefault();
                        }
                    } else {
                        scoreData = (await API.OSU.Client.GetUserBeatmapScore(
                            osuID!.Value,
                            command.order_number,
                            mods,
                            mode!.Value
                        ) ?? await API.OSU.Client.GetUserBeatmapScore(
                            osuID!.Value,
                            command.order_number,
                            mods,
                            mode!.Value,
                            true
                        ))?.Score;
                    }
                }
            }

            if (scoreData == null)
            {
                if (command.self_query)
                    await target.reply("猫猫没有找到你的成绩");
                else
                    await target.reply("猫猫没有找到TA的成绩");
                return;
            }
            //ppy的getscore api不会返回beatmapsets信息，需要手动获取
            if (scoreData.Beatmapset is null) {
                var beatmapInfo = await API.OSU.Client.GetBeatmap(scoreData.BeatmapId);
                scoreData.Beatmap = beatmapInfo;
                scoreData.Beatmapset = beatmapInfo!.Beatmapset;
            }

            if (scoreData.User is null) {
                scoreData.User = tempOsuInfo;
            }

            Image.ScoreV2.ScorePanelData data;
            data = await UniversalCalculator.CalculatePanelData(scoreData, command.special_version_pp ? (is_ppysb ? CalculatorKind.Sb : CalculatorKind.Old) : CalculatorKind.Unset);

            using var stream = new MemoryStream();
            using var img = await Image.ScoreV2.DrawScore(data);
            await img.SaveAsync(stream, new JpegEncoder());
            await target.reply(
                new Chain().image(
                    Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                    ImageSegment.Type.Base64
                )
            );
        }
    }
}
