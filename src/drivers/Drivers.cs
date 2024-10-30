using KanonBot.Event;
using KanonBot.Serializer;


namespace KanonBot.Drivers;

public enum Platform
{
    Unknown,
    OneBot,
    Guild,
    KOOK,
    Discord,
    OSU
}

public interface IDriver
{
    delegate Task MessageDelegate(Target target);
    delegate Task EventDelegate(ISocket socket, IEvent kevent);
    IDriver onMessage(MessageDelegate action);
    IDriver onEvent(EventDelegate action);
    Task Start();
    void Dispose();
}
public interface ISocket
{
    string? selfID { get; }
    void Send(string message);
    Task SendAsync(string message);
    void Send(Object obj) => Send(Json.Serialize(obj));
    Task SendAsync(Object obj) => SendAsync(Json.Serialize(obj));
}

public interface IReply
{
    Task Reply(Target target, Message.Chain msg);
}

public class Drivers
{
    ManualResetEvent exitEvent = new(false);
    List<IDriver> driverList;
    public Drivers()
    {
        this.driverList = new();
    }
    public Drivers append(IDriver n)
    {
        this.driverList.Add(n);
        return this;
    }

    public void StartAll()
    {
        var tasks = driverList.Map(x => x.Start()).ToArray();
        Task.WaitAll(tasks);
        exitEvent.WaitOne();
    }

    public void StopAll()
    {
        foreach (var driver in this.driverList) {
            driver.Dispose();
        }
        exitEvent.Set();
    }
}
