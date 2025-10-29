using System.IO;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using Img = SixLabors.ImageSharp.Image;

namespace KanonBot;

public static partial class Utils
{
  
    public static string Byte2File(string fileName, byte[] buffer)
    {
        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
            fs.Write(buffer, 0, buffer.Length);
        }
        return Path.GetFullPath(fileName);;
    }

    public static Stream LoadFile2ReadStream(string filePath)
    {
        var fs = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        return fs;
    }

    public static async Task<byte[]> LoadFile2Byte(string filePath)
    {
        using var fs = LoadFile2ReadStream(filePath);
        byte[] bt = new byte[fs.Length];
        var mem = new Memory<Byte>(bt);
        await fs.ReadExactlyAsync(mem);
        fs.Close();
        return mem.ToArray();
    }

    async public static Task<Image<Rgba32>> ReadImageRgba(string path)
    {
        return await Img.LoadAsync<Rgba32>(path);
    }

    async public static Task<Image<Rgba32>?> TryReadImageRgba(string path)
    {
        try
        {
            return await Img.LoadAsync<Rgba32>(path);
        }
        catch
        {
            return null;
        }
    }

    async public static Task<(Image<Rgba32>, IImageFormat)> ReadImageRgbaWithFormat(string path)
    {
        using var s = Utils.LoadFile2ReadStream(path);
        var img = await Img.LoadAsync<Rgba32>(s);
        return (img, img.Metadata.DecodedImageFormat!);
    }
}
