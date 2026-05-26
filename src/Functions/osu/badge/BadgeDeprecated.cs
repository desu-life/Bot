using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions
{
    public class BadgeDeprecatedCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge set",
                Description = "Deprecated",
                Aliases =  [ "badge redeem", "badge sudo" ],
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.Treply("badge.deprecated");
    }
}
