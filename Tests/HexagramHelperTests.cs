using System.Globalization;
using KanonBot.Image.Takumi;

namespace Tests;

public class HexagramHelperTests
{
    [Fact]
    public void CalculatePoints_MatchesOriginalFormula()
    {
        var ppd = new[] { 787.56, 919.77, 1965.46, 451.25, 1242.41, 1042.38 };
        var multi = new[] { 14.1, 69.7, 1.92, 19.8, 0.588, 3.06 };
        var exp = new[] { 0.769, 0.596, 0.953, 0.8, 1.175, 0.993 };

        var points = HexagramHelper.CalculatePoints(ppd, multi, exp, 6, 200, 10000, 1);

        var expected = CalculateWithOriginalFormula(ppd, multi, exp, 6, 200, 10000, 1);

        Assert.Equal(6, points.Count);
        for (var i = 0; i < points.Count; i++)
            AssertPoint(points[i], expected[i].X, expected[i].Y);
    }

    [Fact]
    public void ToSvgPoints_ProducesValidSvgFormat()
    {
        var values = new[] { 1d, 2d, 3d, 4d, 5d, 6d };
        var weights = new[] { 1d, 1d, 1d, 1d, 1d, 1d };
        var exp = new[] { 1d, 1d, 1d, 1d, 1d, 1d };

        var points = HexagramHelper.ToSvgPoints(values, weights, exp, 6, 200, 10000, 1);
        var pairs = points.Split(' ');

        Assert.Equal(6, pairs.Length);
        Assert.All(pairs, pair =>
        {
            var xy = pair.Split(',');
            Assert.Equal(2, xy.Length);
            _ = double.Parse(xy[0], CultureInfo.InvariantCulture);
            _ = double.Parse(xy[1], CultureInfo.InvariantCulture);
        });
    }

    private static void AssertPoint((double X, double Y) actual, double expectedX, double expectedY)
    {
        Assert.Equal(expectedX, actual.X, 3);
        Assert.Equal(expectedY, actual.Y, 3);
    }

    private static List<(double X, double Y)> CalculateWithOriginalFormula(
        double[] ppd,
        double[] multi,
        double[] exp,
        int nodeCount,
        int size,
        int nodeMaxValue,
        int mode)
    {
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
            points.Add(
                (
                    r * Math.Sin(angle * Math.PI / 180) + size / 2.0,
                    r * Math.Cos(angle * Math.PI / 180) + size / 2.0
                )
            );
        }

        return points;
    }
}
