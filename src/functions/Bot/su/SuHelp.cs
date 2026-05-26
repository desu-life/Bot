using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSU
{
    public class SuHelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "su",
                Description = "Show superuser commands",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
    }
}
