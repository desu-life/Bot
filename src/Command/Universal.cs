using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using CommandSystem;
using DotNext.Threading;
using Flurl.Util;
using KanonBot.Drivers;
using KanonBot.Functions;
using KanonBot.Functions.OSU;
using KanonBot.Functions.OSUBot;
using KanonBot.I18n;
using KanonBot.Message;
using LanguageExt;
using LanguageExt.ClassInstances;
using Serilog;

namespace KanonBot.Command
{
    public static class Universal
    {
        public class ReduplicateTargetChecker
        {
            private AsyncReaderWriterLock rwlock = new();
            private Dictionary<string, Target> CommandList =  [ ];

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

        private static readonly PlatformHandler _handler = new();

        private static async Task Run(Target target, string cmd)
        {
            try
            {
                var (command, parsed) = _handler.HandleLegacy(cmd);
                if (command is null || parsed is null)
                    return;

                await command.Execute(target, parsed);
            }
            catch (Flurl.Http.FlurlHttpTimeoutException)
            {
                await target.reply(target.T("error.timeout"));
            }
            catch (Flurl.Http.FlurlHttpException ex)
            {
                await target.reply(target.T("error.network"));
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
                    await target.reply(target.T("error.file_io"));
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
                        await target.reply(target.T("error.timeout"));
                        return;
                    }
                    else if (e is Flurl.Http.FlurlHttpException)
                    {
                        await target.reply(target.T("error.network"));
                        Log.Error("获取数据异常 ↓\n{ex}", e);
                        return;
                    }
                }

                await target.reply(target.T("error.unknown"));
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
                await target.reply(target.T("error.unknown"));
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
