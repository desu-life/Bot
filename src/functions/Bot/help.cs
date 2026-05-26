using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class HelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "help",
                Description = "Show KanonBot help",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.Treply("help.main");
    }
}
