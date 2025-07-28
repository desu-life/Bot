
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

await Log.CloseAndFlushAsync();
