using System.Net;
using Discord;
using Discord.Net.Rest;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using KanonBot.Event;
using Serilog.Events;

namespace KanonBot.Drivers;
public partial class Discord : ISocket, IDriver
{
    public static readonly Platform platform = Platform.Discord;
    public string? selfID { get; private set; }
    DiscordSocketClient instance;
    event IDriver.MessageDelegate? msgAction;
    event IDriver.EventDelegate? eventAction;
    string token;
    public API api;
    public Discord(string token, string botID)
    {
        // 初始化变量
        this.token = token;
        this.selfID = botID;

        this.api = new(token);

        var client = new DiscordSocketClient(
            new() {
                WebSocketProvider = DefaultWebSocketProvider.Create(WebRequest.DefaultWebProxy),
                RestClientProvider = DefaultRestClientProvider.Create(true),
            }
        );
        client.Log += LogAsync;

        // client.MessageUpdated += this.Parse;
        client.MessageReceived += msg => {
            Task.Run(async () => {
                try
                {
                    await this.Parse(msg);
                }
                catch (Exception ex) { Log.Error("未捕获的异常 ↓\n{ex}", ex); }
            });
            return Task.CompletedTask;
        };
        
        client.Ready += () =>
        {
            // 连接成功
            return Task.CompletedTask;
        };

        this.instance = client;
    }
    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        Log.Write(severity, message.Exception, "[Discord] [{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }


    private async Task Parse(SocketMessage message)
    {
        if (message.Author.Id == this.instance.CurrentUser.Id)
            return;

        // 过滤掉bot消息和系统消息
        if (message is SocketUserMessage m)
        {
            if (message.Author.IsBot) return;
            if (this.msgAction is null) return;
            
            var ms = await m.Channel.GetMessageAsync(m.Id);
            await this.msgAction.Invoke(new Target()
            {
                platform = Platform.Discord,
                sender = m.Author.Id.ToString(),
                selfAccount = this.selfID,
                msg = Message.Parse(ms),
                raw = ms,
                socket = this
            });
        }
        else
        {
            if (this.eventAction is null) return;
            await this.eventAction.Invoke(
                this,
                new RawEvent(message)
            );
        }
    }


    public IDriver onMessage(IDriver.MessageDelegate action)
    {
        this.msgAction += action;
        return this;
    }
    public IDriver onEvent(IDriver.EventDelegate action)
    {
        this.eventAction += action;
        return this;
    }

    public void Send(string message)
    {
        throw new NotSupportedException("不支持");
    }
    
    public Task SendAsync(string message)
    {
        throw new NotSupportedException("不支持");
    }

    public async Task Start()
    {
        await this.instance.LoginAsync(TokenType.Bot, this.token);
        await this.instance.StartAsync();
    }

    public void Dispose()
    {
        this.instance.Dispose();
    }
}
