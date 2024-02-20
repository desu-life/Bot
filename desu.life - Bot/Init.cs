using desu_life_Bot.Drivers;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static desu_life_Bot.Dev;
using static desu_life_Bot.Command.CommandRegister;
using static desu_life_Bot.Command.CommandSystem;

namespace desu_life_Bot;

/// <summary>
/// 初始化应用程序配置，通过从预定义路径加载配置信息。
/// 如果指定路径上不存在配置文件，则创建一个默认配置并保存到该路径。
/// </summary>
public static partial class Initializer
{
    const string ConfigPath = "config.toml";

    private static void PrintHello()
    {
        Console.WriteLine(@"---------------------------------------------------");
        Console.WriteLine(@"                                                   ");
        Console.WriteLine(@"         _                       _  _   __         ");
        Console.WriteLine(@"        | |                     | |(_) / _|        ");
        Console.WriteLine(@"      __| |  ___  ___  _   _    | | _ | |_  ___    ");
        Console.WriteLine(@"     / _` | / _ \/ __|| | | |   | || ||  _|/ _ \   ");
        Console.WriteLine(@"    | (_| ||  __/\__ \| |_| | _ | || || | |  __/   ");
        Console.WriteLine(@"     \__,_| \___||___/ \__,_|(_)|_||_||_|  \___|   ");
        Console.WriteLine(@"                                                   ");
        Console.WriteLine(@"                                                   ");
        Console.WriteLine(@"---------------------------------------------------");
    }

    public static async Task InitializationAsync()
    {
        // 打印欢迎信息
        PrintHello();

        // 创建Logger
        var log = new LoggerConfiguration()
        .WriteTo
        .Async(a => a.Console())
        .WriteTo
        .Async(a => a.File("logs/log-.log", rollingInterval: RollingInterval.Day));
        Log.Logger = log.CreateLogger();

        // 载入Config
        if (File.Exists(ConfigPath))
        {
            try
            {
                Config.Inner = Config.Load(ConfigPath);
            }
            catch (Tomlyn.TomlException ex)
            {
                Log.Fatal($"载入配置时出错：{ex.Message}");
                Thread.Sleep(500);
                Environment.Exit(1);
            }
            Log.Information("已载入配置");
        }
        else
        {
            Config.Inner = Config.Base.Default();
            Config.Inner.Save(ConfigPath);
            Log.Warning("没有找到配置文件，已重新生成");
        }

        // 注册指令
        Register();

        Log.Information("初始化成功 {@config}", Config.Inner.Dev ? Config.Inner : "");

        // 开发模式，使用OneBot
        if (Config.Inner.Dev) await DeveloperMode();

    }
}
