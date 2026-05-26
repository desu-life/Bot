using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class PpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "pp",
                Description = "Calculate pp for a user's beatmap score",
                Args =
                [
                    new() { Name = "username", Description = "osu! username or user ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "bid", Description = "Beatmap ID", Prefix = ArgPrefix.Hash, Parse = s => CommandDefs.ParseInt(s) },
                    new() { Name = "osu_mode", Description = "osu! game mode", Prefix = ArgPrefix.Colon },
                    new() { Name = "osu_mods", Description = "osu! mods", Prefix = ArgPrefix.Plus },
                ],
                Flags =
                [
                    new() { Name = "special_pp", Description = "Use special pp panel", Value = "", SlashName = "is_special_pp" },
                    new() { Name = "sb_server", Description = "Use the ppysb server", Value = "sb", SlashName = "is_sb" },
                ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Score.Execute(target, cmd, ppFirst: true, fetch_source: true);
    }
}
