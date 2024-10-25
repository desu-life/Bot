using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public class ImageSegment : IMsgSegment, IEquatable<ImageSegment>
{
    public enum Type
    {
        File,   // 如果是file就是文件地址
        Base64,
        Url
    }
    public Type t { get; set; }
    public string value { get; set; }
    public ImageSegment(string value, Type t)
    {
        this.value = value;
        this.t = t;
    }

    public string Build()
    {
        return this.t switch
        {
            Type.File => $"<image;file={this.value}>",
            Type.Base64 => $"<image;base64>",
            Type.Url => $"<image;url={this.value}>",
            // 保险
            _ => "",
        };
    }

    public bool Equals(ImageSegment? other)
    {
        return other != null && this.value == other.value && this.t == other.t;
    }

    public bool Equals(IMsgSegment? other)
    {
        if (other is ImageSegment r)
            return this.Equals(r);
        else
            return false;
    }
}
