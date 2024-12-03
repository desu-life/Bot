using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;

namespace KanonBot;

public static partial class Utils
{
    public static double log1p(double x) =>
        Math.Abs(x) > 1e-4 ? Math.Log(1.0 + x) : (-0.5 * x + 1.0) * x;

    public static double ToLinear(double color) =>
        color <= 0.04045 ? color / 12.92 : Math.Pow((color + 0.055) / 1.055, 0.8);

    public static Vector4 ToLinear(this Vector4 colour) =>
        new(
            (float)ToLinear(colour.X),
            (float)ToLinear(colour.Y),
            (float)ToLinear(colour.Z),
            colour.W
        );

    public static Color ToColor(this Vector4 colour) =>
        Color.FromRgba(
            (byte)(colour.X * 255),
            (byte)(colour.Y * 255),
            (byte)(colour.Z * 255),
            (byte)(colour.W * 255)
        );

    public static Vector4 ValueAt(
        double time,
        Vector4 startColour,
        Vector4 endColour,
        double startTime,
        double endTime
    )
    {
        if (startColour == endColour)
            return startColour;

        double current = time - startTime;
        double duration = endTime - startTime;

        if (duration == 0 || current == 0)
            return startColour;

        var startLinear = startColour.ToLinear();
        var endLinear = endColour.ToLinear();

        float t = Math.Max(0, Math.Min(1, (float)(current / duration)));

        return new Vector4(
            startLinear.X + t * (endLinear.X - startLinear.X),
            startLinear.Y + t * (endLinear.Y - startLinear.Y),
            startLinear.Z + t * (endLinear.Z - startLinear.Z),
            startLinear.W + t * (endLinear.W - startLinear.W)
        );
    }

    public static Vector4 SampleFromLinearGradient(
        IReadOnlyList<(float position, Vector4 colour)> gradient,
        float point
    )
    {
        if (point < gradient[0].position)
            return gradient[0].colour;

        for (int i = 0; i < gradient.Count - 1; i++)
        {
            var startStop = gradient[i];
            var endStop = gradient[i + 1];

            if (point >= endStop.position)
                continue;

            return ValueAt(
                point,
                startStop.colour,
                endStop.colour,
                startStop.position,
                endStop.position
            );
        }

        return gradient[^1].colour;
    }

    private static readonly IReadOnlyList<(float position, Vector4 colour)> GradientMap =
    [
        (0.1f, Rgba32.ParseHex("#AAAAAA").ToVector4()),
        (0.1f, Rgba32.ParseHex("#4290FB").ToVector4()),
        (1.25f, Rgba32.ParseHex("#4FC0FF").ToVector4()),
        (2.0f, Rgba32.ParseHex("#4FFFD5").ToVector4()),
        (2.5f, Rgba32.ParseHex("#7CFF4F").ToVector4()),
        (3.3f, Rgba32.ParseHex("#F6F05C").ToVector4()),
        (4.2f, Rgba32.ParseHex("#FF8068").ToVector4()),
        (4.9f, Rgba32.ParseHex("#FF4E6F").ToVector4()),
        (5.8f, Rgba32.ParseHex("#C645B8").ToVector4()),
        (6.7f, Rgba32.ParseHex("#6563DE").ToVector4()),
        (7.7f, Rgba32.ParseHex("#18158E").ToVector4()),
        (9.0f, Rgba32.ParseHex("#000000").ToVector4()),
    ];

    private static readonly IReadOnlyList<(float position, Vector4 colour)> GradientMapScore =
    [
        (0.1f, Rgba32.ParseHex("#AAAAAA").ToVector4()),
        (0.1f, Rgba32.ParseHex("#4290FB").ToVector4()),
        (1.25f, Rgba32.ParseHex("#4FC0FF").ToVector4()),
        (2.0f, Rgba32.ParseHex("#4FFFD5").ToVector4()),
        (2.5f, Rgba32.ParseHex("#7CFF4F").ToVector4()),
        (3.3f, Rgba32.ParseHex("#F6F05C").ToVector4()),
        (4.2f, Rgba32.ParseHex("#FF8068").ToVector4()),
        (4.9f, Rgba32.ParseHex("#FF4E6F").ToVector4()),
        (5.7f, Rgba32.ParseHex("#C645B8").ToVector4()),
        (6.2f, Rgba32.ParseHex("#6563DE").ToVector4()),
        (7.0f, Rgba32.ParseHex("#18158E").ToVector4()),
        (8.4f, Rgba32.ParseHex("#000000").ToVector4()),
    ];

    public static Color ForStarDifficulty(double starDifficulty) =>
        memo<double, Color>(static starDifficulty =>
                SampleFromLinearGradient(
                        GradientMap,
                        (float)Math.Round(starDifficulty, 2, MidpointRounding.AwayFromZero)
                    )
                    .ToColor()
            )
            .Invoke(starDifficulty);

    public static Color ForStarDifficultyScore(double starDifficulty) =>
        memo<double, Color>(static starDifficulty =>
                SampleFromLinearGradient(
                        GradientMapScore,
                        (float)Math.Round(starDifficulty, 2, MidpointRounding.AwayFromZero)
                    )
                    .ToColor()
            )
            .Invoke(starDifficulty);

    private static readonly BigInteger ONE_HUNDRED_BILLION = BigInteger.Parse("100000000000");
    private static readonly BigInteger MAX_ALLOWED_SCORE = BigInteger.Parse("10000000000000000");

    public static BigInteger GetRequiredScoreForLevel(int level)
    {
        return memo<int, BigInteger>(static level =>
            {
                BigInteger score;
                if (level <= 100)
                {
                    if (level > 1)
                    {
                        score = new BigInteger(
                            Math.Floor(
                                (5000.0 / 3)
                                    * (4 * Math.Pow(level, 3) - 3 * Math.Pow(level, 2) - level)
                                    + Math.Floor(1.25 * Math.Pow(1.8, level - 60))
                            )
                        );
                    }
                    else
                    {
                        score = BigInteger.One;
                    }
                }
                else
                {
                    score = BigInteger.Parse("26931190829") + ONE_HUNDRED_BILLION * level - 100;
                }

                return score;
            })
            .Invoke(level);
    }

    public static int GetLevel(BigInteger score)
    {
        int left = 1;
        int right = 1_000_000;

        while (left < right)
        {
            int mid = left + (right - left) / 2;
            BigInteger requiredScore = GetRequiredScoreForLevel(mid);

            if (score >= requiredScore)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left - 1;
    }

    public static API.OSU.Models.UserLevel GetLevelWithProgress(BigInteger score)
    {
        if (score > MAX_ALLOWED_SCORE)
        {
            return new API.OSU.Models.UserLevel();
        }

        int baseLevel = GetLevel(score);
        BigInteger baseLevelScore = GetRequiredScoreForLevel(baseLevel);
        BigInteger scoreProgress = score - baseLevelScore;
        BigInteger scoreLevelDifference = GetRequiredScoreForLevel(baseLevel + 1) - baseLevelScore;

        double progress = Math.Min(1.0, (double)scoreProgress / (double)scoreLevelDifference);
        if (double.IsNaN(progress) || double.IsInfinity(progress))
        {
            progress = 0;
        }

        return new API.OSU.Models.UserLevel
        {
            Current = baseLevel,
            Progress = (int)Math.Round(progress * 100)
        };
    }
}
