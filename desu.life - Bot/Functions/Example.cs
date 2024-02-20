using desu_life_Bot.Command;
using desu_life_Bot.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace desu_life_Bot.Functions;

public static partial class Example
{
    // 该命令格式为 !ping arg1或arg1_1=value1 arg2=value2
    [Command("example")]
    [Params("arg1", "arg1_1", "arg2")]
    public static async Task ExampleFunction(CommandContext args, Target target)
    {
        string arg1 = "", arg2 = "";
        args.GetParameters<string>(["arg1", "arg1_1"]).IfSome(_arg1 => arg1 = _arg1);

        Log.Information($"[example] 方式一：{arg1}");

        // 另一种方式
        args.GetParameters<string>(["arg2"])
                .Match(
                    Some: _arg2 =>
                    {
                        arg2 = _arg2;
                    },
                    None: () => { }
                );
        Log.Information($"[example] 方式二：{arg2}");
        await Task.CompletedTask;
    }

    [Command("ping")]
    public static async Task Ping(CommandContext args, Target target)
    {
        string? ping = null;
        args.GetDefault<string>().IfSome(_ping => ping = _ping);
        Log.Information($"[ping] ：{((!string.IsNullOrEmpty(ping)) ? ping : "echo")}");
        await Task.CompletedTask;
    }
}
