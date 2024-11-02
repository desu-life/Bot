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
    public class GetBackground
    {
        async public static Task Execute(Target target, string cmd)
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

            var index = Math.Max(0, command.order_number - 1);
            var isBid = int.TryParse(command.search_arg, out var bid);

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

            // beatmapset!.Beatmaps = beatmapset
            //     .Beatmaps!.OrderByDescending(x => x.DifficultyRating)
            //     .ToArray();

            // API.OSU.Models.Beatmap? beatmap = null;

            // if (isBid)
            // {
            //     beatmap = beatmapset
            //         .Beatmaps.Find(x => x.BeatmapId == command.bid)
            //         .IfNone(() => beatmapset.Beatmaps.First());
            // }
            // else
            // {
            //     beatmap = beatmapset.Beatmaps.First();
            // }

            // beatmap.Beatmapset = beatmaps!.Beatmapsets[0];

            await target.reply($"https://assets.ppy.sh/beatmaps/{beatmapset!.Id}/covers/raw.jpg");
        }
    }
}
