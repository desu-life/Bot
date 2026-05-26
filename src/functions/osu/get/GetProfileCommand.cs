using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class GetProfileCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "get mu",
                Description = "Show an osu! profile link",
                SlashName = "get-profile",
                Aliases =  [ "get profile" ],
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Get.SendProfileLink(target, cmd);
    }
}
