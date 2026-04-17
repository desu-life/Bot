using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using KanonBot.Functions.OSU;
using System.IO;
using LanguageExt.UnsafeValueAccess;
using KanonBot.API.OSU;
using KanonBot.OsuPerformance;

namespace KanonBot.Functions.OSUBot
{
    public class Score
    {
        async public static Task Execute(Target target, string cmd, bool ppFirst = false, bool fetch_source = false)
        {
            #region 验证
            // 解析指令
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Score);
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

            #endregion

            // 解析Mod
            List<string> mods = new();
            try
            {
                mods = Enumerable
                    .Range(0, command.osu_mods.Length / 2)
                    .Select(p => new string(command.osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper())
                    .ToList();
            }
            catch { }

            // 判断是否给定了bid
            if (command.order_number == -1)
            {
                if (!fetch_source)
                {
                    await target.reply("请提供谱面bid。");
                    return;
                }

                var lastBeatmapId = HistoryBeatmapMapper.Get(target.source);
                if (lastBeatmapId != null)
                {
                    command.order_number = (int)lastBeatmapId;
                }
            }

            API.OSU.Models.ScoreLazer? scoreData = null;

            if (is_ppysb) {
                // config mods
                if (mods.Find(x => x == "RX") != null) {
                    sbmode = sbmode?.ToRx();
                    if (mods.Count == 1) { mods = []; }
                }

                if (mods.Find(x => x == "AP") != null) {
                    sbmode = sbmode?.ToAp();
                    if (mods.Count == 1) { mods = []; }
                }

                using var rmods = RosuPP.Mods.FromAcronyms(string.Concat(mods), sbmode!.Value.ToOsu().ToRosu());
                var tmpScore = await API.PPYSB.Client.GetMapScore(
                    userId: osuID,
                    command.order_number,
                    sbmode.Value,
                    rmods.Bits(),
                    ppFirst
                );

                if (tmpScore is not null) {
                    scoreData = tmpScore.ToOsu(sbinfo!, sbmode!.Value);
                }
            } else {
                var scoreDatas = await API.OSU.Client.GetUserBeatmapScores(
                    osuID,
                    command.order_number,
                    mode!.Value
                );
                if (ppFirst) {
                    if (mods.Count > 0) {
                        scoreData = Utils.FilterMods(scoreDatas, mods)?.OrderByDescending(s => s.pp).FirstOrDefault();
                    } else {
                        scoreData = scoreDatas?.OrderByDescending(s => s.pp).FirstOrDefault();
                    }
                } else {
                    if (scoreDatas != null && scoreDatas.All(s => s.IsClassic)) {
                        if (mods.Count > 0) {
                            scoreData = Utils.FilterMods(scoreDatas, mods)?.OrderByDescending(s => s.ScoreAuto).FirstOrDefault();
                        } else {
                            scoreData = scoreDatas?.OrderByDescending(s => s.ScoreAuto).FirstOrDefault();
                        }
                    } else {
                        scoreData = (await API.OSU.Client.GetUserBeatmapScore(
                            osuID,
                            command.order_number,
                            mods,
                            mode!.Value
                        ) ?? await API.OSU.Client.GetUserBeatmapScore(
                            osuID,
                            command.order_number,
                            mods,
                            mode!.Value,
                            true
                        ))?.Score;
                    }
                }
            }

            if (scoreData == null)
            {
                if (command.self_query)
                    await target.reply("猫猫没有找到你的成绩");
                else
                    await target.reply("猫猫没有找到TA的成绩");
                return;
            }
            //ppy的getscore api不会返回beatmapsets信息，需要手动获取
            if (scoreData.Beatmapset is null) {
                var beatmapInfo = await API.OSU.Client.GetBeatmap(scoreData.BeatmapId);
                scoreData.Beatmap = beatmapInfo;
                scoreData.Beatmapset = beatmapInfo!.Beatmapset;
            }

            if (scoreData.User is null) {
                scoreData.User = tempOsuInfo;
            }

            Image.ScoreV2.ScorePanelData data;
            data = await UniversalCalculator.CalculatePanelData(scoreData, UniversalCalculator.GetCalculatorKind(is_ppysb, command.special_version_pp));
            
            using var img = await Image.ScoreV2.DrawScore(data);
            await target.reply(img, new JpegEncoder());

            // 缓存本来源查询
            HistoryBeatmapMapper.Map(target.source, scoreData.BeatmapId);
        }
    }
}
