using System.Diagnostics;
using System.IO;
using FFmpeg.AutoGen;
using KanonBot.API;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using static LinqToDB.Common.Configuration;

namespace KanonBot.Functions.OSUBot
{
    public class Info
    {
        async public static Task Execute(Target target, string cmd)
        {
            #region 验证
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
                    if (command.server == "sb") {
                        var OnlineOsuInfo = await API.PPYSB.Client.GetUser(
                            command.osu_username
                        );
                        if (OnlineOsuInfo != null)
                        {
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
                sbinfo = await API.PPYSB.Client.GetUser(command.osu_username);
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

            #region 获取信息
            LegacyImage.Draw.UserPanelData data = new()
            {
                userInfo = tempOsuInfo!
            };
            // 覆写

            if (is_ppysb) {
                data.modeString = sbmode?.ToDisplay();
            } else {
                data.userInfo.Mode = mode!.Value;
            }
            // 查询

            if (DBOsuInfo != null)
            {
                if (command.order_number > 0)
                {
                    // 从数据库取指定天数前的记录
                    (data.daysBefore, data.prevUserInfo) = await Database.Client.GetOsuUserData(
                        DBOsuInfo!.osu_uid,
                        data.userInfo.Mode,
                        command.order_number
                    );
                    if (data.daysBefore > 0)
                        ++data.daysBefore;
                }
                else
                {
                    // 从数据库取最近的一次记录
                    try
                    {
                        (data.daysBefore, data.prevUserInfo) = await Database.Client.GetOsuUserData(
                            DBOsuInfo!.osu_uid,
                            data.userInfo.Mode,
                            0
                        );
                        if (data.daysBefore > 0)
                            ++data.daysBefore;
                    }
                    catch
                    {
                        data.daysBefore = 0;
                    }
                }

                var d = await Database.Client.GetOsuPPlusData(osuID!.Value);
                if (d != null)
                {
                    data.pplusInfo = d;
                }
                else
                {
                    // 设置空数据
                    data.pplusInfo = new();
                    // 异步获取osupp数据，下次请求的时候就有了
                    new Task(async () =>
                    {
                        try
                        {
                            await Database.Client.UpdateOsuPPlusData(
                                (await API.OSU.Client.TryGetUserPlusData(tempOsuInfo!))!.User,
                                tempOsuInfo!.Id
                            );
                        }
                        catch { } //更新pp+失败，不返回信息
                    }).RunSynchronously();
                }

                var badgeID = DBUser!.displayed_badge_ids;
                // 由于v1v2绘制位置以及绘制方向的不同，legacy(v1)只取第一个badge
                if (badgeID != null)
                {
                    try
                    {
                        if (badgeID!.IndexOf(",") != -1)
                        {
                            var y = badgeID.Split(",");
                            foreach (var x in y)
                                data.badgeId.Add(int.Parse(x));
                        }
                        else
                        {
                            data.badgeId.Add(int.Parse(badgeID!));
                        }
                    }
                    catch
                    {
                        data.badgeId = new() { -1 };
                    }
                }
                else
                {
                    data.badgeId = new() { -1 };
                }
            }
            else if (!is_ppysb)
            {
                var d = await Database.Client.GetOsuPPlusData(osuID!.Value);
                if (d != null)
                {
                    data.pplusInfo = d;
                }
                else
                {
                    // 设置空数据
                    data.pplusInfo = new();
                    // 异步获取osupp数据，下次请求的时候就有了
                    new Task(async () =>
                    {
                        try
                        {
                            var temppppinfo = await API.OSU.Client.TryGetUserPlusData(tempOsuInfo!);
                            if (temppppinfo == null)
                                return;
                            await Database.Client.UpdateOsuPPlusData(
                                temppppinfo!.User,
                                tempOsuInfo!.Id
                            );
                        }
                        catch { } //更新pp+失败，不返回信息
                    }).RunSynchronously();
                }
            }

            #endregion

            var isDataOfDayAvaiavle = false;
            if (data.daysBefore > 0)
                isDataOfDayAvaiavle = true;

            int custominfoengineVer = 2;
            if (DBOsuInfo != null)
            {
                custominfoengineVer = DBOsuInfo!.customInfoEngineVer;
                if (Enum.IsDefined(typeof(LegacyImage.Draw.UserPanelData.CustomMode), DBOsuInfo.InfoPanelV2_Mode))
                {
                    data.customMode = (LegacyImage.Draw.UserPanelData.CustomMode)DBOsuInfo.InfoPanelV2_Mode;
                    if (data.customMode == LegacyImage.Draw.UserPanelData.CustomMode.Custom)
                        data.ColorConfigRaw = DBOsuInfo.InfoPanelV2_CustomMode!;
                }
                else
                {
                    throw new Exception("未知的自定义模式");
                }
            }

            using var stream = new MemoryStream();
            //info默认输出高质量图片？
            SixLabors.ImageSharp.Image img;
            API.OSU.Models.ScoreLazer[]? allBP = [];
            switch (custominfoengineVer) //0=null 1=v1 2=v2
            {
                case 1:
                    img = await LegacyImage.Draw.DrawInfo(
                        data,
                        DBOsuInfo != null,
                        isDataOfDayAvaiavle
                    );
                    await img.SaveAsync(stream, new PngEncoder());
                    break;
                case 2:
                    var v2Options = data.customMode switch
                    {
                        LegacyImage.Draw.UserPanelData.CustomMode.Custom => DrawV2.OsuInfoPanelV2.InfoCustom.ParseColors(data.ColorConfigRaw, None),
                        LegacyImage.Draw.UserPanelData.CustomMode.Light => DrawV2.OsuInfoPanelV2.InfoCustom.LightDefault,
                        LegacyImage.Draw.UserPanelData.CustomMode.Dark => DrawV2.OsuInfoPanelV2.InfoCustom.DarkDefault,
                        _ => throw new ArgumentOutOfRangeException("未知的自定义模式")
                    };
                    

                    if (is_ppysb) {
                        var ss = await API.PPYSB.Client.GetUserScores(
                            data.userInfo.Id,
                            API.PPYSB.UserScoreType.Best,
                            sbmode!.Value,
                            20,
                            0
                        );
                        allBP = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
                    } else {
                        allBP = await API.OSU.Client.GetUserScores(
                            data.userInfo.Id,
                            API.OSU.UserScoreType.Best,
                            data.userInfo.Mode,
                            20,
                            0
                        );
                    }

                    img = await DrawV2.OsuInfoPanelV2.Draw(
                        data,
                        allBP!,
                        v2Options,
                        DBOsuInfo != null,
                        false,
                        isDataOfDayAvaiavle,
                        false,
                        kind: command.lazer ? is_ppysb ? CalculatorKind.Sb : CalculatorKind.Rosu : CalculatorKind.Unset
                    );
                    await img.SaveAsync(stream, new PngEncoder());
                    break;
                default:
                    return;
            }
            // 关闭流
            img.Dispose();


            await target.reply(
                new Chain().image(
                    Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                    ImageSegment.Type.Base64
                )
            );

            _ = Task.Run(async () => {
                if (Config.inner!.dev) return;
                try
                {
                    if (data.userInfo.Mode == API.OSU.Mode.OSU) //只存std的
                        if (allBP!.Length > 0)
                            await InsertBeatmapTechInfo(allBP);
                        else
                        {
                            allBP = await API.OSU.Client.GetUserScores(
                            data.userInfo.Id,
                            API.OSU.UserScoreType.Best,
                            API.OSU.Mode.OSU,
                            20,
                            0
                        );
                            if (allBP!.Length > 0)
                                await InsertBeatmapTechInfo(allBP);
                        }
                }
                catch { }
            });
        }

        async public static Task InsertBeatmapTechInfo(API.OSU.Models.ScoreLazer[] allbp)
        {
            foreach (var score in allbp)
            {
                //计算pp
                try
                {
                    if (score.Rank.ToUpper() == "XH" ||
                           score.Rank.ToUpper() == "X" ||
                           score.Rank.ToUpper() == "SH" ||
                           score.Rank.ToUpper() == "S" ||
                           score.Rank.ToUpper() == "A")
                    {
                        var data = await RosuCalculator.CalculatePanelData(score);
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
                                    score.Mods.Map(x => x.Acronym).ToArray()
                                );
                    }
                }
                catch
                {
                    //无视错误
                }
            }
        }
    }
}
