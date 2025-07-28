#pragma warning disable CS8618 // 非null 字段未初始化

namespace KanonBot.Image;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;
using KanonBot.Image.Components;

public static class PPVS
{
    public class PPVSPanelData
    {
        public string u1Name;
        public string u2Name;
        public OSU.Models.PPlusData.UserPerformancesNext u1;
        public OSU.Models.PPlusData.UserPerformancesNext u2;
    }

    public static async Task<Img> DrawPPVS(PPVSPanelData data)
    {
        var ppvsImg = await Img.LoadAsync("work/legacy/ppvs.png");
        Hexagram.HexagramInfo hi =
            new()
            {
                nodeCount = 6,
                nodeMaxValue = 12000,
                size = 1134,
                sideLength = 791,
                mode = 2,
                strokeWidth = 6f,
                nodesize = new SizeF(15f, 15f)
            };
        // hi.abilityLineColor = Color.ParseHex("#FF7BAC");
        var multi = new double[6] { 14.1, 69.7, 1.92, 19.8, 0.588, 3.06 };
        var exp = new double[6] { 0.769, 0.596, 0.953, 0.8, 1.175, 0.993 };
        var u1d = new double[6];
        u1d[0] = data.u1.AccuracyTotal;
        u1d[1] = data.u1.FlowAimTotal;
        u1d[2] = data.u1.JumpAimTotal;
        u1d[3] = data.u1.PrecisionTotal;
        u1d[4] = data.u1.SpeedTotal;
        u1d[5] = data.u1.StaminaTotal;
        var u2d = new double[6];
        u2d[0] = data.u2.AccuracyTotal;
        u2d[1] = data.u2.FlowAimTotal;
        u2d[2] = data.u2.JumpAimTotal;
        u2d[3] = data.u2.PrecisionTotal;
        u2d[4] = data.u2.SpeedTotal;
        u2d[5] = data.u2.StaminaTotal;
        // acc ,flow, jump, pre, speed, sta

        if (data.u1.PerformanceTotal < data.u2.PerformanceTotal)
        {
            hi.abilityFillColor = Color.FromRgba(255, 123, 172, 50);
            hi.abilityLineColor = Color.FromRgba(255, 123, 172, 255);
            using var tmp1 = Hexagram.Draw(u1d, multi, exp, hi);
            ppvsImg.Mutate(x => x.DrawImage(tmp1, new Point(0, -120), 1));
            hi.abilityFillColor = Color.FromRgba(41, 171, 226, 50);
            hi.abilityLineColor = Color.FromRgba(41, 171, 226, 255);
            using var tmp2 = Hexagram.Draw(u2d, multi, exp, hi);
            ppvsImg.Mutate(x => x.DrawImage(tmp2, new Point(0, -120), 1));
        }
        else
        {
            hi.abilityFillColor = Color.FromRgba(41, 171, 226, 50);
            hi.abilityLineColor = Color.FromRgba(41, 171, 226, 255);
            using var tmp1 = Hexagram.Draw(u2d, multi, exp, hi);
            ppvsImg.Mutate(x => x.DrawImage(tmp1, new Point(0, -120), 1));
            hi.abilityFillColor = Color.FromRgba(255, 123, 172, 50);
            hi.abilityLineColor = Color.FromRgba(255, 123, 172, 255);
            using var tmp2 = Hexagram.Draw(u1d, multi, exp, hi);
            ppvsImg.Mutate(x => x.DrawImage(tmp2, new Point(0, -120), 1));
        }

        // 打印用户名
        var font = Fonts.avenirLTStdMedium.Get(36);
        var color = Color.ParseHex("#999999");
        ppvsImg.Mutate(x => x.DrawText(data.u1Name, font, color, 808, 888));
        ppvsImg.Mutate(x => x.DrawText(data.u2Name, font, color, 264, 888));

        // 打印每个用户数据
        var y_offset = new int[6] { 1485, 1150, 1066, 1234, 1318, 1403 }; // pp+数据的y轴坐标
        font = Fonts.avenirLTStdMedium.Get(32);
        for (var i = 0; i < u1d.Length; i++)
        {
            ppvsImg.Mutate(x =>
                x.DrawText(Math.Round(u1d[i]).ToString(), font, color, 664, y_offset[i])
            );
        }
        ppvsImg.Mutate(x =>
            x.DrawText(data.u1.PerformanceTotal.ToString("0.##"), font, color, 664, 980)
        );
        for (var i = 0; i < u2d.Length; i++)
        {
            ppvsImg.Mutate(x =>
                x.DrawText(Math.Round(u2d[i]).ToString(), font, color, 424, y_offset[i])
            );
        }
        ppvsImg.Mutate(x =>
            x.DrawText(data.u2.PerformanceTotal.ToString("0.##"), font, color, 424, 980)
        );

        // 打印数据差异
        var diffPoint = 960;
        color = Color.ParseHex("#ffcd22");
        ppvsImg.Mutate(x =>
            x.DrawText(
                Math.Round(data.u2.PerformanceTotal - data.u1.PerformanceTotal).ToString(),
                font,
                color,
                diffPoint,
                980
            )
        );

        ppvsImg.Mutate(x =>
            x.DrawText(Math.Round(u2d[2] - u1d[2]).ToString(), font, color, diffPoint, 1066)
        );
        ppvsImg.Mutate(x =>
            x.DrawText(Math.Round(u2d[1] - u1d[1]).ToString(), font, color, diffPoint, 1150)
        );
        ppvsImg.Mutate(x =>
            x.DrawText(Math.Round(u2d[3] - u1d[3]).ToString(), font, color, diffPoint, 1234)
        );
        ppvsImg.Mutate(x =>
            x.DrawText(Math.Round(u2d[4] - u1d[4]).ToString(), font, color, diffPoint, 1318)
        );
        ppvsImg.Mutate(x =>
            x.DrawText(Math.Round(u2d[5] - u1d[5]).ToString(), font, color, diffPoint, 1403)
        );
        ppvsImg.Mutate(x =>
            x.DrawText(Math.Round(u2d[0] - u1d[0]).ToString(), font, color, diffPoint, 1485)
        );

        using var title = await Img.LoadAsync($"work/legacy/ppvs_title.png");
        ppvsImg.Mutate(x => x.DrawImage(title, new Point(0, 0), 1));

        return ppvsImg;
    }
}
