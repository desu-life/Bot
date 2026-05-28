using KanonBot.Drivers;

namespace KanonBot.Message;

public record AtSegment(string value, Platform platform) : IMsgSegment
{
    public string Build()
    {
        var p = platform switch
        {
            Platform.OneBot => "qq",
            Platform.Guild => "guild",
            Platform.Discord => "discord",
            _ => "unknown",
        };
        return $"{p}={value}";
    }

    public bool Equals(IMsgSegment? other) => other is AtSegment r && Equals(r);
}
