using System.IO;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KanonBot;

public static partial class Utils
{
    public static Color GetDominantColor(Image<Rgba32> image)
    {
        image.Mutate(x => x.Resize(1, 1));

        // 获取该像素的颜色
        Rgba32 dominantColor = image[0, 0];
        return dominantColor;
    }
}
