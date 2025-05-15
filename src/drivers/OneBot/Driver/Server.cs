using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WatsonWebsocket;
using KanonBot;
using KanonBot.Event;
using KanonBot.Message;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Net;

namespace KanonBot.Drivers;

public partial class OneBot
{
    public class Server : OneBot, IDriver
    {
        public class Socket : ISocket
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
                if (!isClose) this.server.SendAsync(client.Guid, message).Wait();
            }

            public async Task SendAsync(string message)
            {
                if (!isClose) await this.server.SendAsync(client.Guid, message);
            }

            public ClientMetadata ConnectionInfo => this.client;

            public void Close()
            {
                try { this.server.DisconnectClient(this.client.Guid); } catch { }
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
                if (e.MessageType is not WebSocketMessageType.Text) { return; }
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
            // Log.Debug($"[{OneBot.platform} Core] Raw: {msg}");
            var m = Json.ToLinq(msg);
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
                                // 匹配是否是bot发出，防止进入死循环
                                var user_id = (string?)m["user_id"];
                                if (this.clients.Iter().Where(s => s.Value.selfID == user_id).Any())
                                {
                                    return;
                                }
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
                                // throw new NotSupportedException($"不支持的消息格式，请使用数组消息格式");
                                Log.Error(
                                    "不支持的消息格式，请使用数组消息格式，断开来自{0}的连接",
                                    socket.ConnectionInfo
                                );
                                this.Disconnect(socket.ConnectionInfo.Guid);
                                return;
                            }
                            var target = new Target
                            {
                                time = DateTimeOffset.FromUnixTimeSeconds(obj.Time),
                                platform = Platform.OneBot,
                                sender = obj.UserId.ToString(),
                                selfAccount = socket.selfID,
                                msg = Message.Parse(obj.MessageList),
                                raw = obj,
                                socket = socket
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
                                    socket,
                                    new HeartBeat((long)m["time"]!)
                                );
                            }
                            else if (metaEventType == "lifecycle")
                            {
                                await this.eventAction.Invoke(
                                    socket,
                                    new Ready((string)m["self_id"]!, Platform.OneBot)
                                );
                            }
                            else
                            {
                                await this.eventAction.Invoke(socket, new RawEvent(m));
                            }
                        }
                        break;
                    }

                    default:
                        if (eventAction is not null)
                        {
                            await this.eventAction.Invoke(socket, new RawEvent(m));
                        }
                        break;
                }
            }
            // 处理回执消息
            if (m["echo"] != null)
            {
                await socket.api.Echo(m.ToObject<Models.CQResponse>()!);
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

        public void Dispose()
        {
            this.instance.Dispose();
        }
    }
}
