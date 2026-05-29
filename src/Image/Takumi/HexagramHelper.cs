using System.Globalization;

namespace KanonBot.Image.Takumi;

public static class HexagramHelper
{
    public static string ToSvgPoints(
        double[] ppd,
        double[] multi,
        double[] exp,
        int nodeCount,
        int size,
        int nodeMaxValue,
        int mode)
    {
        return string.Join(
            " ",
            CalculatePoints(ppd, multi, exp, nodeCount, size, nodeMaxValue, mode)
                .Select(p =>
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{p.X:0.###},{p.Y:0.###}"
                    )
                )
        );
    }

    public static List<(double X, double Y)> CalculatePoints(
        double[] ppd,
        double[] multi,
        double[] exp,
        int nodeCount,
        int size,
        int nodeMaxValue,
        int mode)
    {
        ArgumentNullException.ThrowIfNull(ppd);
        ArgumentNullException.ThrowIfNull(multi);
        ArgumentNullException.ThrowIfNull(exp);

        if (ppd.Length < nodeCount || multi.Length < nodeCount || exp.Length < nodeCount)
            throw new ArgumentException("Input arrays must contain at least nodeCount values.");

        var points = new List<(double X, double Y)>(nodeCount);
        for (var i = 0; i < nodeCount; i++)
        {
            var r =
                Math.Pow((multi[i] * Math.Pow(ppd[i], exp[i]) / nodeMaxValue), 0.8)
                * size
                / 2.0;

            if (mode == 1 && r > 100.00)
                r = 100.00;
            if (mode == 2 && r > 395.00)
                r = 395.00;
            if (mode == 3 && r > 495.00)
                r = 495.00;

            var angle = 360.0 / nodeCount * i + 90;
            var x = r * Math.Sin(angle * Math.PI / 180) + size / 2.0;
            var y = r * Math.Cos(angle * Math.PI / 180) + size / 2.0;
            points.Add((x, y));
        }

        return points;
    }
}
