using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;
using KanonBot.OsuPerformance;

namespace KanonBot.Functions
{
    public class GetBphtCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get bpht",
                Description = "Show best performance hit table data",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
new() { Name = "pp_version", LegacyName = "pp", Description = "PP algorithm version", Prefix = ArgPrefix.And },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use osu! current PP and legacy scores (legacy alias)", Value = "", SlashName = "is_special_pp" },
                    new()
                    {
                        Name = "legacy_scores",
                        Description = "Fetch only legacy osu! scores",
                        Value = "legacy",
                        SlashName = "is_legacy_scores",
                    },
                    new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;
            bool special_version_pp = cmd.Flag("special_pp");
            bool legacyScoresOnly = special_version_pp || cmd.Flag("legacy_scores");

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID, mode!.Value);
            if (OnlineOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;

            var allBP = await API.OSU
                .Client
                .GetUserScores(
                    OnlineOsuInfo!.Id,
                    API.OSU.UserScoreType.Best,
                    mode!.Value,
                    100,
                    0,
                    LegacyOnly: legacyScoresOnly
                );
            if (allBP == null)
            {
                await target.Treply("error.query_scores_failed");
                return;
            }

            if (special_version_pp || cmd.Has("pp_version"))
            {
                var calculatorId = await PerformanceCommandOptions.Resolve(
                    target,
                    cmd,
                    mode.Value,
                    false
                );
                if (calculatorId is null)
                    return;

                await Parallel.ForEachAsync(
                    allBP,
                    async (score, _) =>
                    {
                        var beatmap = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
                        score.pp = UniversalCalculator
                            .CalculateData(beatmap, score, calculatorId)
                            .Score
                            .Total;
                    }
                );
                allBP.Sort((left, right) => right.pp.GetValueOrDefault().CompareTo(left.pp.GetValueOrDefault()));
            }
            double totalPP = 0;
            // 如果bp数量小于10则取消
            if (allBP!.Length < 10)
            {
                if (cmd.SelfQuery)
                    await target.Treply("osu.bp_too_few_self");
                else
                    await target.Treply("osu.bp_too_few_other", OnlineOsuInfo.Username);
                return;
            }
            foreach (var item in allBP)
            {
                totalPP += item.pp ?? 0.0;
            }
            var last = allBP.Length;
            var str =
                $"{OnlineOsuInfo.Username} 在 {OnlineOsuInfo.Mode.ToStr()} 模式中:"
                + $"\n你的 bp1 有 {allBP[0].pp:0.##}pp"
                + $"\n你的 bp2 有 {allBP[1].pp:0.##}pp"
                + $"\n..."
                + $"\n你的 bp{last - 1} 有 {allBP[last - 2].pp:0.##}pp"
                + $"\n你的 bp{last} 有 {allBP[last - 1].pp:0.##}pp"
                + $"\n你 bp1 与 bp{last} 相差了有 {allBP[0].pp - allBP[last - 1].pp:0.##}pp"
                + $"\n你的 bp 榜上所有成绩的平均值为 {totalPP / allBP.Length:0.##}pp";
            await target.reply(str);
        }
    }
}
