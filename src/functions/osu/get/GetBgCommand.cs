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
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => GetBackground.Execute(target, cmd);
    }
}
