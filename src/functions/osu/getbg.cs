using System.IO;
using CommandSystem.Parsing;
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
    public class GetBackground
    {
        public static async Task Execute(Target target, ParsedCommand cmd)
        {
            var searchArg = cmd.GetString("username") ?? "";
            var index = Math.Max(0, cmd.Get<int>("order_number") - 1);
            var isBid = int.TryParse(searchArg, out var bid);

            var beatmapset =
                await SearchAndValidate(searchArg, isBid, bid, index, hasLeaderboard: true)
                ?? await SearchAndValidate(searchArg, isBid, bid, index, hasLeaderboard: false);

            if (beatmapset is not { Beatmaps.Length: > 0 })
            {
                await target.Treply("osu.beatmap_not_found");
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

            await target.reply(
                $"https://assets.ppy.sh/beatmaps/{beatmapset!.Id}/covers/fullsize.jpg"
            );
        }

        private static async Task<API.OSU.Models.Beatmapset?> SearchAndValidate(
            string searchArg,
            bool isBid,
            int bid,
            int index,
            bool hasLeaderboard
        )
        {
            var beatmaps = await API.OSU.Client.SearchBeatmap(searchArg, null, hasLeaderboard);
            if (beatmaps == null)
                return null;

            if (isBid)
            {
                beatmaps.Beatmapsets = beatmaps
                    .Beatmapsets
                    .OrderByDescending(x => x.Beatmaps!.Any(y => y.BeatmapId == bid))
                    .ToList();
            }

            var beatmapset = beatmaps.Beatmapsets.Skip(index).FirstOrDefault();
            return beatmapset is { Beatmaps.Length: > 0 } ? beatmapset : null;
        }
    }
}
