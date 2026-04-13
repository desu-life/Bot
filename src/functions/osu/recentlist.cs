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
    public class RecentList
    {
        public static async Task Execute(Target target, string cmd, bool includeFails = false)
        {
            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Recent);
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
                API.PPYSB.UserScoreType.Recent,
                    sbmode!.Value,
                    20,
                    0,
                    includeFails
                );
                scoreInfos = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            } else {
                scoreInfos = await API.OSU.Client.GetUserScores(
                    osuID,
                    API.OSU.UserScoreType.Recent,
                    mode!.Value,
                    20, //default was 1, due to seasonalpass set it to 20
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
                for (int i = 0; i < scoreInfos.Length; ++i) {
                    scores.Add(new Image.ScoreList.ScoreRank {
                        Score = scoreInfos[i],
                        Rank = i + 1,
                    });
                }

                await Parallel.ForEachAsync(scores, async (s, _) => {
                    var b = await Utils.LoadOrDownloadBeatmap(s.Score.Beatmap!);
                    s.PPInfo = UniversalCalculator.CalculateData(b, s.Score, UniversalCalculator.GetCalculatorKind(is_ppysb, command.special_version_pp));
                });

                using var img = await KanonBot.Image.ScoreList.Draw(
                    KanonBot.Image.ScoreList.Type.RECENTLIST,
                    scores,
                    tempOsuInfo
                );

                await target.reply(img, new PngEncoder());
            }
            else
            {
                await target.reply("猫猫找不到该玩家最近游玩的成绩。");
                return;
            }
        }

    }
}
