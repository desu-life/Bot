using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public class TextSegment : IMsgSegment, IEquatable<TextSegment>
{
    public string value { get; set; }
    public TextSegment(string msg)
    {
        this.value = msg;
    }


    public string Build()
    {
        return value.ToString();
    }

    public bool Equals(TextSegment? other)
    {
        return other != null && this.value == other.value;
    }

    public bool Equals(IMsgSegment? other)
    {
        if (other is TextSegment r)
            return this.Equals(r);
        else
            return false;
    }
}
