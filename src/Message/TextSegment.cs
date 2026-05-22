namespace KanonBot.Message;

public record TextSegment(string value) : IMsgSegment
{
    public string Build() => value;

    public bool Equals(IMsgSegment? other) => other is TextSegment r && Equals(r);
}
