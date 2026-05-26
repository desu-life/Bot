using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;

namespace Tests;

public class CommandParsingTests
{
    private readonly LegacyParser _legacy = new();
    private readonly SlashParser _slash = new();

    // ─── Helper: 创建一个类似 info 命令的 CommandDef ─────────

    private static CommandDef MakeInfoDef() => new()
    {
        Name = "info",
        Description = "Show info",
        Args =
        [
            new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
            new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
        ],
        Flags =
        [
            new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
        ]
    };

    private static CommandDef MakeBpDef() => new()
    {
        Name = "bp",
        Description = "Show bp",
        LegacyStartsWithMatch = true,
        ExcludePrefixes = ["bpa", "bpme", "bplist"],
        Args =
        [
            new() { Name = "username", Description = "osu! Username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
            new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => int.TryParse(s, out var n) ? n : null },
            new() { Name = "order_number", Description = "Score index", Prefix = ArgPrefix.Hash, Parse = s => int.TryParse(s, out var n) ? n : null },
            new() { Name = "osu_mode", Description = "osu! Gamemode", Prefix = ArgPrefix.Colon },
        ],
        Flags =
        [
            new() { Name = "special_pp", Description = "Alternative pp calculater", Value = "", SlashName = "is_special_pp" },
            new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" },
        ]
    };

    private static CommandDef MakeSimpleDef() => new()
    {
        Name = "ping",
        Description = "Ping",
        Args = [],
        Flags = []
    };

    // ─── LegacyParser Tests ──────────────────────────────

    [Fact]
    public void Legacy_SimpleUsername()
    {
        var def = MakeInfoDef();
        var result = _legacy.Parse("info", "zhjk", def);

        Assert.Equal("info", result.CommandName);
        Assert.Equal("zhjk", result.Args["username"]);
        Assert.False(result.SelfQuery);
    }

    [Fact]
    public void Legacy_SelfQuery_EmptyArgs()
    {
        var def = MakeInfoDef();
        var result = _legacy.Parse("info", "", def);

        Assert.True(result.SelfQuery);
        Assert.Null(result.Args["username"]);
    }

    [Fact]
    public void Legacy_WithMode()
    {
        var def = MakeInfoDef();
        var result = _legacy.Parse("info", "zhjk :mania", def);

        Assert.Equal("zhjk", result.Args["username"]);
        Assert.Equal("mania", result.Args["osu_mode"]);
    }

    [Fact]
    public void Legacy_WithFlag()
    {
        var def = MakeInfoDef();
        var result = _legacy.Parse("info", "zhjk &sb", def);

        Assert.Equal("zhjk", result.Args["username"]);
        Assert.True(result.Flag("sb_server"));
    }

    [Fact]
    public void Legacy_AllPrefixes()
    {
        var def = MakeBpDef();
        var result = _legacy.Parse("bp", "zhjk #5 :taiko &sb", def);

        Assert.Equal("zhjk", result.Args["username"]);
        Assert.Equal("5", result.Args["order_number"]);
        Assert.Equal("taiko", result.Args["osu_mode"]);
        Assert.True(result.Flag("sb_server"));
        Assert.False(result.Flag("special_pp"));
    }

    [Fact]
    public void Legacy_NoArgs_NoFlags()
    {
        var def = MakeSimpleDef();
        var result = _legacy.Parse("ping", "", def);

        Assert.Equal("ping", result.CommandName);
        Assert.Empty(result.Args);
    }

    [Fact]
    public void Legacy_OnlyHash()
    {
        var def = MakeBpDef();
        var result = _legacy.Parse("bp", "#10", def);

        Assert.True(result.SelfQuery);
        Assert.Equal("10", result.Args["order_number"]);
    }

    [Fact]
    public void Legacy_Ambiguous_NumberOnly()
    {
        var def = MakeBpDef();
        var result = _legacy.Parse("bp", "5", def);

        // "5" is fully parsed as number by AmbiguousResolver
        // username = null, order_number = "5"
        Assert.Null(result.Args["username"]);
        Assert.Equal("5", result.Args["order_number"]);
        Assert.True(result.SelfQuery);
    }

    [Fact]
    public void Legacy_Ambiguous_UsernameAndNumber()
    {
        var def = MakeBpDef();
        var result = _legacy.Parse("bp", "zhjk 5", def);

        // AmbiguousResolver splits: tail "5" parses as number
        Assert.Equal("zhjk", result.Args["username"]);
        Assert.Equal("5", result.Args["order_number"]);
        Assert.False(result.SelfQuery);
    }

    [Fact]
    public void Legacy_FlagNotMatching()
    {
        var def = MakeInfoDef();
        var result = _legacy.Parse("info", "user &xyz", def);

        // &xyz doesn't match any known flag
        Assert.False(result.Flag("sb_server"));
    }

    // ─── SlashParser Tests ──────────────────────────────

    [Fact]
    public void Slash_BasicParsing()
    {
        var def = MakeInfoDef();
        var options = new Dictionary<string, string>
        {
            ["username"] = "zhjk",
            ["osu_mode"] = "mania",
        };

        var result = _slash.Parse("info", options, def);

        Assert.Equal("info", result.CommandName);
        Assert.Equal("zhjk", result.Args["username"]);
        Assert.Equal("mania", result.Args["osu_mode"]);
        Assert.False(result.SelfQuery);
    }

    [Fact]
    public void Slash_SelfQuery_NoUsername()
    {
        var def = MakeInfoDef();
        var options = new Dictionary<string, string>
        {
            ["osu_mode"] = "taiko",
        };

        var result = _slash.Parse("info", options, def);

        Assert.True(result.SelfQuery);
    }

    [Fact]
    public void Slash_FlagMapping()
    {
        var def = MakeInfoDef();
        var options = new Dictionary<string, string>
        {
            ["username"] = "test",
            ["is_sb"] = "true",
        };

        var result = _slash.Parse("info", options, def);

        Assert.True(result.Flag("sb_server"));
    }

    [Fact]
    public void Slash_FlagFalse()
    {
        var def = MakeInfoDef();
        var options = new Dictionary<string, string>
        {
            ["username"] = "test",
            ["is_sb"] = "false",
        };

        var result = _slash.Parse("info", options, def);

        Assert.False(result.Flag("sb_server"));
    }

    [Fact]
    public void Slash_EmptyOptions()
    {
        var def = MakeInfoDef();
        var options = new Dictionary<string, string>();

        var result = _slash.Parse("info", options, def);

        Assert.True(result.SelfQuery);
    }

    [Fact]
    public void Slash_UnknownKey_Ignored()
    {
        var def = MakeInfoDef();
        var options = new Dictionary<string, string>
        {
            ["username"] = "test",
            ["nonexistent_param"] = "value",
        };

        var result = _slash.Parse("info", options, def);

        Assert.Equal("test", result.Args["username"]);
        Assert.False(result.Args.ContainsKey("nonexistent_param"));
    }

    [Fact]
    public void Slash_RawArgs_ForSimpleArg()
    {
        var def = new CommandDef
        {
            Name = "bind",
            Description = "Bind account",
            Args = [new() { Name = "code", Description = "Binding verification code", Prefix = ArgPrefix.None }],
            Flags = []
        };

        var result = _slash.Parse("bind", new() { ["code"] = "123456" }, def);

        Assert.Equal("123456", result.RawArgs);
    }

    [Fact]
    public void Slash_RawArgs_ForPrefixedArgsAndFlags()
    {
        var def = MakeInfoDef();
        var result = _slash.Parse(
            "info",
            new() { ["username"] = "zhjk", ["osu_mode"] = "mania", ["is_sb"] = "true" },
            def
        );

        Assert.Equal("zhjk :mania &sb", result.RawArgs);
    }
}
