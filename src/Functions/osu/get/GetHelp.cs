using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions
{
    public class GetHelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get",
                Description = "Show get command help",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply(
                """
            !get bonuspp
                 rolecost
                 bpht
                 bplist
                 todaybp
                 seasonalpass
                 recommend
                 mu/profile
                 bg
            """
            );
    }
}
