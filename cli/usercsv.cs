using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace cli;

public class UserInfo
{
    [Name("qq")]
    public required string QQ { get; set; }

    [Name("osu_id")]
    public long? OsuId { get; set; }

    [Name("osu_name")]
    public string? OsuName { get; set; }

    [Name("osu_pp")]
    public double? OsuPP { get; set; }

    [Name("ppysb_id")]
    public long? PpysbId { get; set; }

    [Name("ppysb_name")]
    public string? PpysbName { get; set; }

    [Name("ppysb_pp")]
    public double? PpysbPP { get; set; }

    [Name("ppysb_rxpp")]
    public double? PpysbRxPP { get; set; }
}

public static class UserProcessor
{
    public static async Task QueryUserToCsv()
    {
        var users = File.ReadAllLines("users.txt")
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim());
        var semaphore = new SemaphoreSlim(5, 5);
        var tasks = users.Select(
            async (user_qq) =>
            {
                var user_info = new UserInfo { QQ = user_qq };

                var user = await KanonBot.Database.Client.GetUsersByUID(
                    user_qq,
                    KanonBot.Drivers.Platform.OneBot
                );

                if (user is not null)
                {
                    var osuDBUser = await KanonBot.Database.Client.GetOsuUserByUID(user.uid);
                    var ppysbDBUser = await KanonBot.Database.Client.GetPpysbUserByUID(user.uid);

                    Log.Information("{@0}", (ppysbDBUser, osuDBUser));

                    if (osuDBUser is not null)
                    {
                        var osuUser = await KanonBot.API.OSU.Client.GetUser(
                            osuDBUser.osu_uid,
                            KanonBot.API.OSU.Mode.OSU
                        );
                        if (osuUser is not null)
                        {
                            user_info.OsuId = osuUser.Id;
                            user_info.OsuName = osuUser.Username;
                            user_info.OsuPP = osuUser.Statistics.PP;
                        }
                    }

                    if (ppysbDBUser is not null)
                    {
                        var ppysbUser = await KanonBot.API.PPYSB.Client.GetUser(
                            ppysbDBUser.osu_uid
                        );
                        if (ppysbUser is not null)
                        {
                            user_info.PpysbId = ppysbUser.Info.Id;
                            user_info.PpysbName = ppysbUser.Info.Name;
                            user_info.PpysbPP = ppysbUser.Stats.StatOsu?.PP;
                            user_info.PpysbRxPP = ppysbUser.Stats.StatRelaxOsu?.PP;
                        }
                    }
                }

                return user_info;
            }
        );

        var records = await Task.WhenAll(tasks);

        using (var writer = new StreamWriter("./users.csv"))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<UserInfo>();
            csv.NextRecord();
            foreach (var record in records)
            {
                csv.WriteRecord(record);
                csv.NextRecord();
            }
        }
    }
}
