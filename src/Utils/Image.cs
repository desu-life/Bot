using System.IO;
using System.Security.Cryptography;
using static KanonBot.API.OSU.OSUExtensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Img = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats;

namespace KanonBot;

public static partial class Utils
{
    public static async Task<Image<Rgba32>?> LoadImageFromUrlAsync(string url)
    {
        try
        {
            using var stream = await url.GetStreamAsync();
            return await Img.LoadAsync<Rgba32>(stream);
        }
        catch
        {
            return null;
        }
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

    public static Color GetDominantColor(Image<Rgba32> image)
    {
        image.Mutate(x => x.Resize(1, 1));

        // 获取该像素的颜色
        Rgba32 dominantColor = image[0, 0];
        return dominantColor;
    }
}
