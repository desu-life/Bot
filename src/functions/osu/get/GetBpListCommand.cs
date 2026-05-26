using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetBpListCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get bplist",
                Description = "Show an osu! best performance list",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                    new() { Name = "range", Description = "Score rank range", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseRange(s) },
                    new() { Name = "range", Description = "Score rank range", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseRange(s) },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags =  [ new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" } ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => BPList.Execute(target, cmd);
    }
}
