using KanonBot.Functions;

namespace KanonBot;

public static partial class Utils
{
    /// <summary>
    /// Resolve user information for either osu! or ppy.sb route from a unified command result.
    /// </summary>
    public static async Task<(API.OSU.Models.UserExtended? OsuUser, API.PPYSB.Models.User? PpysbUser)> ResolveOsuUser(
        Accounts.CommandUserResult resolved
    )
    {
        if (resolved.IsPpysb)
        {
            var sbUser = await API.PPYSB.Client.GetUser(resolved.OsuId);
            return (sbUser?.ToOsu(resolved.SbMode), sbUser);
        }

        return (
            await API.OSU.Client.GetUser(resolved.OsuId, resolved.Mode!.Value),
            null
        );
    }

    public static (double scorePP, double bonusPP, int rankedScores) CalculateBonusPP(IEnumerable<API.OSU.Models.ScoreLazer> allBP, API.OSU.Models.UserExtended OnlineOsuInfo)
    {
        const double BonusCoefficient = 417.0 - 1.0 / 3.0;
        double scorePP = allBP.Select((s, i) => (s.pp ?? 0.0) * Math.Pow(0.95, i)).Sum();
        double bonusPP = OnlineOsuInfo.Statistics.PP - scorePP;
        bonusPP = Math.Clamp(bonusPP, 0.0, 413.894);
        bool max = bonusPP >= 413.894 - 0.01; // 留一点浮点误差余量
        int rankedScores = max ? 1000 : (int)Math.Round(Math.Log(1.0 - bonusPP / BonusCoefficient) / Math.Log(0.995));
        var finalBonusPP = bonusPP;
        return (scorePP, finalBonusPP, rankedScores);
    }
}