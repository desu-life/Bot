using CommandSystem.Definition;

namespace CommandSystem.Execution;

/// <summary>
/// 所有指令的定义注册
/// 对应原来 switch(FuncType) 里的语义
/// </summary>
public static class CommandDefs
{
    // ── 通用 Parse 函数 ───────────────────────────────────

    static int? ParseInt(string s) => int.TryParse(s, out var n) ? n : null;

    static Range? ParseRange(string s)
    {
        if (string.IsNullOrEmpty(s))
            return null;

        if (ParseInt(s) is int single)
            return new Range(0, single);

        var parts = s.Split([ "..", "-" ], StringSplitOptions.RemoveEmptyEntries);
        if (
            parts.Length == 2
            && int.TryParse(parts[0], out var start)
            && int.TryParse(parts[1], out var end)
        )
            return new Range(start, end);
        return null;
    }

    // osu mode 解析，接入你原来的 ParseMode()
    // static OSU.Mode? ParseMode(string s) => s.ParseMode();

    // ── 指令定义 ──────────────────────────────────────────

    public static CommandDef Info =>
        new()
        {
            Name = "info",
            Args =
            [
                new() { Name = "username",     Prefix = ArgPrefix.None,  Strategy = ParseStrategy.Simple },
                new() { Name = "osu_mode",     Prefix = ArgPrefix.Colon, Parse = s => s }, // 接入 ParseMode
                new() { Name = "order_number", Prefix = ArgPrefix.Hash,  Parse = s => ParseInt(s) },
            ],
            Flags =
            [
                new() { Name = "special_pp", Value = "",    SlashName = "is_special_pp" },
                new() { Name = "sb_server",  Value = "sb",  SlashName = "is_sb" },
                new() { Name = "dev_panel",  Value = "dev", SlashName = "is_dev" },
                new() { Name = "sp_panel",   Value = "p",   SlashName = "is_special_panel" },
            ]
        };

    public static CommandDef BestPerformance =>
        new()
        {
            Name = "bp",
            LegacyStartsWithMatch = true,
            Args =
            [
                // Ambiguous: arg1 里 username+number 共存
                new() { Name = "username",     Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                new() { Name = "order_number", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => ParseInt(s) },
                // # 明确指定时覆盖 order_number
                new() { Name = "order_number", Prefix = ArgPrefix.Hash, Parse = s => ParseInt(s) },
                new() { Name = "osu_mode",     Prefix = ArgPrefix.Colon },
            ],
            Flags =
            [
                new() { Name = "special_pp", Value = "",    SlashName = "is_special_pp" },
                new() { Name = "sb_server",  Value = "sb",  SlashName = "is_sb" },
                new() { Name = "dev_panel",  Value = "dev", SlashName = "is_dev" },
            ]
        };

    public static CommandDef Score =>
        new()
        {
            Name = "score",
            Args =
            [
                // Ambiguous: arg1 里 username+bid 共存
                new() { Name = "username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                new() { Name = "bid",      Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => ParseInt(s) },
                // # 明确指定 bid
                new() { Name = "bid",      Prefix = ArgPrefix.Hash, Parse = s => ParseInt(s) },
                new() { Name = "osu_mode", Prefix = ArgPrefix.Colon },
                new() { Name = "osu_mods", Prefix = ArgPrefix.Plus },
            ],
            Flags =
            [
                new() { Name = "special_pp", Value = "",    SlashName = "is_special_pp" },
                new() { Name = "sb_server",  Value = "sb",  SlashName = "is_sb" },
            ]
        };

    public static CommandDef BPList =>
        new()
        {
            Name = "bplist",
            Args =
            [
                // Range: # 里是 1-100 范围
                new() { Name = "username", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous },
                new() { Name = "range", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Ambiguous, Parse = s => ParseRange(s) },
                new() { Name = "range",    Prefix = ArgPrefix.Hash, Parse = s => ParseRange(s) },
                new() { Name = "osu_mode", Prefix = ArgPrefix.Colon },
            ],
            Flags =  [ new() { Name = "sb_server", Value = "sb", SlashName = "is_sb" }, ]
        };

    public static CommandDef Recent =>
        new()
        {
            Name = "recent",
            Args =
            [
                new() { Name = "username",     Prefix = ArgPrefix.None,  Strategy = ParseStrategy.Simple },
                new() { Name = "order_number", Prefix = ArgPrefix.Hash, Parse = s => ParseInt(s) },
                new() { Name = "osu_mode",     Prefix = ArgPrefix.Colon },
            ],
            Flags =
            [
                new() { Name = "special_pp", Value = "",    SlashName = "is_special_pp" },
                new() { Name = "sb_server",  Value = "sb",  SlashName = "is_sb" },
            ]
        };

    public static CommandDef RoleCost =>
        new()
        {
            Name = "rolecost",
            Args =
            [
                new() { Name = "match_name", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                new() { Name = "username",   Prefix = ArgPrefix.Hash }, // #username
            ],
            Flags =  [ ]
        };

    // ── 注册到 Registry ───────────────────────────────────

    public static CommandRegistry BuildRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(Info);
        registry.Register(BestPerformance);
        registry.Register(Score);
        registry.Register(BPList);
        registry.Register(Recent);
        registry.Register(RoleCost);
        return registry;
    }
}
