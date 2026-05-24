using OSU = KanonBot.API.OSU;

namespace KanonBot.Image;

public class ScorePanelData
{
    public OsuPerformance.PPInfo? ppInfo;
    public OSU.Models.ScoreLazer? scoreInfo;
    public RosuPP.Mode mode;
    public string? server;
    public double? oldPP;
    public double? playtime;
}