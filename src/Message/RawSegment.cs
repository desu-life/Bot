using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public class RawSegment : IMsgSegment, IEquatable<RawSegment>
{
    public Object value { get; set; }
    public string type { get; set; }
    public RawSegment(string type, Object value)
    {
        this.type = type;
        this.value = value;
    }

    public string Build()
    {
        return value switch {
            JObject j => $"<raw;{type}={j.ToString(Formatting.None)}>",
            _ => $"<raw;{type}={value}>",
        };
    }

    public bool Equals(RawSegment? other)
    {
        return other != null && this.type == other.type && this.value == other.value;
    }

    public bool Equals(IMsgSegment? other)
    {
        if (other is RawSegment r)
            return this.Equals(r);
        else
            return false;
    }
}
