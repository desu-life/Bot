using desu.life_Bot.Serializer;
using desu.life_Bot.Event;

namespace desu.life_Bot.Drivers;

public enum Platform
{
    Unknown,
    OneBot,
    QQGuild,
    KOOK,
    Discord,
    OSU
}

public interface IDriver
{
    delegate void MessageDelegate(Target target);
    delegate void EventDelegate(ISocket socket, IEvent kevent);
    IDriver onMessage(MessageDelegate action);
    IDriver onEvent(EventDelegate action);
    Task Start();
    void Dispose();
}

public interface ISocket
{
    string? selfID { get; }
    void Send(string message);
    void Send(Object obj) => Send(Json.Serialize(obj));
}

public interface IReply
{
    void Reply(Target target, Message.Chain msg);
}

public class Drivers
{
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

    public Drivers StartAll()
    {
        foreach (var driver in this.driverList)
            driver.Start();
        return this;
    }

    public void StopAll()
    {
        foreach (var driver in this.driverList)
            driver.Dispose();
    }
}
