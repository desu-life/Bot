using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public class Chain: IEquatable<Chain>
{
    List<IMsgSegment> msgList { get; set; }
    public Chain()
    {
        this.msgList = new();
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


    public string Build()
    {
        var raw = "";
        foreach (var item in this.msgList)
        {
            raw += item.Build();
        }
        return raw;
    }

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
            return this.msgList[0] is AtSegment t && t.value == at.value && t.platform == at.platform;
    }

    public T? Find<T>() where T : class, IMsgSegment =>
        this.msgList.Find(t => t is T) as T;

    public bool Equals(Chain? other)
    {
        if (other == null)
            return false;
        if (this.msgList.Count != other.msgList.Count)
            return false;
        for (int i = 0; i < this.msgList.Count; i++)
        {
            if (!this.msgList[i].Equals(other.msgList[i]))
                return false;
        }
        return true;
    }
}
