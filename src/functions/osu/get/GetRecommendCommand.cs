using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
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
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                    new() { Name = "osu_mods", Description = "osu! mods", Prefix = ArgPrefix.Plus },
                ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Get.BeatmapRecommend(target, cmd);
    }
}
