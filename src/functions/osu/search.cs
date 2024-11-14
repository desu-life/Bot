using System.IO;
using DotNext.Collections.Generic;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
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

            try
            {
                mods_lazer = Serializer.Json.Deserialize<API.OSU.Models.Mod[]>(command.osu_mods);
            }
            catch { }

            if (mods_lazer == null)
            {
                List<string> mods = new();
                try
                {
                    mods = Enumerable
                        .Range(0, command.osu_mods.Length / 2)
                        .Select(p =>
                            new string(command.osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper()
                        )
                        .ToList();
                }
                catch { }
                mods_lazer = mods.Map(API.OSU.Models.Mod.FromString).ToArray();
            }

            Log.Debug($"Mods: {string.Join(",", mods_lazer.Select(x => x.Acronym))}");

            bool beatmapFound = true;
            API.OSU.Models.BeatmapSearchResult? beatmaps = null;
            API.OSU.Models.Beatmapset? beatmapset = null;

            beatmaps = await API.OSU.Client.SearchBeatmap(command.search_arg, null);
            if (beatmaps != null && isBid) {
                beatmaps.Beatmapsets = beatmaps.Beatmapsets.OrderByDescending(x => x.Beatmaps.Find(y => y.BeatmapId == bid) != null).ToList();
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

            if (beatmaps != null && isBid) {
                beatmaps.Beatmapsets = beatmaps.Beatmapsets.OrderByDescending(x => x.Beatmaps.Find(y => y.BeatmapId == bid) != null).ToList();
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
                beatmap = beatmapset.Beatmaps.First();
            }

            beatmap.Beatmapset = beatmaps!.Beatmapsets[0];

            var b = await Utils.LoadOrDownloadBeatmap(beatmap);
            var data = RosuCalculator.CalculatePanelSSData(b, beatmap, mods_lazer);

            data.scoreInfo.UserId = 3; // bancho bot
            data.scoreInfo.User = await API.OSU.Client.GetUser(data.scoreInfo.UserId);
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
                    .msg("该功能为测试功能。")
            );
        }
    }
}
