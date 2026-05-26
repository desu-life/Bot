using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetRoleCostCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get rolecost",
                Description = "Calculate role cost for a match",
                Args =
                [
                    new() { Name = "match_name", Description = "Match name", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.Hash },
                    new() { Name = "order_number", Description = "Score list position", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Get.Rolecost(target, cmd);
    }
}
