namespace KanonBot.Message;

public record ReplySegment(string value) : IMsgSegment
{
    public string Build() => $"<Reply;id={value}>";

    public bool Equals(IMsgSegment? other) => other is ReplySegment r && Equals(r);
}
