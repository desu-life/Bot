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
                scoreInfos = await API.OSU.Client.GetUserScores(
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
                await target.reply("查询成绩时出错。");
                return;
            }
            // 正常是找不到玩家，但是上面有验证，这里做保险
            if (scoreInfos.Length > 0)
            {
                List<Image.ScoreList.ScoreRank> scores = [];
                var now = DateTime.Now;
                var t = now.Hour < 4 ? now.Date.AddDays(-1).AddHours(4) : now.Date.AddHours(4);

                t = t.AddDays(-command.order_number);

                for (int i = 0; i < scoreInfos.Length; i++)
                {
                    var item = scoreInfos[i];
                    var bp_time = item.EndedAt.ToLocalTime();

                    if (bp_time >= t)
                    {
                        scores.Add(new Image.ScoreList.ScoreRank {
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
                    s.PPInfo = UniversalCalculator.CalculateData(b, s.Score, command.special_version_pp ? (is_ppysb ? CalculatorKind.Sb : CalculatorKind.Old) : CalculatorKind.Unset);
                });
                
                scores.Sort((a, b) => b.PPInfo!.ppStat.total > a.PPInfo!.ppStat.total ? 1 : -1);

                using var img = await KanonBot.Image.ScoreList.Draw(
                    KanonBot.Image.ScoreList.Type.TODAYBP,
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
    