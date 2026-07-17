using System.IO;
using System.Security.Cryptography;
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
using RosuPP;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions
{
    public class BpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "bp",
                Description = "Show an osu! best performance score",
                LegacyStartsWithMatch = true,
                ExcludePrefixes =  [ "bpa", "bpme", "bplist" ],
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                    new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "pp_version", LegacyName = "pp", Description = "PP algorithm version", Prefix = ArgPrefix.And },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use osu! current PP (legacy alias)", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                    new() { Name = "dev_panel", Description = "Use the Dev panel", Value = "dev", SlashName = "is_dev" },
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

            // 验证osu信息
            var (tempOsuInfo, sbinfo) = await Utils.ResolveOsuUser(resolved);
            if (tempOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }

            #endregion

            // 输入检查
            var orderNumber = cmd.Get<int>("order_number");
            if (orderNumber < 1)
                orderNumber = 1;
            bool dev_panel = cmd.Flag("dev_panel");

            API.OSU.Models.ScoreLazer[]? scores = null;

            if (is_ppysb)
            {
                var ss = await API.PPYSB
                    .Client
                    .GetUserScores(
                        osuID,
                        API.PPYSB.UserScoreType.Best,
                        sbmode!.Value,
                        1,
                        orderNumber - 1
                    );
                scores = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            }
            else
            {
                scores = await API.OSU
                    .Client
                    .GetUserScores(
                        osuID,
                        API.OSU.UserScoreType.Best,
                        mode!.Value,
                        1,
                        orderNumber - 1
                    );
            }

            if (scores == null)
            {
                await target.Treply("error.query_scores_failed");
                return;
            }
            if (scores!.Length > 0)
            {
                var score = scores[0];
                if (score.Beatmap is null)
                {
                    score.Beatmap = await Client.GetBeatmap(score.BeatmapId);
                    score.Beatmapset = score.Beatmap?.Beatmapset;
                }

                score.User ??= tempOsuInfo;

                var calculatorId = await PerformanceCommandOptions.Resolve(
                    target,
                    cmd,
                    score.Mode,
                    is_ppysb
                );
                if (calculatorId is null)
                    return;

                Image.ScorePanelData data;
                data = await UniversalCalculator.CalculatePanelData(score, calculatorId);

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
                HistoryBeatmapMapper.Map(target.source, score.BeatmapId);

                if (is_ppysb)
                    return;
                _ = Task.Run(() => BeatmapTechDataProcess(score, data));
            }
            else
            {
                await target.Treply("osu.bp_not_found");
                return;
            }
        }

        private static async Task BeatmapTechDataProcess(
            Models.ScoreLazer score,
            Image.ScorePanelData data
        )
        {
            if (Config.inner!.dev)
                return;
            if (score.Mode == API.OSU.Mode.OSU)
            {
                if (
                    score.Beatmap!.Status == API.OSU.Models.Status.Ranked
                    || score.Beatmap!.Status == API.OSU.Models.Status.Approved
                )
                {
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
                            score.Mods.Map(m => m.Acronym).ToArray()
                        );
                }
            }
        }
    }
}
