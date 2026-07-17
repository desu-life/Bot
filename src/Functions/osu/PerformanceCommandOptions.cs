using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;
using KanonBot.OsuPerformance;

namespace KanonBot.Functions;

internal static class PerformanceCommandOptions
{
    public static async Task<string?> Resolve(
        Target target,
        ParsedCommand command,
        API.OSU.Mode mode,
        bool isPpysb
    )
    {
        if (command.Errors.Count > 0)
        {
            await target.Treply("error.command_parse");
            return null;
        }

        try
        {
            return UniversalCalculator.ResolveCalculatorId(
                command.GetString("pp_version"),
                mode,
                isPpysb,
                command.Flag("special_pp")
            );
        }
        catch (Exception exception) when (
            exception is KeyNotFoundException or NotSupportedException
        )
        {
            await target.Treply("osu.pp_version_invalid");
            return null;
        }
    }
}
