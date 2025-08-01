using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using KanonBot;
using KanonBot.Event;
using KanonBot.Message;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Serilog;
using WatsonWebsocket;

namespace KanonBot.Drivers;

public partial class OneBot
{
    public class Client : OneBot, IDriver, ISocket
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
            client.ConfigureOptions(options => {
                options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            });

            client.Logger = (message) => { 
                Log.Information($"[{OneBot.platform} Client] {message}");
            };

            // 拿Tasks异步执行
            client.MessageReceived += (sender, e) => {
                if (e.MessageType is not WebSocketMessageType.Text) { return; }
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

            client.ServerDisconnected += (sender, e) => {
                // reconnect timeout
                Thread.Sleep(5000);
                Console.WriteLine("与服务器断开连接，开始重连...");
                Start();
            };

            this.instance = client;
        }

        async Task Parse(string msg)
        {
            var m = Json.ToLinq(msg);
            if (m != null)
            {
                if (m["post_type"] != null)
                {
                    switch ((string?)m["post_type"])
                    {
                        case "message":
                        {
                            if (msgAction is not null)
                            {
                                Models.CQMessageEventBase obj;
                                try
                                {
                                    var msgType = (string?)m["message_type"];
                                    switch (msgType)
                                    {
                                        case "private":
                                            obj = m.ToObject<Models.PrivateMessage>()!;
                                            break;
                                        case "group":
                                            obj = m.ToObject<Models.GroupMessage>()!;
                                            break;
                                        default:
                                            Log.Error("未知消息格式, {0}", msgType);
                                            return;
                                    }
                                }
                                catch (JsonSerializationException)
                                {
                                    Log.Error("不支持的消息格式，请使用数组消息格式，连接断开");
                                    this.Dispose();
                                    return;
                                }
                                var target = new Target
                                {
                                    time = DateTimeOffset.FromUnixTimeSeconds(obj.Time),
                                    platform = Platform.OneBot,
                                    sender = obj.UserId.ToString(),
                                    selfAccount = this.selfID,
                                    msg = Message.Parse(obj.MessageList),
                                    raw = obj,
                                    socket = this
                                };
                                await msgAction.Invoke(target);
                            }
                            break;
                        }
                        case "meta_event":
                        {
                            if (eventAction is not null)
                            {
                                var metaEventType = (string?)m["meta_event_type"];
                                if (metaEventType == "heartbeat")
                                {
                                    await this.eventAction.Invoke(
                                        this,
                                        new HeartBeat((long)m["time"]!)
                                    );
                                }
                                else if (metaEventType == "lifecycle")
                                {
                                    this.selfID = (string)m["self_id"]!;
                                    await this.eventAction.Invoke(
                                        this,
                                        new Ready(this.selfID, Platform.OneBot)
                                    );
                                }
                                else
                                {
                                    await this.eventAction.Invoke(this, new RawEvent(m));
                                }
                            }
                            break;
                        }
                        default:
                            if (eventAction is not null)
                            {
                                await this.eventAction.Invoke(this, new RawEvent(m));
                            }
                            break;
                    }
                }
                // 处理回执消息
                if (m["echo"] != null)
                {
                    await this.api.Echo(m.ToObject<Models.CQResponse>()!);
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

        public void Dispose()
        {
            this.instance.Dispose();
        }
    }
}
