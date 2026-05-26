using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class BadgeHelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge",
                Description = "Show badge commands",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.Treply("badge.help");
    }
}
