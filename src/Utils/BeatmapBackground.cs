using System.IO;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KanonBot;

public static partial class Utils
{
    public static async Task<Image<Rgba32>?> LoadOrDownloadBackground(long sid, long bid)
    {
        var bgPath = $"./work/background/{bid}.png";
        
        var image = await TryReadImageRgba(bgPath);
        if (image is null)
        {
            bgPath = await API.OSU.Client.SayoDownloadBeatmapBackgroundImg(
                sid,
                bid,
                "./work/background/"
            );

            if (bgPath != null) image = await TryReadImageRgba(bgPath);
        }

        if (image is null)
        {
            bgPath = await API.OSU.Client.DownloadBeatmapBackgroundImg(
                sid,
                "./work/background/",
                $"{bid}.png"
            );

            if (bgPath != null) image = await TryReadImageRgba(bgPath);
        }

        return image;
    }
}
