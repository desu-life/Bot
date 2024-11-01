using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using LanguageExt.UnsafeValueAccess;
using KanonBot.Functions.OSU;
using KanonBot.OsuPerformance;


namespace KanonBot.Functions.OSUBot
{
    public class Search
    {
        async public static Task Execute(Target target, string cmd)
        {
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Score, false, true, true, false, false);
            var mode = command.osu_mode;
            // 判断是否给定了bid
            API.OSU.Models.Mod[]? mods_lazer = null;

            try {
                mods_lazer = Serializer.Json.Deserialize<API.OSU.Models.Mod[]>(command.osu_mods);
            } catch { }

            if (mods_lazer == null)
            {
                List<string> mods = new();
                try
                {
                    mods = Enumerable
                        .Range(0, command.osu_mods.Length / 2)
                        .Select(p => new string(command.osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper())
                        .ToList();
                }
                catch { }
                mods_lazer = mods.Map(API.OSU.Models.Mod.FromString).ToArray();
            }

            Log.Debug($"Mods: {string.Join(",", mods_lazer.Select(x => x.Acronym))}");

            bool beatmapFound = true;
            API.OSU.Models.BeatmapSearchResult? beatmaps = null;

            if (command.order_number > 0)
            {
                beatmaps = await API.OSU.Client.SearchBeatmap(command.order_number.ToString(), mode);
            }

            if (beatmaps is null) {
                beatmapFound = false;
            } else if (beatmaps.Beatmapsets.Count == 0) {
                beatmapFound = false;
            }

            if (!beatmapFound) {
                beatmaps = await API.OSU.Client.SearchBeatmap(command.osu_username, mode);
                beatmapFound = true;
            }
            
            if (beatmaps is null) {
                beatmapFound = false;
            } else if (beatmaps.Beatmapsets.Count == 0) {
                beatmapFound = false;
            }

            var beatmapset = beatmaps!.Beatmapsets[0];
            if (beatmapset.Beatmaps is null) {
                beatmapFound = false;
            } else if (beatmapset.Beatmaps.Length == 0) {
                beatmapFound = false;
            }

            if (!beatmapFound)
            {
                await target.reply("未找到谱面。");
                return;
            }

            beatmapset.Beatmaps = beatmapset.Beatmaps!.OrderByDescending(x => x.DifficultyRating).ToArray();

            API.OSU.Models.Beatmap? beatmap = null;

            if (command.order_number != -1) {
                beatmap = beatmapset.Beatmaps.Find(x => x.BeatmapId == command.order_number).IfNone(() => beatmapset.Beatmaps.First());
            } else {
                beatmap = beatmapset.Beatmaps.First();
            }

            beatmap.Beatmapset = beatmaps.Beatmapsets[0];

            var data = await OsuCalculator.CalculatePanelSSData(beatmap, mods_lazer);
            
            data.scoreInfo.UserId = 3;  // bancho bot
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
                new Chain().image(
                    Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                    ImageSegment.Type.Base64
                ).msg("该功能为测试功能。")
            );
        }
    }
}
