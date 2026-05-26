using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;
using KanonBot.OsuPerformance;

namespace KanonBot.Functions
{
    public class GetRecommendCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get recommend",
                Description = "Recommend osu! beatmaps",
                Args =
                [
                    new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
                    new() { Name = "osu_mods", Description = "osu! mods", Prefix = ArgPrefix.Plus },
                ],
                Flags = [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            int normal_range = 20;
            int NFEZHT_range = 60;
            //only osu!standard
            var resolved = await Accounts.ResolveCommandUser(target, cmd);
            if (resolved == null)
                return;

            long osuID = resolved.OsuId;
            API.OSU.Mode? mode = resolved.Mode;

            // 验证osu信息
            var OnlineOsuInfo = await API.OSU.Client.GetUser(osuID, API.OSU.Mode.OSU); //取osu模式的值
            if (OnlineOsuInfo == null)
            {
                await target.Treply("error.user_not_found");
                return;
            }
            OnlineOsuInfo.Mode = mode!.Value;

            //获取前20bp
            var allBP = await API.OSU
                .Client
                .GetUserScoresLeagcy(
                    OnlineOsuInfo.Id,
                    API.OSU.UserScoreType.Best,
                    API.OSU.Mode.OSU,
                    20,
                    0
                );
            if (allBP == null || allBP.Length < 20)
            {
                await target.Treply("osu.too_few_plays");
                return;
            }

            //从数据库获取相似的谱面
            var randBP = allBP![new Random().Next(0, 19)];
            //get stars from rosupp
            var ppinfo = await UniversalCalculator.CalculatePanelData(randBP);

            var data = new List<Database.Model.OsuStandardBeatmapTechData>();

            //解析mod
            List<string> mods = new();
            var osu_mods = cmd.GetString("osu_mods") ?? "";
            try
            {
                osu_mods = osu_mods.ToLower().Trim();
                mods = Enumerable
                    .Range(0, osu_mods.Length / 2)
                    .Select(p => new string(osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper())
                    .ToList<string>();
            }
            catch { }

            if (mods.Count == 0)
            {
                //使用bp mod
                mods = randBP.Mods.ToList();

                bool isDiffReductionMod = false,
                    ez = false,
                    ht = false,
                    nf = false,
                    td = false,
                    so = false,
                    dt = false;
                foreach (var x in mods)
                {
                    var xx = x.ToLower().Trim();
                    if (xx == "nf") { isDiffReductionMod = true; nf = true; }
                    if (xx == "ht") { isDiffReductionMod = true; ht = true; }
                    if (xx == "ez") { isDiffReductionMod = true; ez = true; }
                    if (xx == "td") { isDiffReductionMod = true; td = true; }
                    if (xx == "so") { isDiffReductionMod = true; so = true; }
                    if (xx == "dt" || xx == "nc") dt = true;
                }
                data = await Database
                    .Client
                    .GetOsuStandardBeatmapTechData(
                        (int)ppinfo.ppInfo!.ppStat.aim!,
                        (int)ppinfo.ppInfo.ppStat.speed!,
                        (int)ppinfo.ppInfo.ppStat.acc!,
                        isDiffReductionMod ? NFEZHT_range : normal_range,
                        dt
                    );
                if (data.Count > 0)
                {
                    if (mods.Count == 0)
                    {
                        data.RemoveAll(x => x.mod != "");
                    }
                    else
                    {
                        for (int i = 0; i < mods.Count; i++)
                            if (mods[i].ToUpper() == "NC")
                                mods[i] = "DT";
                        foreach (var xx in mods)
                            data.RemoveAll(x => !x.mod!.Contains(xx));
                        if (!ez) data.RemoveAll(x => x.mod!.IndexOf("EZ") != -1);
                        if (!nf) data.RemoveAll(x => x.mod!.IndexOf("NF") != -1);
                        if (!ht) data.RemoveAll(x => x.mod!.IndexOf("HT") != -1);
                        if (!td) data.RemoveAll(x => x.mod!.IndexOf("TD") != -1);
                        if (!so) data.RemoveAll(x => x.mod!.IndexOf("SO") != -1);
                    }
                }
                else
                {
                    await target.Treply("osu.no_recommendation");
                    return;
                }
            }
            else
            {
                bool isDiffReductionMod = false,
                    ez = false,
                    ht = false,
                    nf = false,
                    td = false,
                    so = false,
                    dt = false;
                foreach (var x in mods)
                {
                    var xx = x.ToLower().Trim();
                    if (xx == "nf") { isDiffReductionMod = true; nf = true; }
                    if (xx == "ht") { isDiffReductionMod = true; ht = true; }
                    if (xx == "ez") { isDiffReductionMod = true; ez = true; }
                    if (xx == "td") { isDiffReductionMod = true; td = true; }
                    if (xx == "so") { isDiffReductionMod = true; so = true; }
                    if (xx == "dt" || xx == "nc") dt = true;
                }
                //使用解析到的mod 如果是EZ/HT 需要适当把pprange放宽
                data = await Database
                    .Client
                    .GetOsuStandardBeatmapTechData(
                        (int)ppinfo.ppInfo!.ppStat.aim!,
                        (int)ppinfo.ppInfo.ppStat.speed!,
                        (int)ppinfo.ppInfo.ppStat.acc!,
                        isDiffReductionMod ? NFEZHT_range : normal_range,
                        dt
                    );

                if (data.Count > 0)
                {
                    for (int i = 0; i < mods.Count; i++)
                        if (mods[i] == "NC")
                            mods[i] = "DT";
                    foreach (var xx in mods)
                        data.RemoveAll(x => !x.mod!.Contains(xx));
                    if (!ez) data.RemoveAll(x => x.mod!.IndexOf("EZ") != -1);
                    if (!nf) data.RemoveAll(x => x.mod!.IndexOf("NF") != -1);
                    if (!ht) data.RemoveAll(x => x.mod!.IndexOf("HT") != -1);
                    if (!td) data.RemoveAll(x => x.mod!.IndexOf("TD") != -1);
                    if (!so) data.RemoveAll(x => x.mod!.IndexOf("SO") != -1);
                }
                else
                {
                    await target.Treply("osu.no_recommendation");
                    return;
                }
            }

            //检查谱面列表长度
            if (data.Count == 0)
            {
                await target.Treply("osu.no_recommendation");
                return;
            }

            //返回
            string msg = $"以下是猫猫给你推荐的谱面：\n";
            int beatmapindex = new Random().Next(0, data.Count - 1);
            string mod = "";
            if (data[beatmapindex].mod != "")
            {
                if (data[beatmapindex].mod!.Contains(','))
                    foreach (var xx in data[beatmapindex].mod!.Split(","))
                        mod += xx;
                else
                    mod += data[beatmapindex].mod!;
            }
            else
                mod += "None";
            msg += $"""
                https://osu.ppy.sh/b/{data[beatmapindex].bid}
                Stars: {data[beatmapindex].stars:0.##*}  Mod: {mod}
                PP Statistics:
                100%: {data[beatmapindex].total}pp  99%: {data[beatmapindex].pp_99acc}pp
                98%: {data[beatmapindex].pp_98acc}pp  97%: {data[
                    beatmapindex
                ].pp_97acc}pp  95%: {data[beatmapindex].pp_95acc}pp
                """;
            await target.reply(msg);
        }
    }
}
