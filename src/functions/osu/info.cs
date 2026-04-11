using System.Diagnostics;
using System.IO;
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
            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Info);
            var resolved = await Accounts.ResolveCommandUser(target, command);
            if (resolved == null) return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;
            API.PPYSB.Mode? sbmode = resolved.SbMode;
            bool is_ppysb = resolved.IsPpysb;

            // 验证osu信息
            API.OSU.Models.UserExtended? tempOsuInfo = null;
            API.PPYSB.Models.User? sbinfo = null;
            if (is_ppysb) {
                sbinfo = await API.PPYSB.Client.GetUser(osuID);
                tempOsuInfo = sbinfo?.ToOsu(sbmode);
            } else {
                tempOsuInfo = await API.OSU.Client.GetUser(osuID, mode!.Value);
            }
            if (tempOsuInfo == null)
            {
                await target.reply("猫猫没有找到此用户。");
                return;
            }

            #endregion

            #region 获取信息
            Image.InfoV1.UserPanelData data = new()
            {
                osuId = osuID,
                userInfo = tempOsuInfo!
            };
            // 覆写

            if (is_ppysb)
            {
                data.userInfo.Mode = sbmode!.Value.ToOsu();
                data.modeString = sbmode?.ToDisplay();
                data.osuId = 0;

                if (data.userInfo.Mode == Mode.OSU)
                {
                    data.pplusInfo = new();
                }
                
            }
            else
            {
                data.userInfo.Mode = mode!.Value;
                if (resolved.OsuId > 0)
                {
                    if (command.order_number > 0)
                    {
                        // 从数据库取指定天数前的记录
                        (data.daysBefore, data.prevUserInfo) = await Database.Client.GetOsuUserData(
                            resolved.OsuId,
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
                                resolved.OsuId,
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

                    

                }
                
                if (data.userInfo.Mode == Mode.OSU)
                {
                    var d = await Client.PPlus.GetUserPlusDataNext(osuID);
                    if (d is not null)
                    {
                        data.pplusInfo = d.Performances;
                        await Database.Client.UpdateOsuPPlusDataNext(d);
                    }
                    else
                    {
                        d = await Database.Client.GetOsuPPlusDataNext(osuID);
                        if (d is not null)
                        {
                            data.pplusInfo = d.Performances;
                        }
                        else
                        {
                            data.pplusInfo = new();
                        }
                    }
                }
            }

            

            #endregion

            var isDataOfDayAvaiavle = false;
            if (data.daysBefore > 0)
                isDataOfDayAvaiavle = true;

            int custominfoengineVer = 2;
            
            if (resolved.IamUserId != null)
            {
                // Try to fetch badges + settings + images from Kagami via IAM
                API.Kagami.KanonImages? kagamiImages = null;
                List<API.Kagami.UserBadgeResponse>? kagamiBadges = null;
                try
                {
                    var iamUserId = resolved.IamUserId;
                    // Fetch profile (panel settings), images, and badges in parallel
                    var imagesTask = API.Kagami.Client.GetKanonImages(iamUserId);
                    var badgesTask = API.Kagami.Client.GetUserWearBadges(iamUserId);
                    await Task.WhenAll(imagesTask, badgesTask);

                    kagamiImages = imagesTask.Result;
                    kagamiBadges = badgesTask.Result;

                    // Populate badges from Kagami
                    if (kagamiBadges != null && kagamiBadges.Count > 0)
                    {
                        data.badgeImageUrls = kagamiBadges
                            .Where(b => !string.IsNullOrEmpty(b.ImageUrl))
                            .Select(b => API.Kagami.Client.NormalizeAssetUrl(b.ImageUrl))
                            .Where(url => !string.IsNullOrEmpty(url))
                            .Select(url => url!)
                            .ToList();
                    }

                    // Populate image URLs from Kagami
                    if (kagamiImages != null)
                    {
                        data.v1PanelUrl = API.Kagami.Client.NormalizeAssetUrl(kagamiImages.InfoPanelV1ImageUrl);
                        data.v1CoverUrl = API.Kagami.Client.NormalizeAssetUrl(kagamiImages.InfoPanelV1CoverImageUrl);
                        data.v2SideImageUrl = API.Kagami.Client.NormalizeAssetUrl(kagamiImages.InfoPanelV2CoverImageUrl);
                        data.v2PanelUrl = API.Kagami.Client.NormalizeAssetUrl(kagamiImages.InfoPanelV2ImageUrl);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to fetch data from Kagami, falling back to local DB");
                }

                // Read panel config from Kagami settings
                var panelVersionStr = kagamiImages?.InfoPanelDefaultVersion;
                if (!string.IsNullOrEmpty(panelVersionStr))
                {
                    custominfoengineVer = panelVersionStr.ToLowerInvariant() == "v2" ? 2 : 1;
                }

                var colorModeStr = kagamiImages?.InfoPanelV2ColorMode;
                if (!string.IsNullOrEmpty(colorModeStr))
                {
                    var parsed = colorModeStr.ToLowerInvariant() switch
                    {
                        "custom" => Image.InfoV1.UserPanelData.CustomMode.Custom,
                        "light" => Image.InfoV1.UserPanelData.CustomMode.Light,
                        "dark" => Image.InfoV1.UserPanelData.CustomMode.Dark,
                        _ => (Image.InfoV1.UserPanelData.CustomMode?)null
                    };
                    if (parsed != null)
                    {
                        data.customMode = parsed.Value;
                        if (data.customMode == Image.InfoV1.UserPanelData.CustomMode.Custom)
                            data.ColorConfigRaw = kagamiImages!.infoPanelV2CustomThemeJson ?? "";
                    }
                }
                data.osuId = resolved.OsuId;
            }


            if (command.special_panel) custominfoengineVer = custominfoengineVer == 1 ? 2 : 1;

            using var stream = new MemoryStream();
            //info默认输出高质量图片？
            SixLabors.ImageSharp.Image img;
            API.OSU.Models.ScoreLazer[]? allBP = [];
            switch (custominfoengineVer) //0=null 1=v1 2=v2
            {
                case 1:
                    img = await Image.InfoV1.DrawInfo(
                        data,
                        resolved.IsRegistered,
                        isDataOfDayAvaiavle
                    );
                    await img.SaveAsync(stream, new PngEncoder());
                    break;
                case 2:
                    var v2Options = data.customMode switch
                    {
                        Image.InfoV1.UserPanelData.CustomMode.Custom => Image.OsuInfoPanelV2.InfoCustom.ParseColors(data.ColorConfigRaw, None),
                        Image.InfoV1.UserPanelData.CustomMode.Light => Image.OsuInfoPanelV2.InfoCustom.LightDefault,
                        Image.InfoV1.UserPanelData.CustomMode.Dark => Image.OsuInfoPanelV2.InfoCustom.DarkDefault,
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

                    img = await Image.OsuInfoPanelV2.Draw(
                        data,
                        allBP!,
                        v2Options,
                        resolved.IsRegistered,
                        false,
                        isDataOfDayAvaiavle,
                        false,
                        kind: command.special_version_pp ? (is_ppysb ? CalculatorKind.Sb : CalculatorKind.Old) : CalculatorKind.Unset
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

            if (Config.inner!.dev) return;
            _ = Task.Run(async () => {
                if (is_ppysb) return;
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
                        var data = await UniversalCalculator.CalculatePanelData(score);
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
