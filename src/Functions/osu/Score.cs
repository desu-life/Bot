using System.IO;
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
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions
{
    public class ScoreCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "score",
                Description = "Show a user's score on a beatmap (sorted by score)",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "osu_mods", Description = "osu! mods", Prefix = ArgPrefix.Plus },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Alternative pp calculater", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Score.Execute(target, cmd, ppFirst: false, fetch_source: true);
    }

    public class PpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "pp",
                Description = "Show a user's score on a beatmap (sorted by pp)",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "osu_mods", Description = "osu! mods", Prefix = ArgPrefix.Plus },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Alternative pp calculater", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Score.Execute(target, cmd, ppFirst: true, fetch_source: true);
    }

    public class Score
    {
        public static async Task Execute(
            Target target,
            ParsedCommand cmd,
            bool ppFirst = false,
            bool fetch_source = false
        )
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

            // 解析Mod
            var osu_mods = cmd.GetString("osu_mods") ?? "";
            List<string> mods = new();
            try
            {
                mods = Enumerable
                    .Range(0, osu_mods.Length / 2)
                    .Select(p => new string(osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper())
                    .ToList();
            }
            catch { }

            // 判断是否给定了bid
            var bid = cmd.Get<int>("bid");
            if (bid == 0)
                bid = -1;
            if (bid == -1)
            {
                if (!fetch_source)
                {
                    await target.Treply("osu.provide_bid");
                    return;
                }

                var lastBeatmapId = HistoryBeatmapMapper.Get(target.source);
                if (lastBeatmapId != null)
                {
                    bid = (int)lastBeatmapId;
                }
            }

            API.OSU.Models.ScoreLazer? scoreData = null;

            if (is_ppysb)
            {
                // config mods
                if (mods.Find(x => x == "RX") != null)
                {
                    sbmode = sbmode?.ToRx();
                    if (mods.Count == 1)
                    {
                        mods = [ ];
                    }
                }

                if (mods.Find(x => x == "AP") != null)
                {
                    sbmode = sbmode?.ToAp();
                    if (mods.Count == 1)
                    {
                        mods = [ ];
                    }
                }

                using var rmods = RosuPP
                    .Mods
                    .FromAcronyms(string.Concat(mods), sbmode!.Value.ToOsu().ToRosu());
                var tmpScore = await API.PPYSB
                    .Client
                    .GetMapScore(userId: osuID, bid, sbmode.Value, rmods.Bits(), ppFirst);

                if (tmpScore is not null)
                {
                    scoreData = tmpScore.ToOsu(sbinfo!, sbmode!.Value);
                }
            }
            else
            {
                var scoreDatas = await API.OSU.Client.GetUserBeatmapScores(osuID, bid, mode!.Value);
                if (ppFirst)
                {
                    if (mods.Count > 0)
                    {
                        scoreData = Utils
                            .FilterMods(scoreDatas, mods)
                            ?.OrderByDescending(s => s.pp)
                            .FirstOrDefault();
                    }
                    else
                    {
                        scoreData = scoreDatas?.OrderByDescending(s => s.pp).FirstOrDefault();
                    }
                }
                else
                {
                    if (scoreDatas != null && scoreDatas.All(s => s.IsClassic))
                    {
                        if (mods.Count > 0)
                        {
                            scoreData = Utils
                                .FilterMods(scoreDatas, mods)
                                ?.OrderByDescending(s => s.ScoreAuto)
                                .FirstOrDefault();
                        }
                        else
                        {
                            scoreData = scoreDatas
                                ?.OrderByDescending(s => s.ScoreAuto)
                                .FirstOrDefault();
                        }
                    }
                    else
                    {
                        scoreData = (
                            await API.OSU.Client.GetUserBeatmapScore(osuID, bid, mods, mode!.Value)
                            ?? await API.OSU
                                .Client
                                .GetUserBeatmapScore(osuID, bid, mods, mode!.Value, true)
                        )?.Score;
                    }
                }
            }

            if (scoreData == null)
            {
                if (cmd.SelfQuery)
                    await target.Treply("osu.score_not_found_self");
                else
                    await target.Treply("osu.score_not_found_other");
                return;
            }
            //ppy的getscore api不会返回beatmapsets信息，需要手动获取
            if (scoreData.Beatmapset is null)
            {
                var beatmapInfo = await API.OSU.Client.GetBeatmap(scoreData.BeatmapId);
                scoreData.Beatmap = beatmapInfo;
                scoreData.Beatmapset = beatmapInfo!.Beatmapset;
            }

            if (scoreData.User is null)
            {
                scoreData.User = tempOsuInfo;
            }

            bool special_version_pp = cmd.Flag("special_pp");
            Image.ScorePanelData data;
            data = await UniversalCalculator.CalculatePanelData(
                scoreData,
                UniversalCalculator.GetCalculatorKind(is_ppysb, special_version_pp)
            );

            var img = await Image.Takumi.ScoreV2.DrawScore(data);
            await target.reply(img);

            // 缓存本来源查询
            HistoryBeatmapMapper.Map(target.source, scoreData.BeatmapId);
        }
    }
}
