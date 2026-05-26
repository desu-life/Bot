using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class BadgeInfoCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge info",
                Description = "Show details for one installed badge",
                Args =
                [
                    new() { Name = "badge_number", Description = "Badge number", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple, Parse = s => CommandDefs.ParseInt(s) }
                ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Badge.ExecuteInfo(target, cmd.RawArgs);
    }
}
