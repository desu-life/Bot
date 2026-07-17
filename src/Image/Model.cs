using OSU = KanonBot.API.OSU;

namespace KanonBot.Image;

public class ScorePanelData
{
    public required OSU.Models.ScoreLazer scoreInfo;
    public OsuPerformance.PerformanceResult? performance;
    public RosuPP.Mode mode;
    public string? server;
    public IReadOnlyList<OsuPerformance.PerformanceComparison> comparisons = [];
    public double? playtime;
}
