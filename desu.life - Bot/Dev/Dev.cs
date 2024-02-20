using desu.life_Bot.Drivers;
using static desu.life_Bot.Command.CommandSystem;
using Msg = desu.life_Bot.Message;
using LanguageExt.UnsafeValueAccess;

namespace desu.life_Bot;

public static partial class Dev
{
    public static async Task DeveloperMode()
    {
        var sender = parseInt(Environment.GetEnvironmentVariable("KANONBOT_TEST_USER_ID"));
        sender.IfNone(() =>
        {
            Log.Error("未设置测试环境变量 KANONBOT_TEST_USER_ID");
            Thread.Sleep(500);
            Environment.Exit(1);
        });

        while (true)
        {
            Log.Warning("请输入消息: ");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                return;
            Log.Warning("解析消息: {0}", input);
            await ProcessCommand(
                new Target()
                {
                    msg = new Msg.Chain().msg(input!.Trim()),
                    sender = $"{sender.Value()}",
                    platform = Platform.OneBot,
                    selfAccount = null,
                    socket = new FakeSocket()
                    {
                        action = (msg) =>
                        {
                            Log.Information("本地测试消息 {0}", msg);
                        }
                    },
                    raw = new OneBot.Models.CQMessageEventBase() { UserId = sender.Value(), }
                }
            );
        }
    }
}
