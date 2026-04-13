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
    public class BPList
    {
        public static async Task Execute(Target target, string cmd, bool includeFails = false)
        {
            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.BPList);

            if (command.StartAt.IsNone)
            {
                await target.reply("指定的范围不正确");
                return;
            }
            if (command.EndAt.IsNone)
            {
                await target.reply("指定的范围不正确");
                return;
            }
            int StartAt = command.StartAt.Value();
            int EndAt = command.EndAt.Value();

            if (StartAt < 1 || StartAt > 199)
            {
                await target.reply("指定的范围不正确");
                return;
            }
            if (EndAt < 2 || EndAt > 200)
            {
                await target.reply("指定的范围不正确");
                return;
            }
            if (EndAt < StartAt)
            {
                await target.reply("指定的范围不正确");
                return;
            }

            var resolved = await Accounts.ResolveCommandUser(target, command);
            if (resolved == null) return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;
            API.PPYSB.Mode? sbmode = resolved.SbMode;
            bool is_ppysb = resolved.IsPpysb;

            // 验证osu信息
            var (tempOsuInfo, sbinfo) = await Utils.ResolveOsuUser(resolved);
            if (tempOsuInfo == null)
            {
                await target.reply("猫猫没有找到此用户。");
                return;
            }

            API.OSU.Models.ScoreLazer[]? scoreInfos = null;

            if (is_ppysb) {
                var ss = await API.PPYSB.Client.GetUserScores(
                    osuID,
                    API.PPYSB.UserScoreType.Best,
                    sbmode!.Value,
                    100,
                    0,
                    includeFails
                );
                scoreInfos = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            } else {
                scoreInfos = await API.OSU.Client.GetUserScoresPage(
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
                await target.reply("查询成绩时出错。");
                return;
            }
            // 正常是找不到玩家，但是上面有验证，这里做保险
            if (scoreInfos.Length > 0)
            {
                List<Image.ScoreList.ScoreRank> scores = [];
                for (int i = StartAt - 1; i < (scoreInfos.Length > EndAt ? EndAt : scoreInfos.Length); ++i) {
                    scores.Add(new Image.ScoreList.ScoreRank {
                        Score = scoreInfos[i],
                        Rank = i + 1,
                    });
                }

                if (scores.Count == 0) {
                    await target.reply($"找不到对应的成绩。。");
                    return;
                }

                await Parallel.ForEachAsync(scores, async (s, _) => {
                    var b = await Utils.LoadOrDownloadBeatmap(s.Score.Beatmap!);
                    s.PPInfo = UniversalCalculator.CalculateData(b, s.Score, UniversalCalculator.GetCalculatorKind(is_ppysb, command.special_version_pp));
                });

                scores.Sort((a, b) => b.PPInfo!.ppStat.total > a.PPInfo!.ppStat.total ? 1 : -1);

                using var img = await KanonBot.Image.ScoreList.Draw(
                    KanonBot.Image.ScoreList.Type.BPLIST,
                    scores,
                    tempOsuInfo
                );

                await target.reply(img, new PngEncoder());
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
