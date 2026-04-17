using Discord;

namespace KanonBot.Drivers;

public record MessageSource(string Platform, string Type, string Id)
{
    public string CacheKey => $"{Platform}:{Type}:{Id}";
    
    public static MessageSource FromDiscord(IMessage message) =>
        message.Channel switch
        {
            ITextChannel c  => new("dc", "guild", c.Id.ToString()),
            IDMChannel c    => new("dc", "dm", c.Recipient.Id.ToString()),
            _               => new("dc", "unknown", message.Channel.Id.ToString())
        };
    
    public static MessageSource FromOneBot(OneBot.Models.CQMessageEventBase message) =>
        message switch
        {
            OneBot.Models.GroupMessage g => new("onebot", "group", g.GroupId.ToString()),
            OneBot.Models.PrivateMessage p => new("onebot", "private", p.UserId.ToString()),
            _ => new("onebot", "unknown", message.UserId.ToString())
        };

    public static MessageSource FromGuild(string guildId) =>
        new("guild", "guild", guildId);

    public static MessageSource FromCacheKey(string cacheKey)
    {
        var parts = cacheKey.Split(':');
        if (parts.Length != 3) throw new ArgumentException("Invalid cache key format");
        return new MessageSource(parts[0], parts[1], parts[2]);
    }
}