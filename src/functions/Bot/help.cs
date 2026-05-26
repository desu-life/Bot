using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions
{
    public class HelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "help",
                Description = "Helpppppppp",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.Treply("help.main");
    }
}
