using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetTodayBpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get todaybp",
                Description = "Show today's best performance",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                    new() { Name = "order_number", Description = "Score list position", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                ],
                Flags =  [ new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" } ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => TodayBP.Execute(target, cmd);
    }
}
