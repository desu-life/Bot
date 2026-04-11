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
    public class Recent
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
                    command.order_number - 1,
                    includeFails
                );
                scoreInfos = ss?.Map(s => s.ToOsu(sbinfo!, sbmode!.Value)).ToArray();
            } else {
                scoreInfos = await API.OSU.Client.GetUserScores(
                    osuID,
                    API.OSU.UserScoreType.Recent,
                    mode!.Value,
                    20, //default was 1, due to seasonalpass set it to 20
                    command.order_number - 1,
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
                Image.ScoreV2.ScorePanelData data;
                data = await UniversalCalculator.CalculatePanelData(scoreInfos[0], command.special_version_pp ? (is_ppysb ? CalculatorKind.Sb : CalculatorKind.Old) : CalculatorKind.Unset);
                
                
                using var img =
                    command.dev_panel
                        ? await Image.OsuScorePanelV3.Draw(data)
                        : await Image.ScoreV2.DrawScore(data);

                await target.reply(img, new JpegEncoder());

                if (is_ppysb) return;
                _ = Task.Run(() => BeatmapTechDataProcess(scoreInfos, osuID));
            }
            else
            {
                await target.reply("猫猫找不到该玩家最近游玩的成绩。");
                return;
            }
        }

        private static async Task BeatmapTechDataProcess(Models.ScoreLazer[] scoreInfos, long? oid)
        {
            if (Config.inner!.dev) return;
            foreach (var x in scoreInfos)
            {
                //处理谱面数据
                if (x.Rank.ToUpper() != "F")
                {
                    //计算pp数据
                    var data = await UniversalCalculator.CalculatePanelData(x);

                    //季票信息
                    if (oid is not null)
                    {
                        bool temp_abletoinsert = true;
                        foreach (var c in x.Mods)
                        {
                            if (c.Acronym.ToUpper() == "AP")
                                temp_abletoinsert = false;
                            if (c.Acronym.ToUpper() == "RX")
                                temp_abletoinsert = false;
                        }
                        if (temp_abletoinsert)
                            await Seasonalpass.Update(oid.Value, data);
                    }
                    //std推图
                    if (x.Mode == API.OSU.Mode.OSU)
                    {
                        if (
                            x.Beatmap!.Status == API.OSU.Models.Status.Ranked
                            || x.Beatmap!.Status == API.OSU.Models.Status.Approved
                        )
                            if (
                                x.Rank.ToUpper() == "XH"
                                || x.Rank.ToUpper() == "X"
                                || x.Rank.ToUpper() == "SH"
                                || x.Rank.ToUpper() == "S"
                                || x.Rank.ToUpper() == "A"
                            )
                            {
                                await Database.Client.InsertOsuStandardBeatmapTechData(
                                    x.Beatmap!.BeatmapId,
                                    data.ppInfo.star,
                                    (int)data.ppInfo.ppStats![0].total,
                                    (int)data.ppInfo.ppStats![0].acc!,
                                    (int)data.ppInfo.ppStats![0].speed!,
                                    (int)data.ppInfo.ppStats![0].aim!,
                                    (int)data.ppInfo.ppStats![1].total,
                                    (int)data.ppInfo.ppStats![2].total,
                                    (int)data.ppInfo.ppStats![3].total,
                                    (int)data.ppInfo.ppStats![4].total,
                                    x.Mods.Map(c => c.Acronym).ToArray()
                                );
                            }
                    }
                }
            }
        }
    }
}
