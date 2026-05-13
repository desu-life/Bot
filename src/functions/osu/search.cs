using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using DotNext.Collections.Generic;
using KanonBot.API;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Image;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions.OSUBot
{
    public class SearchCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "search",
                Aliases =  [ "sc" ],
                Args =
                [
                    new() { Name = "search_arg", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "order_number", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mods", Prefix = ArgPrefix.Plus },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Value = "",    SlashName = "is_special_pp" },
                ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var searchArg = cmd.GetString("search_arg") ?? "";
            if (string.IsNullOrWhiteSpace(searchArg))
            {
                return;
            }

            // 判断是否给定了bid
            API.OSU.Models.Mod[]? mods_lazer = null;
            var index = (int)Math.Max(0, cmd.Get<int>("order_number") - 1);
            var isBid = int.TryParse(searchArg, out var bid);
            var osu_mods = cmd.GetString("osu_mods") ?? "";

            bool beatmapFound = true;
            API.OSU.Models.BeatmapSearchResult? beatmaps = null;
            API.OSU.Models.Beatmapset? beatmapset = null;

            string? search_arg = null;
            string? diff_arg = null;

            // 找到方括号的位置
            int startIndex = searchArg.LastIndexOf('[');
            int endIndex = searchArg.LastIndexOf(']');

            if (startIndex < endIndex && startIndex > 0)
            {
                // 提取name
                string name = searchArg[..startIndex].Trim();

                // 提取subname
                string subname = searchArg
                    .Substring(startIndex + 1, endIndex - startIndex - 1)
                    .Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    search_arg = name;
                    diff_arg = subname;
                }
            }

            search_arg ??= searchArg;
            Log.Debug($"Name: {search_arg}");
            Log.Debug($"Subname: {diff_arg}");

            API.OSU.Mode? preferedMode = null;

            // 通过IAM获取用户偏好模式（非阻塞，失败则不设置）
            var AccInfo = Accounts.GetAccInfo(target);
            try
            {
                var provider = API.IAM.Client.PlatformToProvider(AccInfo.platform);
                var iamUserId = await API.IAM
                    .Client
                    .GetIamUserIdByExternalId(provider, AccInfo.uid);
                if (iamUserId != null)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    preferedMode = KagamiExtensions.ParseKagamiMode(
                        kagamiProfile?.KanonBot?.PreferredGameMode
                    );
                }
            }
            catch { }

            beatmaps = await API.OSU.Client.SearchBeatmap(searchArg, null);
            if (beatmaps != null)
            {
                beatmaps.Beatmapsets =
                [
                    .. beatmaps.Beatmapsets.OrderByDescending(x => {
                    var beatmaps = x.Beatmaps ?? [];
                    // 优先检查是否匹配 BeatmapId
                    if (isBid && beatmaps.Any(y => y.BeatmapId == bid))
                        return 2;

                    // 检查是否匹配 Mode
                    if (beatmaps.Any(y => y.Mode == preferedMode))
                        return 1;

                    // 不匹配则返回最低优先级
                    return 0;
                })
                ];
            }
            beatmapset = beatmaps?.Beatmapsets.Skip(index).FirstOrDefault();

            if (beatmapset == null)
            {
                beatmapFound = false;
            }
            else if (beatmapset.Beatmaps == null)
            {
                beatmapFound = false;
            }
            else if (beatmapset.Beatmaps.Length == 0)
            {
                beatmapFound = false;
            }

            if (!beatmapFound)
            {
                beatmaps = await API.OSU.Client.SearchBeatmap(searchArg, null, false);
                beatmapFound = true;
            }

            if (beatmaps != null)
            {
                beatmaps.Beatmapsets =
                [
                    .. beatmaps.Beatmapsets.OrderByDescending(x => {
                    var beatmaps = x.Beatmaps ?? [];
                    // 优先检查是否匹配 BeatmapId
                    if (isBid && beatmaps.Any(y => y.BeatmapId == bid))
                        return 2;

                    // 检查是否匹配 Mode
                    if (beatmaps.Any(y => y.Mode == preferedMode))
                        return 1;

                    // 不匹配则返回最低优先级
                    return 0;
                })
                ];
            }
            beatmapset = beatmaps?.Beatmapsets.Skip(index).FirstOrDefault();

            if (beatmapset == null)
            {
                beatmapFound = false;
            }
            else if (beatmapset.Beatmaps == null)
            {
                beatmapFound = false;
            }
            else if (beatmapset.Beatmaps.Length == 0)
            {
                beatmapFound = false;
            }

            if (!beatmapFound)
            {
                await target.reply("未找到谱面。");
                return;
            }

            beatmapset!.Beatmaps = beatmapset
                .Beatmaps!
                .OrderByDescending(x => x.DifficultyRating)
                .ToArray();

            API.OSU.Models.Beatmap? beatmap = null;

            if (isBid)
            {
                beatmap = beatmapset
                    .Beatmaps
                    .Find(x => x.BeatmapId == bid)
                    .IfNone(() => beatmapset.Beatmaps.First());
            }
            else
            {
                if (!string.IsNullOrEmpty(diff_arg))
                {
                    if (diff_arg == "*")
                    {
                        beatmap = beatmapset.Beatmaps.First();
                    }
                    else
                    {
                        int closestIndex = Utils.FindClosestMatchIndex(
                            diff_arg,
                            beatmapset.Beatmaps,
                            item => item.Version
                        );
                        beatmap = beatmapset.Beatmaps[closestIndex];
                    }
                }
                else
                {
                    beatmap = beatmapset.Beatmaps.First();
                }
            }

            beatmap.Beatmapset = beatmaps!.Beatmapsets[0];

            var b = await Utils.LoadOrDownloadBeatmap(beatmap);

            using var rmods = RosuPP.Mods.FromAcronyms(osu_mods, beatmap.Mode.ToRosu());
            rmods.Sanitize();
            rmods.RemoveUnknownMods();
            using var js = RosuPP.OwnedString.Empty();
            rmods.Json(js.Context);
            mods_lazer = Serializer.Json.Deserialize<API.OSU.Models.Mod[]>(js.ToCstr())!;
            Log.Debug($"Mods: {string.Join(",", mods_lazer.Select(x => x.Acronym))}");

            ScoreV2.ScorePanelData data;
            API.OSU.Models.User? user = await API.OSU.Client.GetUser(3);
            var is_sb = mods_lazer.Any(x => x.Acronym is "RX" or "AP");
            if (is_sb)
            {
                data = SBRosuCalculator.CalculatePanelSSData(b, beatmap, mods_lazer);
                user!.Id = 1;
                user!.Username = "ChinoBot";
                user!.IsBot = true;
                user!.AvatarUrl = new Uri("https://a.ppy.sb/1");
            }
            else
            {
                if (cmd.Flag("special_pp"))
                {
                    data = OsuCalculator.CalculatePanelSSData(b, beatmap, mods_lazer);
                }
                else
                {
                    data = RosuCalculator.CalculatePanelSSData(b, beatmap, mods_lazer);
                }
            }

            data.scoreInfo.UserId = user!.Id;
            data.scoreInfo.User = user;
            data.scoreInfo.Beatmapset = beatmapset;
            data.scoreInfo.Beatmap = beatmap;
            data.scoreInfo.ModeInt = beatmap.Mode.ToNum();
            data.scoreInfo.Mods = mods_lazer;
            data.scoreInfo.EndedAt = DateTime.UtcNow;
            data.scoreInfo.StartedAt = DateTime.UtcNow;
            data.scoreInfo.Score = 1000000;

            using var img = await Image.ScoreV2.DrawScore(data);
            await target.reply(img, new JpegEncoder());

            // 缓存本来源查询
            HistoryBeatmapMapper.Map(target.source, beatmap.BeatmapId);
        }
    }
}
