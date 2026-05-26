using CommandSystem.Definition;
using Discord;

namespace CommandSystem.Execution;

public static class DiscordSlashCommandBuilder
{
    public static ApplicationCommandProperties[] Build(CommandRegistry registry) =>
        registry.Commands.Select(Build).ToArray();

    public static ApplicationCommandProperties Build(ICommand command)
    {
        var def = command.Definition;
        ValidateRequired(def.SlashName, $"Slash name for command '{def.Name}'");
        ValidateRequired(def.Description, $"Description for command '{def.Name}'");

        var builder = new SlashCommandBuilder()
            .WithName(def.SlashName)
            .WithDescription(def.Description);

        foreach (var argDef in def.Args.DistinctBy(static a => a.Name))
        {
            ValidateRequired(
                argDef.Description,
                $"Description for option '{argDef.Name}' on command '{def.Name}'"
            );
            builder.AddOption(
                argDef.Name,
                ApplicationCommandOptionType.String,
                argDef.Description,
                isRequired: false
            );
        }

        foreach (var flagDef in def.Flags.DistinctBy(static f => f.SlashName))
        {
            ValidateRequired(
                flagDef.Description,
                $"Description for option '{flagDef.SlashName}' on command '{def.Name}'"
            );
            builder.AddOption(
                flagDef.SlashName,
                ApplicationCommandOptionType.Boolean,
                flagDef.Description,
                isRequired: false
            );
        }

        return builder.Build();
    }

    private static void ValidateRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{field} cannot be empty.");
    }
}
