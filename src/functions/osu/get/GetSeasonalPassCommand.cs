using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetSeasonalPassCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get seasonalpass",
                Description = "Show seasonal pass information",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags =  [ new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" } ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Get.SeasonalPass(target, cmd);
    }
}
