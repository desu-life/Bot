using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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
    public class Server : OneBot, IDriver
    {
        public class Socket : ISocket, IReply
        {
            public API api;
            WatsonWsServer server;
            ClientMetadata client;
            public string selfID { get; init; }

            private bool isClose;

            public Socket(WatsonWsServer server, ClientMetadata client, string selfID)
            {
                this.api = new(this);
                this.server = server;
                this.client = client;
                this.selfID = selfID;
                this.isClose = false;
            }

            public void Send(string message)
            {
                if (!isClose)
                    this.server.SendAsync(client.Guid, message).Wait();
            }

            public async Task SendAsync(string message)
            {
                if (!isClose)
                    await this.server.SendAsync(client.Guid, message);
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

            public ClientMetadata ConnectionInfo => this.client;

            public void Close()
            {
                try
                {
                    this.server.DisconnectClient(this.client.Guid);
                }
                catch { }
                isClose = true;
            }
        }

        class Clients
        {
            private Dictionary<Guid, Socket> inner = new();
            private Mutex mut = new();

            public Socket? Get(Guid k)
            {
                return this.inner.GetValueOrDefault(k);
            }

            public void Set(Guid k, Socket v)
            {
                mut.WaitOne();
                this.inner.Add(k, v);
                mut.ReleaseMutex();
            }

            public Socket? Remove(Guid k)
            {
                mut.WaitOne();
                this.inner.Remove(k, out Socket? s);
                mut.ReleaseMutex();
                return s;
            }

            public IEnumerable<KeyValuePair<Guid, Socket>> Iter()
            {
                return this.inner;
            }
        }

        Clients clients = new();
        WatsonWsServer instance;

        public Server(IEnumerable<string> hosts, int port, bool ssl = false)
        {
            this.instance = new WatsonWsServer(hosts.ToList(), port, ssl)
            {
                Logger = message =>
                {
                    Log.Information($"[{OneBot.platform} Core] {message}");
                },

                // HttpHandler = ctx =>
                // {
                //     byte[] data = Encoding.ASCII.GetBytes("返回什么");
                //     ctx.Response.Close(data, true);
                // }
            };

            // 绑定事件
            this.instance.ClientConnected += (sender, e) =>
            {
                var role = e.HttpRequest.Headers.Get("X-Client-Role");
                if (role is null)
                {
                    instance.DisconnectClient(e.Client.Guid);
                    return;
                }

                if (role != "Universal")
                {
                    instance.DisconnectClient(e.Client.Guid);
                    return;
                }

                var selfID = e.HttpRequest.Headers.Get("X-Self-ID");
                if (selfID is null)
                {
                    instance.DisconnectClient(e.Client.Guid);
                    return;
                }

                this.clients.Set(e.Client.Guid, new Socket(this.instance, e.Client, selfID));
            };

            this.instance.ClientDisconnected += (sender, e) =>
            {
                var s = this.clients.Remove(e.Client.Guid);
                s?.Close();
            };

            this.instance.MessageReceived += (sender, e) =>
            {
                if (e.MessageType is not WebSocketMessageType.Text)
                {
                    return;
                }
                string message = Encoding.UTF8.GetString(e.Data);
                Task.Run(async () =>
                {
                    try
                    {
                        var s = this.clients.Get(e.Client.Guid)!;
                        await this.Parse(message, s);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("未捕获的异常 ↓\n{ex}", ex);
                        this.Disconnect(e.Client.Guid);
                    }
                });
            };
        }

        async Task Parse(string msg, Socket socket)
        {
            using var doc = JsonDocument.Parse(msg);
            var root = doc.RootElement;

            if (root.TryGetProperty("echo", out _) && root.TryGetProperty("retcode", out _))
            {
                var res = Json.TryDeserialize<Models.CQResponse>(msg);

                if (res is not null)
                    await socket.api.Echo(res);

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

                    var userId = root.GetProperty("user_id").GetRawText();
                    if (clients.Iter().Any(s => s.Value.selfID == userId))
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
                    catch (JsonException)
                    {
                        // throw new NotSupportedException($"不支持的消息格式，请使用数组消息格式");
                        Log.Error("不支持的消息格式，请使用数组消息格式，断开来自{0}的连接", socket.ConnectionInfo);
                        this.Disconnect(socket.ConnectionInfo.Guid);
                        return;
                    }

                    var source = MessageSource.FromOneBot(obj);

                    await msgAction.Invoke(
                        new Target
                        {
                            time = DateTimeOffset.FromUnixTimeSeconds(obj.Time),
                            platform = Platform.OneBot,
                            sender = obj.UserId.ToString(),
                            selfAccount = socket.selfID,
                            msg = Message.Parse(obj.MessageList),
                            raw = obj,
                            source = source,
                            socket = socket
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
                            await eventAction.Invoke(socket, new HeartBeat(time));
                            return;
                        }

                        case "lifecycle":
                        {
                            await eventAction.Invoke(
                                socket,
                                new Ready(socket.selfID, Platform.OneBot)
                            );
                            return;
                        }

                        default:
                        {
                            await eventAction.Invoke(socket, new RawEvent(root.Clone()));
                            return;
                        }
                    }
                }

                default:
                {
                    if (eventAction is not null)
                    {
                        await eventAction.Invoke(socket, new RawEvent(root.Clone()));
                    }
                    return;
                }
            }
        }

        void Disconnect(Guid id)
        {
            var s = this.clients.Remove(id);
            s?.Close();
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

        public Task Start()
        {
            return instance.StartAsync();
        }

        public async Task Stop()
        {
            this.instance.Stop();
        }
    }
}
