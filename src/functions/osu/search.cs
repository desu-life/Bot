using System.IO;
using DotNext.Collections.Generic;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.LegacyImage;
using KanonBot.Message;
using KanonBot.OsuPerformance;
using LanguageExt.UnsafeValueAccess;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KanonBot.Functions.OSUBot
{
    public class Search
    {
        public static async Task Execute(Target target, string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) { return; }

            var command = BotCmdHelper.CmdParser(
                cmd,
                BotCmdHelper.FuncType.Search,
                false,
                true,
                true,
                false,
                false
            );
            // 判断是否给定了bid
            API.OSU.Models.Mod[]? mods_lazer = null;
            var index = Math.Max(0, command.order_number - 1);
            var isBid = int.TryParse(command.search_arg, out var bid);

            bool beatmapFound = true;
            API.OSU.Models.BeatmapSearchResult? beatmaps = null;
            API.OSU.Models.Beatmapset? beatmapset = null;

            string? search_arg = null;
            string? diff_arg = null;
            
            // 找到方括号的位置
            int startIndex = command.search_arg.LastIndexOf('[');
            int endIndex = command.search_arg.LastIndexOf(']');

            if (startIndex < endIndex && startIndex > 0)
            {
                // 提取name
                string name = command.search_arg[..startIndex].Trim();

                // 提取subname
                string subname = command.search_arg.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                if (!string.IsNullOrEmpty(name)) {
                    search_arg = name;
                    diff_arg = subname;
                }
            }

            search_arg ??= command.search_arg;
            Log.Debug($"Name: {search_arg}");
            Log.Debug($"Subname: {diff_arg}");

            API.OSU.Mode? preferedMode = null;

            var AccInfo = Accounts.GetAccInfo(target);
            var DBUser = await Accounts.GetAccount(AccInfo.uid, AccInfo.platform);
            if (DBUser is not null) {
                var DBOsuInfo = await Accounts.CheckOsuAccount(DBUser.uid);
                preferedMode = DBOsuInfo?.osu_mode?.ToMode();
            }

            beatmaps = await API.OSU.Client.SearchBeatmap(command.search_arg, null);
            if (beatmaps != null) {
                beatmaps.Beatmapsets = [.. beatmaps.Beatmapsets.OrderByDescending(x => {
                    var beatmaps = x.Beatmaps ?? [];
                    // 优先检查是否匹配 BeatmapId
                    if (isBid && beatmaps.Any(y => y.BeatmapId == bid))
                        return 2;

                    // 检查是否匹配 Mode
                    if (beatmaps.Any(y => y.Mode == preferedMode))
                        return 1;

                    // 不匹配则返回最低优先级
                    return 0;
                })];
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
                beatmaps = await API.OSU.Client.SearchBeatmap(command.search_arg, null, false);
                beatmapFound = true;
            }

            if (beatmaps != null) {
                beatmaps.Beatmapsets = [.. beatmaps.Beatmapsets.OrderByDescending(x => {
                    var beatmaps = x.Beatmaps ?? [];
                    // 优先检查是否匹配 BeatmapId
                    if (isBid && beatmaps.Any(y => y.BeatmapId == bid))
                        return 2;

                    // 检查是否匹配 Mode
                    if (beatmaps.Any(y => y.Mode == preferedMode))
                        return 1;

                    // 不匹配则返回最低优先级
                    return 0;
                })];
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
                .Beatmaps!.OrderByDescending(x => x.DifficultyRating)
                .ToArray();

            API.OSU.Models.Beatmap? beatmap = null;

            if (isBid)
            {
                beatmap = beatmapset
                    .Beatmaps.Find(x => x.BeatmapId == bid)
                    .IfNone(() => beatmapset.Beatmaps.First());
            }
            else
            {
                if (!string.IsNullOrEmpty(diff_arg)) {
                    if (diff_arg == "*") {
                        beatmap = beatmapset.Beatmaps.First();
                    } else {
                        int closestIndex = Utils.FindClosestMatchIndex(
                            diff_arg,
                            beatmapset.Beatmaps,
                            item => item.Version
                        );
                        beatmap = beatmapset.Beatmaps[closestIndex];
                    }
                } else {
                    beatmap = beatmapset.Beatmaps.First();
                }
            }

            beatmap.Beatmapset = beatmaps!.Beatmapsets[0];

            var b = await Utils.LoadOrDownloadBeatmap(beatmap);

            var rmods = RosuPP.Mods.FromAcronyms(command.osu_mods, beatmap.Mode.ToRosu());
            rmods.RemoveIncompatibleMods();
            var js = RosuPP.OwnedString.Empty();
            rmods.Json(js.Context);
            mods_lazer = Serializer.Json.Deserialize<API.OSU.Models.Mod[]>(js.ToCstr())!;
            Log.Debug($"Mods: {string.Join(",", mods_lazer.Select(x => x.Acronym))}");

            Draw.ScorePanelData data;
            API.OSU.Models.User? user = await API.OSU.Client.GetUser(3);
            if (mods_lazer.Any(x => x.Acronym is "RX" or "AP")) {
                data = SBRosuCalculator.CalculatePanelSSData(b, beatmap, mods_lazer);
                user!.Id = 1;
                user!.Username = "ChinoBot";
                user!.IsBot = true;
                user!.AvatarUrl = new Uri("https://a.ppy.sb/1");
            } else {
                data = RosuCalculator.CalculatePanelSSData(b, beatmap, mods_lazer);
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

            using var stream = new MemoryStream();
            using var img = await LegacyImage.Draw.DrawScore(data);
            await img.SaveAsync(stream, new JpegEncoder());
            await target.reply(
                new Chain()
                    .image(
                        Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                        ImageSegment.Type.Base64
                    )
            );
        }
    }
}
