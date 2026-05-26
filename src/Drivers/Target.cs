using System.Threading.Channels;
using libDiscord = Discord;
using Msg = KanonBot.Message;

namespace KanonBot.Drivers;

// 消息target封装
// 暂时还不知道怎么写
public class Target
{
    public static Atom<List<(Target, ChannelWriter<Target>)>> Waiters { get; set; } =
        Atom<List<(Target, ChannelWriter<Target>)>>(new());

    public async Task<Option<Target>> prompt(TimeSpan timeout)
    {
        var channel = Channel.CreateBounded<Target>(1);
        Waiters.Swap(l =>
        {
            l.Add((this, channel.Writer));
            return l;
        });
        var ret = await channel.Reader.ReadAsync().AsTask().TimeOut(timeout);
        Waiters.Swap(l =>
        {
            l.Remove((this, channel.Writer));
            return l;
        });
        return ret;
    }

    public required Msg.Chain msg { get; init; }

    // account和sender为用户ID字符串，可以是qq号，khl号，等等
    public required string? selfAccount { get; init; }
    public required MessageSource source { get; init; }
    public required string? sender { get; init; }
    public required Platform platform { get; init; }
    public bool isFromAdmin { get; set; } = false;
    public DateTimeOffset time { get; set; } = DateTimeOffset.Now;
    public bool HasReplied { get; private set; } = false;

    // 原平台消息结构
    public object? raw { get; init; }

    // 原平台接口
    public required ISocket socket { get; init; }

    public Task<bool> reply(string m)
    {
        return this.reply(new Msg.Chain().msg(m));
    }

    public Task<bool> privateReply(string m)
    {
        return this.privateReply(new Msg.Chain().msg(m));
    }

    public Task<bool> reply(SixLabors.ImageSharp.Image img, SixLabors.ImageSharp.Formats.IImageEncoder encoder)
    {
        using var ms = new System.IO.MemoryStream();
        img.Save(ms, encoder);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return reply(new Msg.Chain().image(base64, Msg.ImageSegment.Type.Base64));
    }

    public Task<bool> reply(Takumi.Render.UniFFI.RenderedImage img)
    {
        using var stream = new System.IO.MemoryStream(img.Bytes, writable: false);
        var base64 = Convert.ToBase64String(stream.ToArray());
        return reply(new Msg.Chain().image(base64, Msg.ImageSegment.Type.Base64));
    }

    public async Task<bool> reply(Msg.Chain msgChain)
    {
        bool result;
        if (this.socket is IReply r) {
            result = await r.Reply(this, msgChain);
        } else {
            await socket.SendAsync(msgChain.ToString());
            result = true;
        }

        if (result)
            HasReplied = true;

        return result;
    }

    public async Task<bool> privateReply(Msg.Chain msgChain)
    {
        bool result;
        if (this.socket is IPrivateReply r)
        {
            result = await r.PrivateReply(this, msgChain);
        }
        else
        {
            result = await reply(msgChain);
        }

        if (result)
            HasReplied = true;

        return result;
    }
}
