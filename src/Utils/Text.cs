using SixLabors.Fonts;
using DrawingRichTextOptions = SixLabors.ImageSharp.Drawing.Processing.RichTextOptions;

namespace KanonBot;

public static partial class Utils
{
    /// <summary>
    /// Truncate text to fit width using binary search over substring length.
    /// </summary>
    public static string TruncateTextByWidth(
        string? text,
        DrawingRichTextOptions textOptions,
        float maxWidth,
        string suffix = "..."
    )
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        if (TextMeasurer.MeasureSize(text, textOptions).Width <= maxWidth)
            return text;

        if (!string.IsNullOrEmpty(suffix)
            && TextMeasurer.MeasureSize(suffix, textOptions).Width > maxWidth)
            return string.Empty;

        int left = 0;
        int right = text.Length;

        while (left < right)
        {
            int mid = (left + right + 1) / 2;
            string candidate = text[..mid] + suffix;
            if (TextMeasurer.MeasureSize(candidate, textOptions).Width <= maxWidth)
                left = mid;
            else
                right = mid - 1;
        }

        if (left <= 0) return string.Empty;
        return text[..left] + suffix;
    }
}