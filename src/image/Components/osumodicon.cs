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

public static class OsuModIcon
{
    public static async Task<Img> DrawV2(OSU.Models.Mod mod, bool showCustomSettings = false)
    {
        var modName = mod.Acronym.ToUpper();
        var modPath = $"./work/mods/{modName}.png";
        if (File.Exists(modPath))
        {
            var modPic = await Img.LoadAsync(modPath);
            modPic.Mutate(x => x.Resize(200, 0));

            if (showCustomSettings)
            {
                var speedChange = (double?)mod.Settings?.GetValue("speed_change");

                if (speedChange is not null)
                {
                    var color = Utils.GetDominantColor(modPic.CloneAs<Rgba32>());
                    var i = new Image<Rgba32>(200, 20);
                    i.Mutate(x => x.Fill(color).RoundCorner(new Size(130, 20), 7));
                    modPic.Mutate(x =>
                        x.DrawImage(
                            i,
                            new Point(35, 50),
                            PixelColorBlendingMode.Subtract,
                            PixelAlphaCompositionMode.SrcAtop,
                            0.7f
                        )
                    );
                    modPic.Mutate(x =>
                        x.DrawText(
                            $"{speedChange}x",
                            Fonts.Mizolet.Get(8),
                            Color.LightGray,
                            100,
                            55,
                            VerticalAlignment.Center,
                            HorizontalAlignment.Center
                        )
                    );
                }
            }

            return modPic;
        }
        else
        {
            var font = Fonts.Mizolet.Get(40);
            var modPic = await Img.LoadAsync($"./work/mods/Unknown.png");
            modPic.Mutate(x => x.Resize(200, 0));
            modPic.Mutate(operation: x => x.DrawText(modName, font, Color.Black, 96, 34));
            modPic.Mutate(x => x.DrawText(modName, font, Color.White, 96, 33));
            return modPic;
        }
    }
}
