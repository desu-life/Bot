using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using KanonBot;
using KanonBot.Event;
using KanonBot.Message;
using KanonBot.Serializer;
using Serilog;
using WatsonWebsocket;

namespace KanonBot.Drivers;

public partial class OneBot
{
    public class Client : OneBot, IDriver, ISocket, IReply
    {
        WatsonWsClient instance;
        public API api;
        public string? selfID { get; private set; }

        public Client(string url)
        {
            // 初始化
            this.api = new(this);

            // 初始化ws
            var client = new WatsonWsClient(new Uri(url));
            client.ConfigureOptions(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            });

            client.Logger = (message) =>
            {
                Log.Information($"[{OneBot.platform} Client] {message}");
            };

            // 拿Tasks异步执行
            client.MessageReceived += (sender, e) =>
            {
                if (e.MessageType is not WebSocketMessageType.Text)
                {
                    return;
                }
                string message = System.Text.Encoding.UTF8.GetString(e.Data);
                Task.Run(async () =>
                {
                    try
                    {
                        await this.Parse(message);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("未捕获的异常 ↓\n{ex}", ex);
                    }
                });
            };

            client.ServerDisconnected += (sender, e) =>
            {
                // reconnect timeout
                Thread.Sleep(5000);
                Console.WriteLine("与服务器断开连接，开始重连...");
                Start();
            };

            this.instance = client;
        }

        async Task Parse(string msg)
        {
            using var doc = JsonDocument.Parse(msg);
            var root = doc.RootElement;

            if (root.TryGetProperty("echo", out _) && root.TryGetProperty("retcode", out _))
            {
                var res = Json.TryDeserialize<Models.CQResponse>(msg);

                if (res is not null)
                    await this.api.Echo(res);

                return;
            }

            if (!root.TryGetProperty("post_type", out var postTypeProp))
                return;

            var postType = postTypeProp.GetString();
            switch (postType)
            {
                case "message":
                {
                    if (msgAction is null)
                        return;

                    var messageType = root.GetProperty("message_type").GetString();
                    Models.CQMessageEventBase obj;
                    try
                    {
                        switch (messageType)
                        {
                            case "private":
                                obj = root.Deserialize<Models.PrivateMessage>(Json.Options)!;
                                break;
                            case "group":
                                obj = root.Deserialize<Models.GroupMessage>(Json.Options)!;
                                break;
                            default:
                                Log.Error("未知消息类型: {0}", messageType);
                                return;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(ex, "不支持的消息格式，请使用数组消息格式，连接断开");
                        await this.Stop();
                        return;
                    }

                    var source = MessageSource.FromOneBot(obj);

                    await msgAction.Invoke(
                        new Target
                        {
                            time = DateTimeOffset.FromUnixTimeSeconds(obj.Time),
                            platform = Platform.OneBot,
                            sender = obj.UserId.ToString(),
                            selfAccount = this.selfID,
                            msg = Message.Parse(obj.MessageList),
                            raw = obj,
                            source = source,
                            socket = this
                        }
                    );
                    return;
                }

                case "meta_event":
                {
                    if (eventAction is null)
                        return;

                    var metaType = root.TryGetProperty("meta_event_type", out var t)
                        ? t.GetString()
                        : null;

                    switch (metaType)
                    {
                        case "heartbeat":
                        {
                            var time = root.GetProperty("time").GetInt64();
                            await eventAction.Invoke(this, new HeartBeat(time));
                            return;
                        }

                        case "lifecycle":
                        {
                            this.selfID = root.GetProperty("self_id").GetRawText();
                            await eventAction.Invoke(this, new Ready(this.selfID, Platform.OneBot));
                            return;
                        }

                        default:
                        {
                            await eventAction.Invoke(this, new RawEvent(root.Clone()));
                            return;
                        }
                    }
                }

                default:
                {
                    if (eventAction is not null)
                    {
                        await eventAction.Invoke(this, new RawEvent(root.Clone()));
                    }
                    return;
                }
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
            this.instance.SendAsync(message).Wait();
        }

        public async Task SendAsync(string message)
        {
            await this.instance.SendAsync(message);
        }

        public Task Start()
        {
            return this.instance.StartAsync();
        }

        public async Task Stop()
        {
            await this.instance.StopAsync();
        }

        public async Task<bool> Reply(Target target, Chain msg)
        {
            switch (target.raw)
            {
                case OneBot.Models.GroupMessage g:
                {
                    if (!(await api.SendGroupMessage(g.GroupId, msg)).HasValue)
                    {
                        Log.Error("发送 QQ 消息失败");
                        return false;
                    }
                    break;
                }
                case OneBot.Models.PrivateMessage p:
                    if (!(await api.SendPrivateMessage(p.UserId, msg)).HasValue)
                    {
                        Log.Error("发送 QQ 消息失败");
                        return false;
                    }
                    break;
                default:
                    break;
            }
            return true;
        }
    }
}
