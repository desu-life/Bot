using KanonBot.Drivers;

namespace KanonBot.Message;

public class Chain : IEquatable<Chain>
{
    List<IMsgSegment> msgList { get; set; }

    public Chain()
    {
        this.msgList =  [ ];
    }

    public static Chain FromList(List<IMsgSegment> list)
    {
        return new Chain { msgList = list };
    }

    public void Add(IMsgSegment n)
    {
        this.msgList.Add(n);
    }

    public Chain msg(string v)
    {
        this.Add(new TextSegment(v));
        return this;
    }

    public Chain at(string v, Platform p)
    {
        this.Add(new AtSegment(v, p));
        return this;
    }

    public Chain image(string v, ImageSegment.Type t)
    {
        this.Add(new ImageSegment(v, t));
        return this;
    }

    public IEnumerable<IMsgSegment> Iter()
    {
        return this.msgList.AsEnumerable();
    }

    public string Build() => string.Concat(this.msgList.Select(item => item.Build()));

    public override string ToString()
    {
        return this.Build();
    }

    public int Length() => this.msgList.Count;

    public bool StartsWith(string s)
    {
        if (this.msgList.Count == 0)
            return false;
        else
            return this.msgList[0] is TextSegment t && t.value.StartsWith(s);
    }

    public bool StartsWith(AtSegment at)
    {
        if (this.msgList.Count == 0)
            return false;
        else
            return this.msgList[0] is AtSegment t
                && t.value == at.value
                && t.platform == at.platform;
    }

    public T? Find<T>()
        where T : class, IMsgSegment => this.msgList.Find(t => t is T) as T;

    public bool Equals(Chain? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return this.msgList.SequenceEqual(other.msgList);
    }

    public override bool Equals(object? obj) => obj is Chain other && Equals(other);

    public override int GetHashCode() =>
        msgList
            .Aggregate(
                new HashCode(),
                (hash, msg) =>
                {
                    hash.Add(msg);
                    return hash;
                }
            )
            .ToHashCode();
}
