#pragma warning disable IDE0044 // 添加只读修饰符
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Img = SixLabors.ImageSharp.Image;

namespace KanonBot.Image;

static class Helpers
{
    public static IImageProcessingContext DrawText(
        this IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        float x,
        float y,
        VerticalAlignment verticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left
    )
    {
        return DrawText(
            ctx,
            text,
            font,
            color,
            new PointF(x, y),
            verticalAlignment,
            horizontalAlignment
        );
    }

    public static IImageProcessingContext DrawText(
        this IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        PointF point,
        VerticalAlignment verticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left
    )
    {
        var drawingOptions = ctx.GetDrawingOptions();
        var textOptions = new RichTextOptions(font)
        {
            FallbackFontFamilies = [Fonts.HarmonySans, Fonts.HarmonySansArabic],
            VerticalAlignment = verticalAlignment,
            HorizontalAlignment = horizontalAlignment,
            Origin = point,
        };
        return ctx.DrawText(drawingOptions, textOptions, text, new SolidBrush(color), null);
    }
}

static class BuildCornersClass
{
    private static readonly GraphicsOptions GraphicsOptions =
        new()
        {
            Antialias = true,
            AlphaCompositionMode = PixelAlphaCompositionMode.DestOut // enforces that any part of this shape that has color is punched out of the background
        };

    #region buildCornersPart
    public static IImageProcessingContext ApplyRoundedCornersPart(
        this IImageProcessingContext ctx,
        float cornerRadiusLT,
        float cornerRadiusRT,
        float cornerRadiusLB,
        float cornerRadiusRB
    )
    {
        Size size = ctx.GetCurrentSize();
        IPathCollection corners = BuildCornersPart(
            size.Width,
            size.Height,
            cornerRadiusLT,
            cornerRadiusRT,
            cornerRadiusLB,
            cornerRadiusRB
        );

        ctx.SetGraphicsOptions(GraphicsOptions);

        // mutating in here as we already have a cloned original
        // use any color (not Transparent), so the corners will be clipped
        foreach (var c in corners)
        {
            ctx = ctx.Fill(Color.Red, c);
        }
        return ctx;
    }

    public static IImageProcessingContext RoundCornerParts(
        this IImageProcessingContext processingContext,
        Size size,
        float cornerRadiusLT,
        float cornerRadiusRT,
        float cornerRadiusLB,
        float cornerRadiusRB
    )
    {
        return processingContext
            .Resize(
                new SixLabors.ImageSharp.Processing.ResizeOptions
                {
                    Size = size,
                    Mode = ResizeMode.Crop
                }
            )
            .ApplyRoundedCornersPart(
                cornerRadiusLT,
                cornerRadiusRT,
                cornerRadiusLB,
                cornerRadiusRB
            );
    }

    public static PathCollection BuildCornersPart(
        int imageWidth,
        int imageHeight,
        float cornerRadiusLT,
        float cornerRadiusRT,
        float cornerRadiusLB,
        float cornerRadiusRB
    )
    {
        //CREARE SQUARE
        var rectLT = new RectangularPolygon(-0.5f, -0.5f, cornerRadiusLT, cornerRadiusLT);
        var rectRT = new RectangularPolygon(-0.5f, -0.5f, cornerRadiusRT, cornerRadiusRT);
        var rectLB = new RectangularPolygon(-0.5f, -0.5f, cornerRadiusLB, cornerRadiusLB);
        var rectRB = new RectangularPolygon(-0.5f, -0.5f, cornerRadiusRB, cornerRadiusRB);

        float rightPos,
            bottomPos;
        //TOP LEFT
        IPath cornerTopLeft = rectLT.Clip(
            new EllipsePolygon(cornerRadiusLT - 0.5f, cornerRadiusLT - 0.5f, cornerRadiusLT)
        );

        //TOP RIGHT
        IPath cornerTopRight = rectRT.Clip(
            new EllipsePolygon(cornerRadiusRT - 0.5f, cornerRadiusRT - 0.5f, cornerRadiusRT)
        );
        rightPos = imageWidth - cornerTopRight.Bounds.Width + 1;
        cornerTopRight = cornerTopRight.RotateDegree(90).Translate(rightPos, 0);

        //BOTTOM LEFT
        IPath cornerBottomLeft = rectLB.Clip(
            new EllipsePolygon(cornerRadiusLB - 0.5f, cornerRadiusLB - 0.5f, cornerRadiusLB)
        );
        bottomPos = imageHeight - cornerBottomLeft.Bounds.Height + 1;
        cornerBottomLeft = cornerBottomLeft.RotateDegree(-90).Translate(0, bottomPos);

        //BOTTOM RIGHT
        IPath cornerBottomRight = rectRB.Clip(
            new EllipsePolygon(cornerRadiusRB - 0.5f, cornerRadiusRB - 0.5f, cornerRadiusRB)
        );
        rightPos = imageWidth - cornerBottomRight.Bounds.Width + 1;
        bottomPos = imageHeight - cornerBottomRight.Bounds.Height + 1;
        cornerBottomRight = cornerBottomRight.RotateDegree(180).Translate(rightPos, bottomPos);

        return new PathCollection(
            cornerTopLeft,
            cornerBottomLeft,
            cornerTopRight,
            cornerBottomRight
        );
    }
    #endregion


    #region RoundedCorners
    private static IImageProcessingContext ApplyRoundedCorners(
        this IImageProcessingContext ctx,
        float cornerRadius
    )
    {
        Size size = ctx.GetCurrentSize();
        IPathCollection corners = BuildCorners(size.Width, size.Height, cornerRadius);

        ctx.SetGraphicsOptions(
            new GraphicsOptions()
            {
                Antialias = true,
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut // enforces that any part of this shape that has color is punched out of the background
            }
        );

        // mutating in here as we already have a cloned original
        // use any color (not Transparent), so the corners will be clipped
        foreach (var c in corners)
        {
            ctx = ctx.Fill(Color.Red, c);
        }
        return ctx;
    }

    public static IImageProcessingContext RoundCorner(
        this IImageProcessingContext processingContext,
        Size size,
        float cornerRadius
    )
    {
        return processingContext
            .Resize(new ResizeOptions { Size = size, Mode = ResizeMode.Crop })
            .ApplyRoundedCorners(cornerRadius);
    }

    public static IImageProcessingContext RoundCorner(
        this IImageProcessingContext processingContext,
        float cornerRadius
    )
    {
        return processingContext.ApplyRoundedCorners(cornerRadius);
    }

    private static PathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
    {
        // first create a square
        var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

        // then cut out of the square a circle so we are left with a corner
        IPath cornerTopLeft = rect.Clip(
            new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius)
        );

        // corner is now a corner shape positions top left
        //lets make 3 more positioned correctly, we can do that by translating the original around the center of the image

        float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
        float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

        // move it across the width of the image - the width of the shape
        IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
        IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
        IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

        return new PathCollection(
            cornerTopLeft,
            cornerBottomLeft,
            cornerTopRight,
            cornerBottomRight
        );
    }
    #endregion
}
