using KanonBot.Image;
using OSU = KanonBot.API.OSU;

namespace KanonBot.OsuPerformance;

public sealed record PerformanceCalculatorDescriptor(
    string Id,
    string DisplayName,
    IReadOnlySet<OSU.Mode> SupportedModes,
    bool IsHistorical = false
)
{
    public bool Supports(OSU.Mode mode) => SupportedModes.Contains(mode);
}

public interface IPerformanceCalculator
{
    PerformanceCalculatorDescriptor Descriptor { get; }

    ScorePanelData CalculatePerfectPanelData(
        byte[] beatmap,
        OSU.Models.Beatmap beatmapInfo,
        OSU.Models.Mod[] mods
    );

    ScorePanelData CalculatePanelData(byte[] beatmap, OSU.Models.ScoreLazer score);

    PerformanceResult CalculateData(byte[] beatmap, OSU.Models.ScoreLazer score);
}
