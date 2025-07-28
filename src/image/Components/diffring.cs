#pragma warning disable CS8618 // 非null 字段未初始化

namespace KanonBot.Image.Components;

using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;

public static class DifficultyRing
{
    public static async Task<Img> Draw(RosuPP.Mode mode, double star)
    {
        var ringFile = mode switch
        {
            RosuPP.Mode.Osu => "std-expertplus.png",
            RosuPP.Mode.Taiko => "taiko-expertplus.png",
            RosuPP.Mode.Catch => "ctb-expertplus.png",
            RosuPP.Mode.Mania => "mania-expertplus.png",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        using var color = new Image<Rgba32>(128, 128);
        color.Mutate(x => x.Fill(Utils.ForStarDifficultyScore(star)));

        using var cover = await Image<Rgba32>.LoadAsync($"./work/icons/ringcontent.png");
        cover.Mutate(x => x.Resize(128, 128));
        color.Mutate(x => x.DrawImage(cover, new Point(0, 0), 0.3f));
        cover.Mutate(x => x.Brightness(0.9f)); // adjust

        var ring = await Image<Rgba32>.LoadAsync($"./work/icons/{ringFile}");
        ring.Mutate(x => x.Resize(128, 128));
        ring.Mutate(x =>
            x.DrawImage(
                color,
                new Point(0, 0),
                PixelColorBlendingMode.Lighten,
                PixelAlphaCompositionMode.SrcAtop,
                1f
            )
        );
        return ring;
    }
}
