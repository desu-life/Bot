
using cli;
using Flurl.Http.Newtonsoft;
using KanonBot;

Console.WriteLine("---KanonBot---");
var configPath = "config.toml";
if (File.Exists(configPath))
{
    Config.inner = Config.load(configPath);
}
else
{
    Config.inner = Config.Base.Default();
    Config.inner.save(configPath);
}

FlurlHttp
    .Clients.UseNewtonsoft()
    .WithDefaults(c =>
    {
        c.Settings.Redirects.Enabled = true;
        c.Settings.Redirects.MaxAutoRedirects = 10;
        c.Settings.Redirects.ForwardAuthorizationHeader = true;
        c.Settings.Redirects.AllowSecureToInsecure = true;
    });

var config = Config.inner;

var log = new LoggerConfiguration().WriteTo.Async(a => a.Console());
log = log.MinimumLevel.Debug();
Log.Logger = log.CreateLogger();


await KanonBot.API.OSU.Client.CheckToken();

// await UserProcessor.QueryUserToCsv();

var users = File.ReadAllLines("users.txt")
    .Where(line => !string.IsNullOrWhiteSpace(line))
    .Select(line => long.Parse(line.Trim()));

foreach (var uid in users)
{
    var u = await KanonBot.Database.Client.GetUserByOsuUID(uid);
    if (u is not null)
    {
        var ppysb = await KanonBot.Database.Client.GetPpysbUserByUID(u.uid);
        if (ppysb is not null)
        {
            var ppyonline = await KanonBot.API.OSU.Client.GetUser(uid, KanonBot.API.OSU.Mode.OSU);
            var online = await KanonBot.API.PPYSB.Client.GetUser(ppysb.uid);

            var osupp = ppyonline!.Statistics.PP;
            var ppysbpp = online?.Stats.StatOsu?.PP;
            var ppysbpprx = online?.Stats.StatRelaxOsu?.PP;
            Console.WriteLine(ppysb.uid);
            Console.WriteLine($"{uid}的官方pp为{osupp}, 私服pp为{ppysbpp ?? 0}  rx: {ppysbpprx ?? 0}");
        }
        else
        {
            Console.WriteLine($"{uid}未绑定ppysb");
        }

    }
    else
    {
        
        Console.WriteLine($"{uid}未绑定");
    }
}

await Log.CloseAndFlushAsync();
