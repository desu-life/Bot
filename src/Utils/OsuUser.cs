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
}