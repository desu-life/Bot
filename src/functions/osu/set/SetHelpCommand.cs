using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class SetHelpCommand : ICommand
    {
        private const string SettingsUrl = "https://hub.kagamistudio.com/settings/";
        public CommandDef Definition =>
            new()
            {
                Name = "set",
                Description = "Deprecated",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.Treply("set.migrated", SettingsUrl);
    }
}
