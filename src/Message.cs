using System.IO;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using KanonBot.Drivers;

namespace KanonBot.Message;

public interface IMsgSegment
{
    string Build();
}

public class RawMessage : IMsgSegment
{
    public JObject value;
    public string type;
    public RawMessage(string type, JObject value)
    {
        this.type = type;
        this.value = value;
    }

    public string Build()
    {
        return $"<raw;{type}={value.ToString(Formatting.None)}>";
    }
}
public class TextSegment : IMsgSegment
{
    public string value;
    public TextSegment(string msg)
    {
        this.value = msg;
    }

    public string Build()
    {
        return value.ToString();
    }
}
public class FaceSegment : IMsgSegment
{
    public string value;
    public FaceSegment(string value)
    {
        this.value = value;
    }

    public string Build()
    {
        return $"<Face;id={value}>";
    }
}
public class AtSegment : IMsgSegment
{
    public Platform platform;
    public string value;
    public AtSegment(string target, Platform platform)
    {
        this.value = target;
        this.platform = platform;
    }

    public string Build()
    {
        return $"<at;{platform.ToString()}={value}>";
    }
}

public class ImageSegment : IMsgSegment
{
    public enum Type
    {
        File,
        Base64,
        Url
    }
    public Type t;
    public string value;
    public ImageSegment(string value, Type t)
    {
        this.value = value;
        this.t = t;
    }

    public string Build()
    {
        switch (this.t)
        {
            case Type.File:
                return $"<image;file:///{this.value}>";
            case Type.Base64:
                return $"<image;base64:///{this.value}>";
            case Type.Url:
                return $"<image;file:///{this.value}>";
        }
        // 保险
        return "";
    }
}

public class Chain
{
    List<IMsgSegment> msgList;
    public Chain()
    {
        this.msgList = new();
    }

    public void append(IMsgSegment n)
    {
        this.msgList.Add(n);
    }

    public Chain msg(string v)
    {
        this.append(new TextSegment(v));
        return this;
    }

    public Chain image(string v, ImageSegment.Type t)
    {
        this.append(new ImageSegment(v, t));
        return this;
    }

    public List<IMsgSegment> GetList()
    {
        return this.msgList;
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

    public bool StartsWith(string s)
    {
        if (this.msgList.Count == 0)
            return false;
        else
            return this.msgList[0] is TextSegment t && t.value.StartsWith(s);
    }
}