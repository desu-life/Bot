using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions
{
    public class RecentCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "recent",
                Description = "Show a user's most recent osu! play",
                Aliases = [ "re" ],
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "pp_version", LegacyName = "pp", Description = "PP algorithm version", Prefix = ArgPrefix.And },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use osu! current PP (legacy alias)", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "dev_panel", Description = "Use the Dev panel", Value = "dev", SlashName = "is_dev" },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Recent.Execute(target, cmd, includeFails: true);
    }

    public class PassRecentCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "pr",
                Description = "Show a user's most recent passed osu! play",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "pp_version", LegacyName = "pp", Description = "PP algorithm version", Prefix = ArgPrefix.And },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use osu! current PP (legacy alias)", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "dev_panel", Description = "Use the Dev panel", Value = "dev", SlashName = "is_dev" },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Recent.Execute(target, cmd, includeFails: false);
    }

    public class Recent
    {
        public static async Task Execute(
            Target target,
            ParsedCommand cmd,
            bool includeFails = false
        )
        {
            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;
            API.PPYSB.Mode? sbmode = resolved.SbMode;
            bool is_ppysb = resolved.IsPpysb;

            // 验证osu信息
            var (tempOsuInfo, sbinfo) = await Utils.ResolveOsuUser(resolved);
            if (tempOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }

            API.OSU.Models.ScoreLazer[]? scoreInfos = null;
            var orderNumber = cmd.Get<int>("order_number");
            if (orderNumber < 1)
                orderNumber = 1;
            bool dev_panel = cmd.Flag("dev_panel");

            if (is_ppysb)
            {
                var ss = await API.PPYSB
                    .Client
                    .GetUserScores(
                        osuID,
                        API.PPYSB.UserScoreType.Recent,
                        sbmode!.Value,
                        20,
                        orderNumber - 1,
                        includeFails
                    );
                scoreInfos = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            }
            else
            {
                scoreInfos = await API.OSU
                    .Client
                    .GetUserScores(
                        osuID,
                        API.OSU.UserScoreType.Recent,
                        mode!.Value,
                        20, //default was 1, due to seasonalpass set it to 20
                        orderNumber - 1,
                        includeFails
                    );
            }

            if (scoreInfos == null)
            {
                await target.Treply("error.query_scores_failed");
                return;
            }
            // 正常是找不到玩家，但是上面有验证，这里做保险
            if (scoreInfos.Length > 0)
            {
                var calculatorId = await PerformanceCommandOptions.Resolve(
                    target,
                    cmd,
                    scoreInfos[0].Mode,
                    is_ppysb
                );
                if (calculatorId is null)
                    return;

                Image.ScorePanelData data;
                data = await UniversalCalculator.CalculatePanelData(scoreInfos[0], calculatorId);

                if (dev_panel)
                {
                    using var img = await Image.OsuScorePanelV3.Draw(data);
                    await target.reply(img, new JpegEncoder());
                }
                else
                {
                    var img = await Image.Takumi.ScoreV2.DrawScore(data);
                    await target.reply(img);
                }

                // 缓存本来源查询
                HistoryBeatmapMapper.Map(target.source, scoreInfos[0].BeatmapId);

                if (is_ppysb)
                    return;
                _ = Task.Run(async () =>
                {
                    try { await BeatmapTechDataProcess(scoreInfos, osuID, calculatorId); }
                    catch (Exception ex) { Log.Error(ex, "BeatmapTechDataProcess failed"); }
                });
            }
            else
            {
                await target.Treply("osu.recent_not_found");
                return;
            }
        }

        private static async Task BeatmapTechDataProcess(
            Models.ScoreLazer[] scoreInfos,
            long? oid,
            string calculatorId
        )
        {
            if (Config.inner!.dev)
                return;
            foreach (var x in scoreInfos)
            {
                //处理谱面数据
                if (x.Rank.ToUpper() != "F")
                {
                    //计算pp数据
                    var data = await UniversalCalculator.CalculatePanelData(x, calculatorId);

                    //季票信息
                    if (oid is not null)
                    {
                        bool temp_abletoinsert = !x.Mods.Any(c =>
                            c.Acronym.Equals("AP", StringComparison.OrdinalIgnoreCase) ||
                            c.Acronym.Equals("RX", StringComparison.OrdinalIgnoreCase));
                        if (temp_abletoinsert)
                        {
                            var seasonalData = calculatorId == PerformanceCalculatorIds.RosuCurrent
                                ? data
                                : await UniversalCalculator.CalculatePanelData(
                                    x,
                                    PerformanceCalculatorIds.RosuCurrent
                                );
                            await SeasonalPass.Update(oid.Value, seasonalData);
                        }
                    }
                    //std推图
                    if (x.Mode == API.OSU.Mode.OSU)
                    {
                        if (
                            x.Beatmap!.Status is API.OSU.Models.Status.Ranked
                                or API.OSU.Models.Status.Approved
                        )
                            if (x.Rank.ToUpperInvariant() is "XH" or "X" or "SH" or "S" or "A")
                            {
                                await Database
                                    .Client
                                    .InsertOsuStandardBeatmapTechData(
                                        x.Beatmap!.BeatmapId,
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
                                        x.Mods.Map(c => c.Acronym).ToArray()
                                    );
                            }
                    }
                }
            }
        }
    }
}
