using OSU = KanonBot.API.OSU;

namespace KanonBot.Image;

public class ScorePanelData
{
    public required OSU.Models.ScoreLazer scoreInfo;
    public OsuPerformance.PPInfo? ppInfo;
    public RosuPP.Mode mode;
    public string? server;
    public double? oldPP;
    public double? playtime;
}