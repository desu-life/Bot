using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.Message;

public record RawSegment(string type, Object value) : IMsgSegment
{
    public string Build() => value switch
    {
        JsonObject j => $"<raw;{type}={j.ToJsonString()}>",
        _ => $"<raw;{type}={value}>",
    };

    public virtual bool Equals(RawSegment? other) =>
        other is not null && type == other.type && Equals(value, other.value);

    public override int GetHashCode() => HashCode.Combine(type, value);

    public bool Equals(IMsgSegment? other) => other is RawSegment r && Equals(r);
}
