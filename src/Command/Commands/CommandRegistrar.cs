using CommandSystem;
using CommandSystem.Definition;
using KanonBot.Functions.OSU;
using KanonBot.Functions.OSUBot;

namespace CommandSystem.Execution;

/// <summary>
/// 注册所有 ICommand 实例到 Registry
/// </summary>
public static class CommandRegistrar
{
    public static CommandRegistry BuildRegistry()
    {
        var registry = new CommandRegistry();

        // 基础命令
        registry.Register(new InfoCommand());
        registry.Register(new BpCommand());
        registry.Register(new ScoreCommand());
        registry.Register(new PpCommand());
        registry.Register(new RecentCommand());
        registry.Register(new PassRecentCommand());
        registry.Register(new RecentListCommand());
        registry.Register(new PassRecentListCommand());
        registry.Register(new BpListCommand());
        registry.Register(new SearchCommand());
        registry.Register(new LeewayCommand());
        registry.Register(new TodayBpCommand());
        registry.Register(new PpvsCommand());
        registry.Register(new UpdateCommand());

        // Badge 命令
        registry.Register(new BadgeInfoCommand());
        registry.Register(new BadgeListCommand());
        registry.Register(new BadgeDeprecatedCommand());
        registry.Register(new BadgeHelpCommand());

        // Get 命令
        registry.Register(new GetBonusPpCommand());
        registry.Register(new GetBpListCommand());
        registry.Register(new GetRoleCostCommand());
        registry.Register(new GetBphtCommand());
        registry.Register(new GetTodayBpCommand());
        registry.Register(new GetSeasonalPassCommand());
        registry.Register(new GetRecommendCommand());
        registry.Register(new GetProfileCommand());
        registry.Register(new GetBgCommand());
        registry.Register(new GetHelpCommand());

        // Set 命令
        registry.Register(new SetOsuModeCommand());
        registry.Register(new SetDeprecatedCommand());
        registry.Register(new SetHelpCommand());

        // Su 命令
        registry.Register(new SuUpdateAllCommand());
        registry.Register(new SuHelpCommand());

        // Bot 命令
        registry.Register(new BindCommand());
        registry.Register(new HelpCommand());
        registry.Register(new PingCommand());

        return registry;
    }
}
