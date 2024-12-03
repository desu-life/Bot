using System.Net.WebSockets;
using WatsonWebsocket;
using KanonBot.Serializer;
using KanonBot.Event;
using Newtonsoft.Json.Linq;
namespace KanonBot.Drivers;
public partial class Guild : ISocket, IDriver
{
    public static readonly Platform platform = Platform.Guild;
    public string? selfID { get; private set; }
    WatsonWsClient instance;
    event IDriver.MessageDelegate? msgAction;
    event IDriver.EventDelegate? eventAction;
    public API api;
    string AuthToken;
    Guid? SessionId;
    Enums.Intent intents;
    System.Timers.Timer heartbeatTimer = new();
    int lastSeq = 0;
    public Guild(long appID, string token, Enums.Intent intents, bool sandbox = false)
    {
        // 初始化变量

        this.AuthToken = $"Bot {appID}.{token}";
        this.intents = intents;

        this.api = new(AuthToken, sandbox);

        // 获取频道ws地址

        var url = api.GetWebsocketUrl().Result;

        // 初始化ws
        var client = new WatsonWsClient(new Uri(url));
        client.ConfigureOptions(options => {
            options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            options.SetRequestHeader("Authorization", this.AuthToken);
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

    void Dispatch<T>(Models.PayloadBase<T> obj)
    {
        switch (obj.Type)
        {
            case Enums.EventType.Ready:
                var readyData = (obj.Data as JObject)?.ToObject<Models.ReadyData>();
                this.SessionId = readyData!.SessionId;
                this.selfID = readyData.User.ID;
                Log.Information("鉴权成功 {@0}", readyData);
                this.eventAction?.Invoke(this, new Ready(readyData.User.ID, Platform.Guild));
                break;
            case Enums.EventType.AtMessageCreate:
                var MessageData = (obj.Data as JObject)?.ToObject<Models.MessageData>();
                this.msgAction?.Invoke(new Target() {
                    platform = Platform.Guild,
                    sender = MessageData!.Author.ID,
                    selfAccount = this.selfID,
                    msg = Message.Parse(MessageData!),
                    raw = MessageData,
                    socket = this
                });
                break;
            case Enums.EventType.Resumed:
                // 恢复连接成功
                // 不做任何事
                break;
            default:
                this.eventAction?.Invoke(this, new RawEvent(obj));
                break;
        }
    }

    async Task Parse(string msg)
    {
        var obj = Json.Deserialize<Models.PayloadBase<JToken>>(msg)!;
        // Log.Debug("收到消息: {@0} 数据: {1}", obj, obj.Data?.ToString(Formatting.None) ?? null);

        if (obj.Seq != null)
            this.lastSeq = obj.Seq.Value;   // 存储最后一次seq

        switch (obj.Operation)
        {
            case Enums.OperationCode.Dispatch:
                this.Dispatch(obj);
                break;
            case Enums.OperationCode.Hello:
                var heartbeatInterval = (obj.Data as JObject)!["heartbeat_interval"]!.Value<int>();

                SetHeartBeatTicker(heartbeatInterval);  // 设置心跳定时器

                this.Send(this.SessionId switch {
                    null => Json.Serialize(new Models.PayloadBase<Models.IdentityData> {    // 鉴权
                    Operation = Enums.OperationCode.Identify,
                    Data = new Models.IdentityData{
                        Token = this.AuthToken,
                        Intents = this.intents,
                        Shard = [0, 1],
                    }
                    }),
                    not null => Json.Serialize(new Models.PayloadBase<Models.ResumeData> {    // 鉴权
                    Operation = Enums.OperationCode.Resume,
                    Data = new Models.ResumeData{
                        Token = this.AuthToken,
                        SessionId = this.SessionId.Value,
                        Seq = this.lastSeq,
                    }
                    })
                });
                break;
            case Enums.OperationCode.Reconnect:
                await this.instance.StartAsync();    // 重连
                break;
            case Enums.OperationCode.InvalidSession:
                await this.instance.StopAsync();
                this.Dispose();      // 销毁客户端
                throw new KanonError("无效的session，需要重新鉴权");
            case Enums.OperationCode.HeartbeatACK:
                // 无需处理
                break;
            default:
                break;
        }

    }

    void SetHeartBeatTicker(int interval)
    {
        this.heartbeatTimer = new System.Timers.Timer(interval);    // 初始化定时器
        this.heartbeatTimer.Elapsed += (s, e) =>
        {
            HeartBeatTicker();
        };
        this.heartbeatTimer.AutoReset = true;   // 设置定时器是否重复触发
        this.heartbeatTimer.Enabled = true;  // 启动定时器
    }

    void HeartBeatTicker()
    {
        // Log.Debug("Sending heartbeat..");   // log（仅测试）
        var j = Json.Serialize(new Models.PayloadBase<Models.IdentityData> {
            Operation = Enums.OperationCode.Heartbeat,
            Seq = this.lastSeq
        });

        this.Send(j);
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
