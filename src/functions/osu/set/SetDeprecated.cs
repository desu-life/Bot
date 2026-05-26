using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions
{
    public class SetDeprecatedCommand : ICommand
    {
        private const string SettingsUrl = "https://hub.kagamistudio.com/settings/";
        public CommandDef Definition =>
            new()
            {
                Name = "set osuinfopanelversion",
                Description = "Deprecated",
                Aliases =
                [
                    "set osuinfopanelv2colormode",
                    "set osuinfopanelv2colorcustom",
                    "set osuinfopanelv2img",
                    "set osuinfopanelv1img",
                    "set osuinfopanelv2panel",
                    "set osuinfopanelv1panel",
                ],
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.Treply("set.deprecated", SettingsUrl);
    }
}
