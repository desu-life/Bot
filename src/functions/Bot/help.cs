using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;
using KanonBot.I18n;

namespace KanonBot.Functions.OSUBot
{
    public class HelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "help",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply(target.T("help.main"));
    }
}
