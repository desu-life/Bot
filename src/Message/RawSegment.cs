using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public record RawSegment(string type, Object value) : IMsgSegment
{
    public string Build() => value switch
    {
        JObject j => $"<raw;{type}={j.ToString(Formatting.None)}>",
        _ => $"<raw;{type}={value}>",
    };

    public virtual bool Equals(RawSegment? other) =>
        other is not null && type == other.type && Equals(value, other.value);

    public override int GetHashCode() => HashCode.Combine(type, value);

    public bool Equals(IMsgSegment? other) => other is RawSegment r && Equals(r);
}
