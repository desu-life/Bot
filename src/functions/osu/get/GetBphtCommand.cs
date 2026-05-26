using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetBphtCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get bpht",
                Description = "Show best performance hit table data",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use special pp panel", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Get.Bpht(target, cmd);
    }
}
