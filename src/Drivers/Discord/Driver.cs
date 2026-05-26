using System.Net;
using System.Globalization;
using CommandSystem.Execution;
using Discord;
using Discord.Net.Rest;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using KanonBot.Event;
using KanonBot.Message;
using Serilog.Events;

namespace KanonBot.Drivers;

public partial class Discord : ISocket, IDriver, IReply, IPrivateReply
{
    public static readonly Platform platform = Platform.Discord;
    public delegate Task SlashCommandDelegate(Target target, string slashName, Dictionary<string, string> options);

    public string? selfID { get; private set; }
    DiscordSocketClient instance;
    event IDriver.MessageDelegate? msgAction;
    event IDriver.EventDelegate? eventAction;
    event SlashCommandDelegate? slashAction;
    string token;
    string slashMode;
    ulong[] slashGuildIds;
    bool slashRegisterOnStartup;
    bool slashRegistered;
    public API api;

    public Discord(
        string token,
        string botID,
        string? gatewayHost = null,
        string? apiBaseUrl = null,
        string slashMode = "global",
        IEnumerable<ulong>? slashGuildIds = null,
        bool slashRegisterOnStartup = false
    )
    {
        // 初始化变量
        this.token = token;
        this.selfID = botID;
        this.slashMode = slashMode;
        this.slashGuildIds = slashGuildIds?.ToArray() ?? [];
        this.slashRegisterOnStartup = slashRegisterOnStartup;

        this.api = new(token);

        var restClientProvider = DefaultRestClientProvider.Create(true);

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            WebSocketProvider = DefaultWebSocketProvider.Create(WebRequest.DefaultWebProxy),
            RestClientProvider = baseUrl => restClientProvider(string.IsNullOrWhiteSpace(apiBaseUrl) ? baseUrl : apiBaseUrl),
        };

        // 如果配置了 gateway host，则使用自定义的 gateway 地址
        if (!string.IsNullOrEmpty(gatewayHost))
        {
            config.GatewayHost = gatewayHost;
        }

        var client = new DiscordSocketClient(config);
        client.Log += LogAsync;

        // client.MessageUpdated += this.Parse;
        client.MessageReceived += msg =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Parse(msg);
                }
                catch (Exception ex)
                {
                    Log.Error("未捕获的异常 ↓\n{ex}", ex);
                }
            });
            return Task.CompletedTask;
        };

        client.SlashCommandExecuted += command =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Parse(command);
                }
                catch (Exception ex)
                {
                    Log.Error("未捕获的Discord Slash异常 ↓\n{ex}", ex);
                }
            });
            return Task.CompletedTask;
        };

        client.Ready += async () =>
        {
            await RegisterSlashCommandsOnce();
            if (this.eventAction is not null)
                await this.eventAction.Invoke(this, new Ready(this.selfID!, Platform.Discord));
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
        Log.Write(
            severity,
            message.Exception,
            "[Discord] [{Source}] {Message}",
            message.Source,
            message.Message
        );
        await Task.CompletedTask;
    }

    private async Task Parse(SocketMessage message)
    {
        if (message.Author.Id == this.instance.CurrentUser.Id)
            return;

        // 过滤掉bot消息和系统消息
        if (message is SocketUserMessage m)
        {
            if (message.Author.IsBot)
                return;
            if (this.msgAction is null)
                return;

            var ms = await m.Channel.GetMessageAsync(m.Id);
            var source = MessageSource.FromDiscord(m);
            await this.msgAction.Invoke(
                new Target()
                {
                    platform = Platform.Discord,
                    sender = m.Author.Id.ToString(),
                    selfAccount = this.selfID,
                    msg = Message.Parse(ms),
                    raw = ms,
                    source = source,
                    socket = this
                }
            );
        }
        else
        {
            if (this.eventAction is null)
                return;
            await this.eventAction.Invoke(this, new RawEvent(message));
        }
    }

    private async Task Parse(SocketSlashCommand command)
    {
        if (this.slashAction is null)
            return;

        Target? target = null;
        await command.DeferAsync();

        try
        {
            target = new Target()
            {
                platform = Platform.Discord,
                sender = command.User.Id.ToString(),
                selfAccount = this.selfID,
                msg = new Chain().msg($"/{command.Data.Name}"),
                raw = command,
                source = MessageSource.FromDiscord(command),
                socket = this,
                isFromAdmin = true
            };

            await this.slashAction.Invoke(target, command.Data.Name, FlattenOptions(command.Data.Options));
        }
        finally
        {
            if (target is null || !target.HasReplied)
                await TryDeleteOriginalResponse(command);
        }
    }

    private static Dictionary<string, string> FlattenOptions(
        IReadOnlyCollection<SocketSlashCommandDataOption> options
    )
    {
        var result = new Dictionary<string, string>();
        foreach (var option in options)
        {
            if (option.Options.Count > 0)
            {
                foreach (var (key, value) in FlattenOptions(option.Options))
                    result[key] = value;

                continue;
            }

            if (option.Value is null)
                continue;

            result[option.Name] = Convert.ToString(option.Value, CultureInfo.InvariantCulture) ?? "";
        }

        return result;
    }

    private static async Task TryDeleteOriginalResponse(SocketSlashCommand command)
    {
        try
        {
            await command.DeleteOriginalResponseAsync();
        }
        catch (Exception ex)
        {
            Log.Debug("清除Discord Slash defer状态失败 ↓\n{ex}", ex);
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

    public Discord onSlashCommand(SlashCommandDelegate action)
    {
        this.slashAction += action;
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

    public async Task<bool> Reply(Target target, Chain msg)
    {
        try
        {
            if (target.raw is SocketSlashCommand slashCommand)
            {
                await api.SendMessage(slashCommand, msg);
            }
            else
            {
                var discordRawMessage = target.raw as IMessage;
                await api.SendMessage(discordRawMessage!.Channel, msg, discordRawMessage);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("发送Discord消息失败 ↓\n{ex}", ex);
            return false;
        }
        return true;
    }

    public async Task<bool> PrivateReply(Target target, Chain msg)
    {
        try
        {
            switch (target.raw)
            {
                case SocketSlashCommand slashCommand:
                    await api.SendPrivateMessage(slashCommand.User, msg);
                    await TryDeleteOriginalResponse(slashCommand);
                    return true;
                case IMessage discordRawMessage:
                    await api.SendPrivateMessage(discordRawMessage.Author, msg);
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            Log.Warning("发送Discord私聊消息失败 ↓\n{ex}", ex);
            return false;
        }
    }

    private async Task RegisterSlashCommandsOnce()
    {
        if (!slashRegisterOnStartup || slashRegistered)
            return;

        slashRegistered = true;

        try
        {
            var registry = CommandRegistrar.BuildRegistry();
            var commands = DiscordSlashCommandBuilder.Build(registry);

            switch (slashMode.ToLowerInvariant())
            {
                case "guild":
                    if (slashGuildIds.Length == 0)
                    {
                        Log.Error("Discord Slash注册失败: slash_mode=guild 但 slash_guild_ids 为空");
                        return;
                    }

                    foreach (var guildId in slashGuildIds)
                    {
                        var guild = instance.GetGuild(guildId);
                        if (guild is null)
                        {
                            Log.Error("Discord Slash注册失败: 找不到Guild {GuildId}", guildId);
                            continue;
                        }

                        await ((IGuild)guild).BulkOverwriteApplicationCommandsAsync(commands);
                        Log.Information("Discord Slash已同步到Guild {GuildId}: {Count}条", guildId, commands.Length);
                    }
                    return;

                case "global":
                    await ((IDiscordClient)instance).BulkOverwriteGlobalApplicationCommand(commands);
                    Log.Information("Discord Slash已同步为全局指令: {Count}条", commands.Length);
                    return;

                default:
                    Log.Error("Discord Slash注册失败: 未知slash_mode {SlashMode}", slashMode);
                    return;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Discord Slash注册失败 ↓\n{ex}", ex);
        }
    }

    public async Task Start()
    {
        await this.instance.LoginAsync(TokenType.Bot, this.token);
        await this.instance.StartAsync();
    }

    public async Task Stop()
    {
        await this.instance.StopAsync();
    }
}
