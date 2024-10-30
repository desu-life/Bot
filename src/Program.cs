using System.IO;
using Destructurama;
using Flurl.Http.Newtonsoft;
using KanonBot.command_parser;
using KanonBot.Drivers;
using KanonBot.Event;
using KanonBot.Functions.OSU;
using KanonBot.Serializer;
using LanguageExt.ClassInstances.Pred;
using LanguageExt.UnsafeValueAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RosuPP;
using SixLabors.ImageSharp.Diagnostics;
using API = KanonBot.API;
using Msg = KanonBot.Message;

#region 初始化
Console.WriteLine("---KanonBot---");
var configPath = "config.toml";
if (File.Exists(configPath))
{
    Config.inner = Config.load(configPath);
}
else
{
    Config.inner = Config.Base.Default();
    Config.inner.save(configPath);
}

FlurlHttp
    .Clients.UseNewtonsoft()
    .WithDefaults(c =>
    {
        c.Settings.Redirects.Enabled = true;
        c.Settings.Redirects.MaxAutoRedirects = 10;
        c.Settings.Redirects.ForwardAuthorizationHeader = true;
        c.Settings.Redirects.AllowSecureToInsecure = true;
    });

var config = Config.inner;

if (config.dev)
{
    var log = new LoggerConfiguration().WriteTo.Async(a => a.Console());
    log = log.MinimumLevel.Debug();
    Log.Logger = log.CreateLogger();
}
else
{
    var log = new LoggerConfiguration()
        .Destructure.UsingAttributes()
        .WriteTo.Async(a => a.Console())
        .WriteTo.Async(a => a.File("logs/log-.log", rollingInterval: RollingInterval.Day));
    if (config.debug)
    {
        log = log.MinimumLevel.Debug();
    }
    else
    {
        log = log.MinimumLevel.Information();
    }
    Log.Logger = log.CreateLogger();
}
Log.Information("初始化成功 {@config}", config);

if (config.dev)
{
    var sender = parseInt(Environment.GetEnvironmentVariable("KANONBOT_TEST_QQ_ID"));
    sender.IfNone(() =>
    {
        Log.Error("未设置测试环境变量 KANONBOT_TEST_QQ_ID");
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
        var target = new Target()
        {
            msg = new Msg.Chain().msg(input.Trim()),
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
            raw = new OneBot.Models.CQMessageEventBase() { UserId = sender.Value(), },
            isFromAdmin = true
        };
        _ = Task.Run(async () => await Universal.Parser(target));
        await Universal.reduplicateTargetChecker.TryUnlock(target);
    }
}

Log.Information("注册用户数据更新事件");
GeneralUpdate.DailyUpdate();

//这个东西很占资源，先注释了
//MemoryDiagnostics.UndisposedAllocation += allocationStackTrace =>
//{
//    Log.Warning($@"Undisposed allocation detected at:{Environment.NewLine}{allocationStackTrace}");
//};

#endregion


var drivers = new Drivers();
foreach (var driverConfig in config.drivers)
{
    Log.Information("启动驱动 {@0}", driverConfig.Config);
    try
    {
        drivers.append(
            driverConfig.Config switch
            {
                Config.OneBotServer c
                    => new OneBot.Server($"ws://{c.host}:{c.port}")
                        .onMessage(
                            async (target) =>
                            {
                                var api = (target.socket as OneBot.Server.Socket)!.api;
                                Log.Information(
                                    "← [{0}] 收到OneBot用户 {1} 的消息 {2}",
                                    c.elevated ? "提权" : "用户",
                                    target.sender,
                                    target.msg
                                );
                                Log.Debug("↑ OneBot详情 {@0}", target.raw!);
                                try
                                {
                                    target.isFromAdmin = c.elevated;
                                    await Universal.Parser(target);
                                }
                                finally
                                {
                                    await Universal.reduplicateTargetChecker.TryUnlock(target);
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
                                }
                                return Task.CompletedTask;
                            }
                        ),
                Config.OneBotClient c
                    => new OneBot.Client($"ws://{c.host}:{c.port}")
                        .onMessage(
                            async (target) =>
                            {
                                var api = (target.socket as OneBot.Client)!.api;
                                Log.Information(
                                    "← 收到OneBot用户 {0} 的消息 {1}",
                                    target.sender,
                                    target.msg
                                );
                                Log.Debug("↑ OneBot详情 {@0}", target.raw!);
                                try
                                {
                                    target.isFromAdmin = true;
                                    await Universal.Parser(target);
                                }
                                finally
                                {
                                    await Universal.reduplicateTargetChecker.TryUnlock(target);
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
                                }
                                return Task.CompletedTask;
                            }
                        ),
                Config.Guild c
                    => new Guild(
                        c.appID,
                        c.token!,
                        Guild.Enums.Intent.GuildAtMessage | Guild.Enums.Intent.DirectMessages,
                        c.sandbox
                    )
                        .onMessage(
                            async (target) =>
                            {
                                var api = (target.socket as Guild)!.api;
                                var messageData = (target.raw as Guild.Models.MessageData)!;
                                Log.Information("← 收到QQ频道消息 {0}", target.msg);
                                Log.Debug("↑ QQ频道详情 {@0}", messageData);
                                Log.Debug("↑ QQ频道附件 {@0}", Json.Serialize(messageData.Attachments));
                                try
                                {
                                    target.isFromAdmin = true;
                                    await Universal.Parser(target);
                                }
                                catch (Flurl.Http.FlurlHttpException ex)
                                {
                                    Log.Error("请求 API 时发生异常<QQ Guild>，{0}", ex);
                                    await target.reply("请求 API 时发生异常");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("发生未知错误<QQ Guild>，{0}", ex);
                                    await target.reply("发生未知错误");
                                }
                                finally
                                {
                                    await Universal.reduplicateTargetChecker.TryUnlock(target);
                                }
                            }
                        )
                        .onEvent(
                            (client, e) =>
                            {
                                switch (e)
                                {
                                    case RawEvent r:
                                        var data = (r.value as Guild.Models.PayloadBase<JToken>)!;
                                        Log.Debug(
                                            "收到QQ Guild事件: {@0} 数据: {1}",
                                            data,
                                            data.Data?.ToString(Formatting.None) ?? null
                                        );
                                        break;
                                    case Ready l:
                                        Log.Debug("收到QQ Guild生命周期事件 {h}", l);
                                        break;
                                }
                                return Task.CompletedTask;
                            }
                        ),

                Config.KOOK c
                    => new KanonBot.Drivers.Kook(c.token!, c.botID!)
                        .onMessage(
                            async (target) =>
                            {
                                try
                                {
                                    target.isFromAdmin = true;
                                    await Universal.Parser(target);
                                }
                                finally
                                {
                                    await Universal.reduplicateTargetChecker.TryUnlock(target);
                                }
                            }
                        )
                        .onEvent(
                            (client, e) =>
                            {
                                switch (e)
                                {
                                    case Ready l:
                                        Log.Debug("收到KOOK生命周期事件 {h}", l);
                                        break;
                                }
                                return Task.CompletedTask;
                            }
                        ),
                _ => throw new NotImplementedException()
            }
        );
    }
    catch (Exception e)
    {
        Log.Error(e, "启动驱动失败: {0}", e.Message);
        continue;
    }
}

drivers.StartAll();
Log.CloseAndFlush();
