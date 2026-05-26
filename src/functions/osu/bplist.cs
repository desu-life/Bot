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
    public class BpListCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "bplist",
                Description = "Show an osu! best performance list",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                    new() { Name = "range", Description = "Score rank range", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseRange(s) },
                    new() { Name = "range", Description = "Score rank range", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseRange(s) },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use special pp panel", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" }
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => BPList.Execute(target, cmd);
    }

    public class BPList
    {
        public static async Task Execute(
            Target target,
            ParsedCommand cmd,
            bool includeFails = false
        )
        {
            if (!cmd.Has("range"))
            {
                await target.Treply("osu.invalid_range");
                return;
            }
            var range = cmd.Get<Range>("range");
            int StartAt,
                EndAt;
            StartAt = range.Start.Value;
            EndAt = range.End.Value;
            if (StartAt == 0)
                StartAt = 1; // ParseRange returns Range(0, single) for single value

            if (StartAt < 1 || StartAt > 199)
            {
                await target.Treply("osu.invalid_range");
                return;
            }
            if (EndAt < 2 || EndAt > 200)
            {
                await target.Treply("osu.invalid_range");
                return;
            }
            if (EndAt < StartAt)
            {
                await target.Treply("osu.invalid_range");
                return;
            }

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
                    .GetUserScoresPage(
                        osuID,
                        API.OSU.UserScoreType.Best,
                        mode!.Value,
                        200,
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
                List<Image.ScoreList.ScoreRank> scores =  [ ];
                for (
                    int i = StartAt - 1;
                    i < (scoreInfos.Length > EndAt ? EndAt : scoreInfos.Length);
                    ++i
                )
                {
                    scores.Add(
                        new Image.ScoreList.ScoreRank { Score = scoreInfos[i], Rank = i + 1, }
                    );
                }

                if (scores.Count == 0)
                {
                    await target.Treply("osu.no_bp_scores");
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
                            UniversalCalculator.GetCalculatorKind(is_ppysb, cmd.Flag("special_pp"))
                        );
                    }
                );

                scores.Sort((a, b) => b.PPInfo!.ppStat.total > a.PPInfo!.ppStat.total ? 1 : -1);

                using var img = await KanonBot
                    .Image
                    .ScoreList
                    .Draw(KanonBot.Image.ScoreList.Type.BPLIST, scores, tempOsuInfo);

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
}
