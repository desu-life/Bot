namespace KanonBot.Message;

public interface IMsgSegment : IEquatable<IMsgSegment>
{
    string Build();
}
