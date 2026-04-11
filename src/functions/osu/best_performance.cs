using System.IO;
using System.Security.Cryptography;
using KanonBot.API;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using RosuPP;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions.OSUBot
{
    public class BestPerformance
    {
        public static async Task Execute(Target target, string cmd)
        {
            #region 验证
            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.BestPerformance);
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

            // 输入检查
            if (command.order_number < 1)
            {
                command.order_number = 1;
            }

            API.OSU.Models.ScoreLazer[]? scores = null;

            
            if (command.special_version_pp && is_ppysb)
            {
                var ss = await API.PPYSB.Client.GetUserScores(
                    osuID,
                API.PPYSB.UserScoreType.Best,
                    sbmode!.Value,
                    1,
                    command.order_number - 1
                );
                scores = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            } else {
                scores = await API.OSU.Client.GetUserScores(
                    osuID,
                    API.OSU.UserScoreType.Best,
                    mode!.Value,
                    1,
                    command.order_number - 1
                );
            }

            if (scores == null)
            {
                await target.reply("查询成绩时出错。");
                return;
            }
            if (scores!.Length > 0)
            {
                var score = scores[0];
                if (score.Beatmap is null) {
                    score.Beatmap = await Client.GetBeatmap(score.BeatmapId);
                    score.Beatmapset = score.Beatmap?.Beatmapset;
                }

                score.User ??= tempOsuInfo;

                Image.ScoreV2.ScorePanelData data;
                data = await UniversalCalculator.CalculatePanelData(score, command.special_version_pp ? (is_ppysb ? CalculatorKind.Sb : CalculatorKind.Old) : CalculatorKind.Unset);
                using var stream = new MemoryStream();

                using var img =
                    command.dev_panel
                        ? await Image.OsuScorePanelV3.Draw(data)
                        : await Image.ScoreV2.DrawScore(data);

                await img.SaveAsync(stream, new JpegEncoder());
                await target.reply(
                    new Chain().image(
                        Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                        ImageSegment.Type.Base64
                    )
                );

                if (is_ppysb) return;
                _ = Task.Run(() => BeatmapTechDataProcess(score, data));
            }
            else
            {
                await target.reply("猫猫找不到该BP。");
                return;
            }
        }

        private static async Task BeatmapTechDataProcess(
            Models.ScoreLazer score,
            Image.ScoreV2.ScorePanelData data
        )
        {
            if (Config.inner!.dev) return;
            if (score.Mode == API.OSU.Mode.OSU)
            {
                if (
                    score.Beatmap!.Status == API.OSU.Models.Status.Ranked
                    || score.Beatmap!.Status == API.OSU.Models.Status.Approved
                )
                {
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
                        score.Mods.Map(m => m.Acronym).ToArray()
                    );
                }
            }
        }
    }
}
