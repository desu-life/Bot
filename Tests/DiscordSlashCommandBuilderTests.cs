using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using Discord;
using KanonBot;
using KanonBot.Drivers;
using KanonBot.Serializer;
using System.Collections;

namespace Tests;

file class SlashBuilderTestCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "get bonuspp",
        Description = "Show bonus pp",
        Args =
        [
            new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None },
        ],
        Flags =
        [
            new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
        ]
    };

    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}

public class DiscordSlashCommandBuilderTests
{
    [Fact]
    public void Builder_CreatesFlatSlashCommandWithOptionTypes()
    {
        var props = DiscordSlashCommandBuilder.Build(new SlashBuilderTestCommand());
        var slash = Assert.IsType<SlashCommandProperties>(props);

        Assert.Equal("get-bonuspp", Value<string>(slash, "Name"));
        Assert.False(string.IsNullOrWhiteSpace(Value<string>(slash, "Description")));

        var options = Values(slash, "Options").ToArray();
        Assert.Equal(ApplicationCommandOptionType.String, Value<ApplicationCommandOptionType>(options.Single(o => Value<string>(o, "Name") == "username"), "Type"));
        Assert.Equal(ApplicationCommandOptionType.Boolean, Value<ApplicationCommandOptionType>(options.Single(o => Value<string>(o, "Name") == "is_sb"), "Type"));
        Assert.All(options, option => Assert.False(string.IsNullOrWhiteSpace(Value<string>(option, "Description"))));
    }

    [Fact]
    public void Builder_BuildsRegisteredCommandsWithDescriptions()
    {
        var props = DiscordSlashCommandBuilder.Build(CommandRegistrar.BuildRegistry());

        Assert.NotEmpty(props);
        foreach (var slash in props.Cast<SlashCommandProperties>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Value<string>(slash, "Name")));
            Assert.False(string.IsNullOrWhiteSpace(Value<string>(slash, "Description")));
            Assert.All(
                Values(slash, "Options"),
                option => Assert.False(string.IsNullOrWhiteSpace(Value<string>(option, "Description")))
            );
        }
    }

    [Fact]
    public void Builder_RejectsEmptyOptionDescription()
    {
        var command = new EmptyOptionDescriptionCommand();
        var ex = Assert.Throws<InvalidOperationException>(() => DiscordSlashCommandBuilder.Build(command));

        Assert.Contains("Description for option", ex.Message);
    }

    [Fact]
    public void Config_DiscordSlashFieldsDeserialize()
    {
        var config = Toml.Deserialize<Config.Base>(
            """
            [[drivers]]
            [drivers.discord]
            bot_id = "1"
            token = "token"
            slash_mode = "guild"
            slash_guild_ids = [1321456104479391774]
            slash_register_on_startup = true
            """
        );

        var discord = config!.drivers.Single().Discord!;
        Assert.Equal("guild", discord.slashMode);
        Assert.Equal([1321456104479391774L], discord.slashGuildIds);
        Assert.True(discord.slashRegisterOnStartup);
    }

    private static T Value<T>(object target, string property)
    {
        var value = target.GetType().GetProperty(property)!.GetValue(target)!;
        var optionalValue = value.GetType().GetProperty("Value");
        if (optionalValue is not null && value.GetType().Name.StartsWith("Optional", StringComparison.Ordinal))
            value = optionalValue.GetValue(value)!;

        return (T)value;
    }

    private static IEnumerable<object> Values(object target, string property)
    {
        var value = target.GetType().GetProperty(property)!.GetValue(target)!;
        if (value.GetType().Name.StartsWith("Optional", StringComparison.Ordinal))
        {
            var isSpecified = (bool)(value.GetType().GetProperty("IsSpecified")!.GetValue(value)!);
            if (!isSpecified)
                return [];

            value = value.GetType().GetProperty("Value")!.GetValue(value)!;
        }

        var values = (IEnumerable)value;
        return values.Cast<object>();
    }
}

file class EmptyOptionDescriptionCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "empty",
        Description = "Has an empty option description",
        Args = [new() { Name = "value", Prefix = ArgPrefix.None }],
        Flags = []
    };

    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}
