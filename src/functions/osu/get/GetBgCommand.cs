using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetBgCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get bg",
                Description = "Get a recent beatmap background",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "order_number", Description = "Score list position", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                ],
                Flags = [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
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
