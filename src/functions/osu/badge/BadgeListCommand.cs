using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class BadgeListCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge list",
                Description = "List your installed badges",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Badge.ExecuteList(target);
    }
}
