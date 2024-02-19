using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace desu.life_Bot;

/// <summary>
/// 初始化应用程序配置，通过从预定义路径加载配置信息。
/// 如果指定路径上不存在配置文件，则创建一个默认配置并保存到该路径。
/// </summary>
public static partial class ConfigInitializer
{
    const string ConfigPath = "config.toml";

    public static void InitializeConfig()
    {
        if (File.Exists(ConfigPath))
        {
            Config.Inner = Config.Load(ConfigPath);
        }
        else
        {
            Config.Inner = Config.Base.Default();
            Config.Inner.Save(ConfigPath);
        }
























        // Log.Information("初始化成功 {@config}", config);
    }
}
