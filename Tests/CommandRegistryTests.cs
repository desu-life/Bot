using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace Tests;

/// <summary>
/// 简单的 ICommand 实现，用于测试注册和匹配
/// </summary>
file class TestInfoCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "info",
        Description = "Show info",
        Aliases = ["i"],
        Args = [new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple }],
        Flags = []
    };
    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}

file class TestBpCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "bp",
        Description = "Show bp",
        LegacyStartsWithMatch = true,
        ExcludePrefixes = ["bpa", "bplist"],
        Args = [],
        Flags = []
    };
    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}

file class TestBpListCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "bplist",
        Description = "Show bplist",
        Args = [],
        Flags = []
    };
    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}

file class TestRecentCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "recent",
        Description = "Show recent",
        Aliases = ["re", "r"],
        Args = [],
        Flags = []
    };
    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}

file class TestGetBonusPpCommand : ICommand
{
    public CommandDef Definition => new()
    {
        Name = "get bonuspp",
        Description = "Show bonus pp",
        Args = [],
        Flags = []
    };
    public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
}

public class CommandRegistryTests
{
    private CommandRegistry BuildTestRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new TestInfoCommand());
        registry.Register(new TestBpCommand());
        registry.Register(new TestBpListCommand());
        registry.Register(new TestRecentCommand());
        registry.Register(new TestGetBonusPpCommand());
        return registry;
    }

    [Fact]
    public void MatchCommand_ExactMatch()
    {
        var registry = BuildTestRegistry();
        var (cmd, rawArgs) = registry.MatchCommand("info zhjk");

        Assert.NotNull(cmd);
        Assert.Equal("info", cmd!.Definition.Name);
        Assert.Equal("zhjk", rawArgs);
    }

    [Fact]
    public void MatchCommand_AliasMatch()
    {
        var registry = BuildTestRegistry();
        var (cmd, _) = registry.MatchCommand("i zhjk");

        Assert.NotNull(cmd);
        Assert.Equal("info", cmd!.Definition.Name);
    }

    [Fact]
    public void MatchCommand_NoMatch()
    {
        var registry = BuildTestRegistry();
        var (cmd, _) = registry.MatchCommand("nonexistent");

        Assert.Null(cmd);
    }

    [Fact]
    public void MatchCommand_LongestPrefix()
    {
        var registry = BuildTestRegistry();

        // "bplist" should match TestBpListCommand, not TestBpCommand
        var (cmd, _) = registry.MatchCommand("bplist");

        Assert.NotNull(cmd);
        Assert.Equal("bplist", cmd!.Definition.Name);
    }

    [Fact]
    public void MatchCommand_ExcludePrefix()
    {
        var registry = BuildTestRegistry();

        // "bpa..." - bp has ExcludePrefixes = ["bpa", "bplist"]
        // but "bpa" doesn't match bplist either
        var (cmd, _) = registry.MatchCommand("bpa something");

        // "bpa" is excluded by bp's ExcludePrefixes, and doesn't match bplist exactly
        Assert.Null(cmd);
    }

    [Fact]
    public void MatchCommand_StartsWithMatch()
    {
        var registry = BuildTestRegistry();

        // "bp5" should match bp since LegacyStartsWithMatch = true
        var (cmd, rawArgs) = registry.MatchCommand("bp5");

        Assert.NotNull(cmd);
        Assert.Equal("bp", cmd!.Definition.Name);
        Assert.Equal("5", rawArgs);
    }

    [Fact]
    public void MatchCommand_ExactMatchRequiresWhitespace()
    {
        var registry = BuildTestRegistry();

        // "infox" should NOT match "info" since LegacyStartsWithMatch = false
        var (cmd, _) = registry.MatchCommand("infox");

        Assert.Null(cmd);
    }

    [Fact]
    public void MatchCommand_CaseInsensitive()
    {
        var registry = BuildTestRegistry();
        var (cmd, _) = registry.MatchCommand("INFO test");

        Assert.NotNull(cmd);
        Assert.Equal("info", cmd!.Definition.Name);
    }

    [Fact]
    public void TryGet_ByName()
    {
        var registry = BuildTestRegistry();
        var found = registry.TryGet("recent", out var cmd);

        Assert.True(found);
        Assert.NotNull(cmd);
        Assert.Equal("recent", cmd!.Definition.Name);
    }

    [Fact]
    public void TryGet_ByAlias()
    {
        var registry = BuildTestRegistry();
        var found = registry.TryGet("re", out var cmd);

        Assert.True(found);
        Assert.Equal("recent", cmd!.Definition.Name);
    }

    [Fact]
    public void TryGet_NotFound()
    {
        var registry = BuildTestRegistry();
        var found = registry.TryGet("missing", out var cmd);

        Assert.False(found);
        Assert.Null(cmd);
    }

    [Fact]
    public void SlashName_DefaultFlattensLegacyName()
    {
        var def = new CommandDef { Name = "get bonuspp" };

        Assert.Equal("get-bonuspp", def.SlashName);
    }

    [Fact]
    public void TryGetSlash_ByFlatName()
    {
        var registry = BuildTestRegistry();
        var found = registry.TryGetSlash("get-bonuspp", out var cmd);

        Assert.True(found);
        Assert.NotNull(cmd);
        Assert.Equal("get bonuspp", cmd!.Definition.Name);
    }
}
