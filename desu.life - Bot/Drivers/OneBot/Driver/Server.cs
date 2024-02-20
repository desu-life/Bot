using Fleck;
using desu_life_Bot.Event;
using desu_life_Bot.Serializer;
using Newtonsoft.Json;

namespace desu_life_Bot.Drivers;

public partial class OneBot
{
    public class Server : OneBot, IDriver
    {
        public class Socket : ISocket
        {
            public API api;
            IWebSocketConnection inner;
            public string selfID { get; private set; }

            public Socket(IWebSocketConnection socket)
            {
                api = new(this);
                inner = socket;
                selfID = socket.ConnectionInfo.Headers["X-Self-ID"];
            }

            public void Send(string message)
            {
                inner.Send(message);
            }

            public IWebSocketConnectionInfo ConnectionInfo()
            {
                return inner.ConnectionInfo;
            }

            public void Close()
            {
                inner.Close();
            }
        }

        class Clients
        {
            private Dictionary<Guid, Socket> inner = new();
            private Mutex mut = new();

            public Socket? Get(Guid k)
            {
                return inner.GetValueOrDefault(k);
            }

            public void Set(Guid k, Socket v)
            {
                mut.WaitOne();
                inner.Add(k, v);
                mut.ReleaseMutex();
            }

            public Socket? Remove(Guid k)
            {
                mut.WaitOne();
                inner.Remove(k, out Socket? s);
                mut.ReleaseMutex();
                return s;
            }

            public IEnumerable<KeyValuePair<Guid, Socket>> Iter()
            {
                return inner;
            }
        }

        Clients clients = new();
        WebSocketServer instance;

        public Server(string url)
        {
            var server = new WebSocketServer(url) { RestartAfterListenError = true };
            Fleck.FleckLog.LogAction = (level, message, ex) =>
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        // 不要debug
                        // Log.Debug($"[{OneBot.platform} Core] {message}", ex);
                        break;
                    case LogLevel.Error:
                        Log.Error($"[{OneBot.platform} Core] {message}", ex);
                        break;
                    case LogLevel.Warn:
                        Log.Warning($"[{OneBot.platform} Core] {message}", ex);
                        break;
                    default:
                        Log.Information($"[{OneBot.platform} Core] {message}", ex);
                        break;
                }
            };
            instance = server;
        }

        void SocketAction(IWebSocketConnection socket)
        {
            // 获取请求头数据
            // 数据验证失败后直接断开链接

            if (!socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role", out string? role))
            {
                socket.Close();
                return;
            }

            if (role != "Universal")
            {
                socket.Close();
                return;
            }

            socket.OnError = (e) =>
            {
                Disconnect(socket.ConnectionInfo.Id);
                Log.Error(
                    $"[{OneBot.platform} Core] 与 {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort} 的连接异常断开"
                );
            };
            socket.OnOpen = () =>
            {
                clients.Set(socket.ConnectionInfo.Id, new Socket(socket));
                Log.Information(
                    $"[{OneBot.platform} Core] 已建立来自 {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort} 的连接"
                );
            };
            socket.OnClose = () =>
            {
                Disconnect(socket.ConnectionInfo.Id);
                Log.Information(
                    $"[{OneBot.platform} Core] 与 {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort} 的连接已断开"
                );
            };
            socket.OnMessage = message =>
                Task.Run(() =>
                {
                    try
                    {
                        var s = clients.Get(socket.ConnectionInfo.Id);
                        if (s != null)
                            Parse(message, s);
                        else
                            socket.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("未捕获的异常 ↓\n{ex}", ex);
                        Disconnect(socket.ConnectionInfo.Id);
                    }
                });
        }

        void Parse(string msg, Socket socket)
        {
            var m = Json.ToLinq(msg);
            if (m != null)
            {
                if (m["post_type"] != null)
                {
                    switch ((string?)m["post_type"])
                    {
                        case "message":
                            Models.CQMessageEventBase obj;
                            try
                            {
                                // 匹配是否是bot发出，防止进入死循环
                                var user_id = (string?)m["user_id"];
                                if (clients.Iter().Where(s => s.Value.selfID == user_id).Any())
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
                                Log.Error(
                                    "不支持的消息格式，请使用数组消息格式，断开来自{0}的连接",
                                    socket.ConnectionInfo().ClientIpAddress
                                );
                                Disconnect(socket.ConnectionInfo().Id);
                                return;
                            }
                            var target = new Target
                            {
                                platform = Platform.OneBot,
                                sender = obj.UserId.ToString(),
                                selfAccount = socket.selfID,
                                msg = Message.Parse(obj.MessageList),
                                raw = obj,
                                socket = socket
                            };
                            msgAction?.Invoke(target);
                            break;

                        case "meta_event":
                            var metaEventType = (string?)m["meta_event_type"];
                            if (metaEventType == "heartbeat")
                            {
                                eventAction?.Invoke(socket, new HeartBeat((long)m["time"]!));
                            }
                            else if (metaEventType == "lifecycle")
                            {
                                eventAction?.Invoke(
                                    socket,
                                    new Ready((string)m["self_id"]!, Platform.OneBot)
                                );
                            }
                            else
                            {
                                eventAction?.Invoke(socket, new RawEvent(m));
                            }
                            break;

                        default:
                            eventAction?.Invoke(socket, new RawEvent(m));
                            break;
                    }
                }
                // 处理回执消息
                if (m["echo"] != null)
                {
                    socket.api.Echo(m.ToObject<Models.CQResponse>()!);
                }
            }
        }

        void Disconnect(Guid id)
        {
            var s = clients.Remove(id);
            s?.Close();
        }

        public IDriver onMessage(IDriver.MessageDelegate action)
        {
            msgAction += action;
            return this;
        }

        public IDriver onEvent(IDriver.EventDelegate action)
        {
            eventAction += action;
            return this;
        }

        public Task Start()
        {
            return Task.Run(() => instance.Start(SocketAction));
        }

        public void Dispose()
        {
            instance.Dispose();
        }
    }
}