using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using desu_life_Bot.Drivers;
using desu_life_Bot.Event;
using static desu_life_Bot.Command.CommandSystem;
using Msg = desu_life_Bot.Message;

namespace desu_life_Bot.Core;

public static class ServiceManager
{
    // todo: ReduplicateTargetChecker 还需要修改
    public static void RunService()
    {
        var ExitEvent = new ManualResetEvent(false);
        var drivers = new Drivers.Drivers()
            .append(
                new OneBot.Server($"ws://0.0.0.0:{Config.Inner!.Onebot?.ServerPort}")
                    .onMessage(
                        async (target) =>
                        {
                            var api = (target.socket as OneBot.Server.Socket)!.api;
                            Log.Information("← 收到OneBot用户 {0} 的消息 {1}", target.sender, target.msg);
                            Log.Debug("↑ OneBot详情 {@0}", target.raw!);
                            try
                            {
                                await ProcessCommand(target);
                            }
                            finally
                            {
                                //Universal.reduplicateTargetChecker.TryUnlock(target);
                            }
                        }
                    )
                    .onEvent(
                        (client, e) =>
                        {
                            switch (e)
                            {
                                case HeartBeat h:
                                    Log.Debug("收到OneBot心跳包 {h}", h);
                                    break;
                                case Ready l:
                                    Log.Debug("收到OneBot生命周期事件 {h}", l);
                                    break;
                                case RawEvent r:
                                    Log.Debug("收到OneBot事件 {r}", r);
                                    break;
                                default:
                                    break;
                            }
                        }
                    )
            )
            .StartAll();
        ExitEvent.WaitOne();
    }
}
