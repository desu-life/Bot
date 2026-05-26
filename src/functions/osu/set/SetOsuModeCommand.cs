using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class SetOsuModeCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "set osumode",
                Description = "Set your default osu! game mode",
                Args =
                [
                    new() { Name = "mode", Description = "Default osu! game mode", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple }
                ],
                Flags =  [ new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" } ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Setter.SetOsuMode(target, cmd);
    }
}
