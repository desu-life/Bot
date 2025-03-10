using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using DotNext.Threading;
using Flurl.Util;
using KanonBot.Drivers;
using KanonBot.Functions;
using KanonBot.Functions.OSU;
using KanonBot.Functions.OSUBot;
using KanonBot.Message;
using LanguageExt;
using LanguageExt.ClassInstances;
using Serilog;

namespace KanonBot.command_parser
{
    public static class Universal
    {
        public class ReduplicateTargetChecker
        {
            private AsyncReaderWriterLock rwlock = new();
            private Dictionary<string, Target> CommandList = [];

            public async Task<bool> Lock(Target target)
            {
                using (await rwlock.AcquireWriteLockAsync(CancellationToken.None))
                {
                    return CommandList.TryAdd(target.sender!, target);
                }
            }

            public async Task<bool> Contains(Target target)
            {
                using (await rwlock.AcquireReadLockAsync(CancellationToken.None))
                {
                    // 检查消息是否已被处理并判断是否为同一条指令
                    if (CommandList.TryGetValue(target.sender!, out var value))
                    {
                        if (value.msg.Equals(target.msg))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public async Task TryUnlock(Target target)
            {
                using (await rwlock.AcquireWriteLockAsync(CancellationToken.None))
                {
                    CommandList.Remove(target.sender!, out _);
                }
            }
        }

        public static ReduplicateTargetChecker reduplicateTargetChecker = new();

        private static async Task Run(Target target, string cmd)
        {
            string rootCmd;
            string childCmd = "";
            string childCmdOriginal = "";

            try
            {
                var tmp = cmd.Split(' ', 2, StringSplitOptions.TrimEntries);
                rootCmd = tmp[0].ToDBC();
                childCmd = tmp[1].ToDBC();
                childCmdOriginal = tmp[1];
            }
            catch
            {
                rootCmd = cmd.ToDBC();
            }

            try
            {
                switch (rootCmd.ToLower()) // 不区分大小写
                {
                    case "reg":
                        await Accounts.RegAccount(target, childCmd);
                        return;
                    case "bind":
                        await Accounts.BindService(target, childCmd);
                        return;
                    case "info":
                        await Info.Execute(target, childCmd);
                        return;
                    case "sc":
                    case "search":
                        await Search.Execute(target, childCmdOriginal);
                        return;
                    case "res":
                        await RecentList.Execute(target, childCmd, true);
                        return;
                    case "prs":
                        await RecentList.Execute(target, childCmd, false);
                        return;
                    case "recent":
                    case "re":
                        await Recent.Execute(target, childCmd, true);
                        return;
                    case "pr":
                        await Recent.Execute(target, childCmd, false);
                        return;
                    case "bp":
                        await BestPerformance.Execute(target, childCmd);
                        return;
                    case "score":
                        await Score.Execute(target, childCmd);
                        return;
                    case "pp":
                        await Score.Execute(target, childCmd, true);
                        return;
                    case "help":
                        await Help.Execute(target, childCmd);
                        return;
                    case "ping":
                        await Ping.Execute(target);
                        return;
                    case "update":
                        await Update.Execute(target, childCmd);
                        return;
                    case "get":
                        await Get.Execute(target, childCmd);
                        return; // get bonuspp/elo/rolecost/bpht/todaybp/annualpass
                    case "todaybp":
                        await TodayBP.Execute(target, childCmd);
                        return;
                    case "badge":
                        await Badge.Execute(target, childCmd);
                        return;
                    case "leeway":
                    case "lc":
                        await Leeway.Execute(target, childCmd);
                        return;
                    case "set":
                        await Setter.Execute(target, childCmd);
                        return;
                    case "ppvs":
                        await PPvs.Execute(target, childCmd);
                        return;
                    case "cat":
                        await ChatBot.Execute(target, childCmd);
                        return;
                    //管理员
                    case "sudo":
                        await Sudo.Execute(target, childCmd);
                        return;
                    //超级管理员
                    case "su":
                        await Su.Execute(target, childCmd);
                        return;
                }

                // 有些例外，用StartsWith匹配
                // if (rootCmd.StartsWith("bp"))
                // {
                //     string numberPart = cmd[2..];
                //     if (!string.IsNullOrEmpty(numberPart) && int.TryParse(numberPart, out int number))
                //     {
                //         await BestPerformance.Execute(target, cmd[2..].Trim());
                //         return;
                //     }                    
                    
                // }

                if (cmd.StartsWith("bp"))
                {
                    // 防止和某抽象bot冲突
                    if (cmd.StartsWith("bpa"))
                    {
                        return;
                    }

                    // 修复白菜bpme兼容
                    if (cmd.StartsWith("bpme"))
                    {
                        return;
                    }

                    if (cmd.StartsWith("bplist"))
                    {
                        return;
                    }

                    await BestPerformance.Execute(target, cmd[2..].Trim());
                    return;
                }
            }
            catch (Flurl.Http.FlurlHttpTimeoutException)
            {
                await target.reply("获取数据超时，请稍后重试吧");
            }
            catch (Flurl.Http.FlurlHttpException ex)
            {
                await target.reply("获取数据时出错，之后再试试吧");
                var rtmp = $"""
                    网络异常
                    Target Platform: {target.platform}
                    Target User: {target.sender}
                    Target Message: {target.msg}
                    Exception: {ex}
                    """;
                await Utils.SendDebugMail(rtmp);
                Log.Error("网络异常 ↓\n{ex}", ex);
            }
            catch (System.IO.IOException ex)
            {
                // 文件竞争问题, 懒得处理了直接摆烂
                if (ex.Message.Contains("being used by another process"))
                {
                    Log.Error("出现文件竞争问题 ↓\n{ex}", ex);
                }
                else
                {
                    await target.reply("文件操作异常，错误内容已自动上报");
                    var rtmp = $"""
                        文件操作异常
                        Target Platform: {target.platform}
                        Target User: {target.sender}
                        Target Message: {target.msg}
                        Exception: {ex}
                        """;
                    await Utils.SendDebugMail(rtmp);
                    Log.Error("文件操作异常 ↓\n{ex}", ex);
                }
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is Flurl.Http.FlurlHttpTimeoutException)
                    {
                        await target.reply("获取数据超时，请稍后重试吧");
                        return;
                    }
                    else if (e is Flurl.Http.FlurlHttpException)
                    {
                        await target.reply("获取数据时出错，之后再试试吧");
                        Log.Error("获取数据异常 ↓\n{ex}", e);
                        return;
                    }
                }

                await target.reply("出现了未知错误，错误内容已自动上报");
                var rtmp = $"""
                    未知异常
                    Target Platform: {target.platform}
                    Target User: {target.sender}
                    Target Message: {target.msg}
                    Exception: {ae}
                    """;
                await Utils.SendDebugMail(rtmp);
                Log.Error("执行指令异常 ↓\n{ex}", ae);
            }
            catch (Exception ex)
            {
                await target.reply("出现了未知错误，错误内容已自动上报");
                var rtmp = $"""
                    未知异常
                    Target Platform: {target.platform}
                    Target User: {target.sender}
                    Target Message: {target.msg}
                    Exception: {ex}
                    """;
                await Utils.SendDebugMail(rtmp);
                Log.Error("执行指令异常 ↓\n{ex}", ex);
            }
        }

        public static async Task Parser(Target target)
        {
            // 解析之前先确认是否有等待的消息
            foreach (var (t, cw) in Target.Waiters.Value)
            {
                if (t.platform == target.platform && t.sender == target.sender)
                {
                    await cw.WriteAsync(target);
                    return;
                }
            }

            var msg = target.msg;

            if (msg.StartsWith("!") || msg.StartsWith("！"))
            {
                // 检测相同指令重复
                if (await reduplicateTargetChecker.Contains(target))
                {
                    return;
                }

                await reduplicateTargetChecker.Lock(target);
                var cmd = msg.Build();
                cmd = cmd[1..]; //删除命令唤起符

                if (!string.IsNullOrEmpty(cmd))
                {
                    await Run(target, cmd);
                }

                await reduplicateTargetChecker.TryUnlock(target);
            }
        }
    }
}
