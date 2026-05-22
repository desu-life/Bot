namespace KanonBot.Message;

public record EmojiSegment(string value) : IMsgSegment
{
    public string Build() => $"<Face;id={value}>";

    public bool Equals(IMsgSegment? other) => other is EmojiSegment r && Equals(r);
}
