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

public static class LineChart
{
    /// <summary>
    /// RawData[0] 不绘制，仅仅为了适配 InfoPanelV2 的差异文本输出，其它方法调用的时候保持第一个数据为 0，DataCount 一定为 RawData.Length - 1
    /// </summary>
    public static Img Draw(
        int Width,
        int Height,
        int DataCount,
        long[] RawData,
        Color ChartLineColor,
        Color ChartTextColor,
        Color DashColor,
        Color DotColor,
        Color DotStrokeColor,
        bool DrawDiff,
        float dotThickness,
        float LineThickness
    )
    {
        Img image = new Image<Rgba32>(Width, Height);

        List<int> xPos = new();
        List<int> yPos = new();

        //计算x坐标
        var xPosEach = (Width - 10) / DataCount;
        for (int i = 0; i < DataCount; i++)
            xPos.Add(50 + xPosEach * i);

        //计算y坐标
        long[] Data = RawData.Take(7).Reverse().ToArray();

        var yPosMax = Data.Max();
        var yPosMin = Data.Min();

        for (int i = 0; i < DataCount; i++)
        {
            var x = ((double)(Data[i] - yPosMin) / (double)(yPosMax - yPosMin));
            if (double.IsNaN(x))
                x = 0.8;
            yPos.Add(((int)(((double)Height - 80.00) * x)) + 50);
        }

        //绘制虚线
        for (int i = 0; i < DataCount; i++)
        {
            PointF[] p = [new Point(xPos[i], yPos[i]), new Point(xPos[i], Height + 20)];
            var pen = Pens.Dash(DashColor, 3f);
            image.Mutate(x => x.DrawLine(pen, p));
        }

        //绘制线
        for (int i = 0; i < DataCount - 1; i++)
        {
            PointF[] p = [new Point(xPos[i], yPos[i]), new Point(xPos[i + 1], yPos[i + 1])];
            image.Mutate(x => x.DrawLine(ChartLineColor, LineThickness, p));
        }

        //绘制点
        for (int i = 0; i < DataCount; i++)
            image.Mutate(x =>
                x.Fill(
                    DotStrokeColor,
                    new EllipsePolygon(new Point(xPos[i], yPos[i]), dotThickness / 4 * 5)
                )
            );
        for (int i = 0; i < DataCount; i++)
            image.Mutate(x =>
                x.Fill(DotColor, new EllipsePolygon(new Point(xPos[i], yPos[i]), dotThickness))
            );

        //绘制差异数值
        if (DrawDiff)
        {
            var textOptions = new RichTextOptions( Fonts.TorusSemiBold.Get(120))
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            textOptions.Font = Fonts.TorusRegular.Get(40);
            Data = RawData.Reverse().ToArray();
            for (int i = 0; i < DataCount; i++)
            {
                textOptions.Origin = new PointF(xPos[i], yPos[i] - 34);
                image.Mutate(x =>
                    x.DrawText(textOptions, ((Data[i + 1] - Data[i]) * -1).ToString(), ChartTextColor)
                );
            }
        }
        return image;
    }
}
