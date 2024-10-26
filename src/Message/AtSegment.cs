using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public class AtSegment : IMsgSegment, IEquatable<AtSegment>
{
    public Platform platform { get; set; }
    // all 表示全体成员
    public string value { get; set; }
    public AtSegment(string target, Platform platform)
    {
        this.value = target;
        this.platform = platform;
    }

    public string Build()
    {
        var platform = this.platform switch
        {
            Platform.OneBot => "qq",
            Platform.Guild => "gulid",
            Platform.Discord => "discord",
            Platform.KOOK => "kook",
            _ => "unknown",
        };
        return $"{platform}={value}";
    }
    
    public bool Equals(AtSegment? other)
    {
        return other != null && this.value == other.value && this.platform == other.platform;
    }

    public bool Equals(IMsgSegment? other)
    {
        if (other is AtSegment r)
            return this.Equals(r);
        else
            return false;
    }

}
