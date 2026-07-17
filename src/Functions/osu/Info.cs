using System.Diagnostics;
using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.API;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using static LinqToDB.Common.Configuration;

namespace KanonBot.Functions
{
    public class InfoCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "info",
                Description = "Show an osu! user profile panel",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon, Parse = s => s },
                    new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
new() { Name = "pp_version", LegacyName = "pp", Description = "PP algorithm version", Prefix = ArgPrefix.And },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use osu! current PP (legacy alias)", Value = "", SlashName = "is_special_pp" },
                    new()
                    {
                        Name = "legacy_scores",
                        Description = "Fetch only legacy osu! scores",
                        Value = "legacy",
                        SlashName = "is_legacy_scores",
                    },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                    new() { Name = "dev_panel", Description = "Use the Dev panel", Value = "dev", SlashName = "is_dev" },
                    new() { Name = "sp_panel", Description = "Use the v1 panel", Value = "p", SlashName = "is_special_panel" },
                ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            #region 验证
            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;
            API.PPYSB.Mode? sbmode = resolved.SbMode;
            bool is_ppysb = resolved.IsPpysb;

            var orderNumber = cmd.Get<int>("order_number");
            bool special_version_pp = cmd.Flag("special_pp");
            bool legacyScoresOnly = special_version_pp || cmd.Flag("legacy_scores");
            bool recalculateTotalPp = special_version_pp || cmd.Has("pp_version");
            bool dev_panel = cmd.Flag("dev_panel");
            bool special_panel = cmd.Flag("sp_panel");

            // 验证osu信息
            var (tempOsuInfo, sbinfo) = await Utils.ResolveOsuUser(resolved);
            if (tempOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }

            var calculationMode = is_ppysb ? sbmode!.Value.ToOsu() : mode!.Value;
            var calculatorId = await PerformanceCommandOptions.Resolve(
                target,
                cmd,
                calculationMode,
                is_ppysb
            );
            if (calculatorId is null)
                return;

            #endregion

            #region 获取信息
            Image.InfoV1.UserPanelData data = new() { osuId = osuID, userInfo = tempOsuInfo! };
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
                    if (orderNumber > 0)
                    {
                        // 从数据库取指定天数前的记录
                        (data.daysBefore, data.prevUserInfo) = await Database
                            .Client
                            .GetOsuUserData(resolved.OsuId, data.userInfo.Mode, orderNumber);
                        if (data.daysBefore > 0)
                            ++data.daysBefore;
                    }
                    else
                    {
                        // 从数据库取最近的一次记录
                        try
                        {
                            (data.daysBefore, data.prevUserInfo) = await Database
                                .Client
                                .GetOsuUserData(resolved.OsuId, data.userInfo.Mode, 0);
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
                        data.v1PanelUrl = API.Kagami
                            .Client
                            .NormalizeAssetUrl(kagamiImages.InfoPanelV1ImageUrl);
                        data.v1CoverUrl = API.Kagami
                            .Client
                            .NormalizeAssetUrl(kagamiImages.InfoPanelV1CoverImageUrl);
                        data.v2SideImageUrl = API.Kagami
                            .Client
                            .NormalizeAssetUrl(kagamiImages.InfoPanelV2CoverImageUrl);
                        data.v2PanelUrl = API.Kagami
                            .Client
                            .NormalizeAssetUrl(kagamiImages.InfoPanelV2ImageUrl);
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
                            data.CustomTheme = kagamiImages!.infoPanelV2CustomThemeJson;
                    }
                }
                data.osuId = resolved.OsuId;
            }

            if (special_panel)
                custominfoengineVer = custominfoengineVer == 1 ? 2 : 1;

            //info默认输出高质量图片？
            API.OSU.Models.ScoreLazer[]? allBP =  [ ];
            switch (custominfoengineVer) //0=null 1=v1 2=v2
            {
                case 1:
                    if (recalculateTotalPp)
                    {
                        if (is_ppysb)
                        {
                            var scores = await API.PPYSB.Client.GetUserScores(
                                data.userInfo.Id,
                                API.PPYSB.UserScoreType.Best,
                                sbmode!.Value,
                                100,
                                0
                            );
                            if (scores is null)
                            {
                                await target.Treply("error.query_scores_failed");
                                return;
                            }
                            allBP = scores?.Map(score => score.ToOsu(sbinfo!, sbmode.Value)).ToArray();
                        }
                        else
                        {
                            var allBPList = await Task.WhenAll(
                                [
                                    API.OSU.Client.GetUserScores(
                                        data.userInfo.Id,
                                        API.OSU.UserScoreType.Best,
                                        mode!.Value,
                                        100,
                                        0,
                                        LegacyOnly: legacyScoresOnly
                                    ),
                                    API.OSU.Client.GetUserScores(
                                        data.userInfo.Id,
                                        API.OSU.UserScoreType.Best,
                                        mode!.Value,
                                        100,
                                        100,
                                        LegacyOnly: legacyScoresOnly
                                    )
                                ]
                            );
                            allBP = allBPList.Flatten();
                        }

                        if (allBP is { Length: > 0 })
                            await RecalculateTotalPp(allBP, data.userInfo, calculatorId);
                    }

                    var rendered = await Image
                        .Takumi
                        .InfoV1Takumi
                        .Draw(data, resolved.IsRegistered, isDataOfDayAvaiavle);
                    await target.reply(rendered);
                    break;
                case 2:
                    var v2Options = data.customMode switch
                    {
                        Image.InfoV1.UserPanelData.CustomMode.Custom
                            => Image
                                .OsuInfoPanelV2
                                .InfoCustom
                                .FromCustomTheme(data.CustomTheme, None),
                        Image.InfoV1.UserPanelData.CustomMode.Light
                            => Image.OsuInfoPanelV2.InfoCustom.LightDefault,
                        Image.InfoV1.UserPanelData.CustomMode.Dark
                            => Image.OsuInfoPanelV2.InfoCustom.DarkDefault,
                        _ => throw new ArgumentOutOfRangeException("未知的自定义模式")
                    };

                    if (is_ppysb)
                    {
                        var ss = await API.PPYSB
                            .Client
                            .GetUserScores(
                                data.userInfo.Id,
                                API.PPYSB.UserScoreType.Best,
                                sbmode!.Value,
                                100,
                                0
                            );
                        if (ss is null)
                        {
                            await target.Treply("error.query_scores_failed");
                            return;
                        }
                        allBP = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
                    }
                    else
                    {
                        var allBPList = await Task.WhenAll(
                            [
                                API.OSU.Client.GetUserScores(
                                    data.userInfo.Id,
                                    API.OSU.UserScoreType.Best,
                                    mode!.Value,
                                    100,
                                    0,
                                    LegacyOnly: legacyScoresOnly
                                ),
                                API.OSU.Client.GetUserScores(
                                    data.userInfo.Id,
                                    API.OSU.UserScoreType.Best,
                                    mode!.Value,
                                    100,
                                    100,
                                    LegacyOnly: legacyScoresOnly
                                )
                            ]
                        );
                        allBP = allBPList.Flatten();
                    }

                    if (recalculateTotalPp && allBP is { Length: > 0 })
                    {
                        await RecalculateTotalPp(allBP, data.userInfo, calculatorId);
                    }

                    using (var img = await Image
                        .OsuInfoPanelV2
                        .Draw(
                            data,
                            allBP!,
                            v2Options,
                            resolved.IsRegistered,
                            false,
                            isDataOfDayAvaiavle,
                            false,
                            calculatorId: calculatorId
                        ))
                    {
                        await target.reply(img, new PngEncoder());
                    }

                    break;
                default:
                    return;
            }

            if (Config.inner!.dev)
                return;
            _ = Task.Run(async () =>
            {
                if (is_ppysb)
                    return;
                try
                {
                    if (data.userInfo.Mode == API.OSU.Mode.OSU) //只存std的
                        if (allBP!.Length > 0)
                            await InsertBeatmapTechInfo(allBP, calculatorId);
                        else
                        {
                            allBP = await API.OSU
                                .Client
                                .GetUserScores(
                                    data.userInfo.Id,
                                    API.OSU.UserScoreType.Best,
                                    API.OSU.Mode.OSU,
                                    20,
                                    0
                                );
                            if (allBP!.Length > 0)
                                await InsertBeatmapTechInfo(allBP, calculatorId);
                        }
                }
                catch { }
            });
        }

        public static async Task InsertBeatmapTechInfo(
            API.OSU.Models.ScoreLazer[] allbp,
            string calculatorId
        )
        {
            foreach (var score in allbp)
            {
                //计算pp
                try
                {
                    if (
                        score.Rank.ToUpper() == "XH"
                        || score.Rank.ToUpper() == "X"
                        || score.Rank.ToUpper() == "SH"
                        || score.Rank.ToUpper() == "S"
                        || score.Rank.ToUpper() == "A"
                    )
                    {
                        var data = await UniversalCalculator.CalculatePanelData(score, calculatorId);
                        await Database
                            .Client
                            .InsertOsuStandardBeatmapTechData(
                                score.Beatmap!.BeatmapId,
                                data.performance!.Difficulty.Stars,
                                (int)data.performance.GetForecast(PerformanceForecast.Perfect)!.Total,
                                (int)(data.performance.GetForecast(PerformanceForecast.Perfect)!.Get(PerformanceComponent.Accuracy) ?? 0),
                                (int)(data.performance.GetForecast(PerformanceForecast.Perfect)!.Get(PerformanceComponent.Speed) ?? 0),
                                (int)(data.performance.GetForecast(PerformanceForecast.Perfect)!.Get(PerformanceComponent.Aim) ?? 0),
                                (int)data.performance.GetForecast(PerformanceForecast.Accuracy99)!.Total,
                                (int)data.performance.GetForecast(PerformanceForecast.Accuracy98)!.Total,
                                (int)data.performance.GetForecast(PerformanceForecast.Accuracy97)!.Total,
                                (int)data.performance.GetForecast(PerformanceForecast.Accuracy95)!.Total,
                                data.performance.CalculatorId,
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

        private static async Task RecalculateTotalPp(
            API.OSU.Models.ScoreLazer[] scores,
            API.OSU.Models.UserExtended user,
            string calculatorId
        )
        {
            var original = Utils.CalculateBonusPP(scores, user);
            await Parallel.ForEachAsync(
                scores,
                async (score, _) =>
                {
                    var beatmap = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
                    score.pp = UniversalCalculator
                        .CalculateData(beatmap, score, calculatorId)
                        .Score
                        .Total;
                }
            );
            scores.Sort((left, right) =>
                right.pp.GetValueOrDefault().CompareTo(left.pp.GetValueOrDefault())
            );
            var recalculated = Utils.CalculateBonusPP(scores, user);
            user.Statistics.PP = original.bonusPP + recalculated.scorePP;
        }
    }
}
