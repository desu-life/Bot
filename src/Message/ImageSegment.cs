namespace KanonBot.Message;

public record ImageSegment(string value, ImageSegment.Type t) : IMsgSegment
{
    public enum Type
    {
        File,
        Base64,
        Url
    }

    public string Build() =>
        t switch
        {
            Type.File => $"<image;file={value}>",
            Type.Base64 => "<image;base64>",
            Type.Url => $"<image;url={value}>",
            _ => "",
        };

    public bool Equals(IMsgSegment? other) => other is ImageSegment r && Equals(r);
}
