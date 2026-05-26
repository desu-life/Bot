using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
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
    public class TodayBpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "todaybp",
                Description = "Show today's best performance",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                    new() { Name = "order_number", Description = "Score list position", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                ],
                Flags = [ new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" }, ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => ExecuteTodayBP(target, cmd);

        public static async Task ExecuteTodayBP(
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

            if (is_ppysb)
            {
                var ss = await API.PPYSB
                    .Client
                    .GetUserScores(
                        osuID,
                        API.PPYSB.UserScoreType.Best,
                        sbmode!.Value,
                        100,
                        0,
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
                        API.OSU.UserScoreType.Best,
                        mode!.Value,
                        100,
                        0,
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
                List<Image.ScoreList.ScoreRank> scores = [ ];
                var now = DateTime.Now;
                var t = now.Hour < 4 ? now.Date.AddDays(-1).AddHours(4) : now.Date.AddHours(4);

                t = t.AddDays(-cmd.Get<int>("order_number"));

                for (int i = 0; i < scoreInfos.Length; i++)
                {
                    var item = scoreInfos[i];
                    var bp_time = item.EndedAt.ToLocalTime();

                    if (bp_time >= t)
                    {
                        scores.Add(new Image.ScoreList.ScoreRank { Score = item, Rank = i + 1 });
                    }
                }

                if (scores.Count == 0)
                {
                    if (cmd.SelfQuery)
                    {
                        await target.Treply("osu.no_todaybp_self", tempOsuInfo.Mode.ToStr());
                    }
                    else
                    {
                        await target.Treply("osu.no_todaybp_other", tempOsuInfo.Username, tempOsuInfo.Mode.ToStr());
                    }
                    return;
                }

                await Parallel.ForEachAsync(
                    scores,
                    async (s, _) =>
                    {
                        var b = await Utils.LoadOrDownloadBeatmap(s.Score.Beatmap!);
                        s.PPInfo = UniversalCalculator.CalculateData(
                            b,
                            s.Score,
                            UniversalCalculator.GetCalculatorKind(is_ppysb, false)
                        );
                    }
                );

                scores.Sort((a, b) => b.PPInfo!.ppStat.total > a.PPInfo!.ppStat.total ? 1 : -1);

                using var img = await KanonBot
                    .Image
                    .ScoreList
                    .Draw(KanonBot.Image.ScoreList.Type.TODAYBP, scores, tempOsuInfo);

                await target.reply(img, new PngEncoder());
            }
            else
            {
                if (cmd.SelfQuery)
                {
                    await target.Treply("osu.no_bp_self", tempOsuInfo.Mode.ToStr());
                }
                else
                {
                    await target.Treply("osu.no_bp_other", tempOsuInfo.Username, tempOsuInfo.Mode.ToStr());
                }
                return;
            }
        }
    }

    public class GetTodayBpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get todaybp",
                Description = "Show an osu! today's best performance",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "order_number", Description = "Score list position", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags = [ new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" } ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => TodayBpCommand.ExecuteTodayBP(target, cmd);
    }
}
