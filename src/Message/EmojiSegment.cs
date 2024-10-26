using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public class EmojiSegment : IMsgSegment, IEquatable<EmojiSegment>
{
    public string value { get; set; }
    public EmojiSegment(string value)
    {
        this.value = value;
    }

    public string Build()
    {
        return $"<Face;id={value}>";
    }

    public bool Equals(EmojiSegment? other)
    {
        return other != null && this.value == other.value;
    }

    public bool Equals(IMsgSegment? other)
    {
        if (other is EmojiSegment r)
            return this.Equals(r);
        else
            return false;
    }
}
