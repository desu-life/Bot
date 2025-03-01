#pragma warning disable IDE1006 // 命名样式

using System.IO;
using KanonBot.Database;
using KanonBot.Serializer;
using LanguageExt.UnitsOfMeasure;
using Tomlet.Attributes;
using Destructurama;
using Destructurama.Attributed;

namespace KanonBot;

public class Config
{
    public static Base? inner;


    public class OpenAI
    {
        [TomlProperty("key")]
        public string? Key { get; set; }

        [TomlProperty("max_tokens")]
        public int MaxTokens { get; set; } //def 16.

        ///OpenAI generally recommend altering Temperature or top_p but not both.
        [TomlProperty("temperature")]
        public double Temperature { get; set; } //def 1

        [TomlProperty("top_p")]
        public double TopP { get; set; } //def 1

        [TomlProperty("pre_define")]
        public string? PreDefine { get; set; }
    }

    public class Mail
    {
        [TomlProperty("smtp_host")]
        public string? smtpHost { get; set; }

        [TomlProperty("smtp_port")]
        public int smtpPort { get; set; }

        [TomlProperty("username")]
        public string? username { get; set; }

        [TomlProperty("password")]
        public string? password { get; set; }

        [TomlProperty("mail_to")]
        public string[] mailTo { get; set; } = [];
    }

    public class Database
    {
        [TomlProperty("type")]
        public string? type { get; set; }

        [TomlProperty("host")]
        public string? host { get; set; }

        [TomlProperty("port")]
        public int port { get; set; }

        [TomlProperty("db")]
        public string? db { get; set; }

        [TomlProperty("user")]
        public string? user { get; set; }

        [TomlProperty("password")]
        public string? password { get; set; }
    }

    public class OSU
    {
        [TomlProperty("client_id")]
        public int clientId { get; set; }

        [TomlProperty("client_secret")]
        public required string clientSecret { get; set; }

        [TomlProperty("v1_key")]
        public string? v1key { get; set; }

        [TomlProperty("v2_end_point")]
        public string? v2EndPoint { get; set; }
    }

    public class OSS
    {
        [TomlProperty("url")]
        public string? url { get; set; }

        [TomlProperty("access_key_id")]
        public string? accessKeyId { get; set; }

        [TomlProperty("access_key_secret")]
        public string? accessKeySecret { get; set; }

        [TomlProperty("end_point")]
        public string? endPoint { get; set; }

        [TomlProperty("bucket_name")]
        public string? bucketName { get; set; }
    }

    public enum ConfigType
    {
        OneBotServer,
        OneBotClient,
        Guild,
        Kook,
    }

    public class DriverConfig {
        [TomlDoNotInlineObject]
        [TomlProperty("onebot_server")]
        [NotLoggedIfDefault]
        public OneBotServer? OneBotServer { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("onebot_client")]
        [NotLoggedIfDefault]
        public OneBotClient? OneBotClient { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("guild")]
        [NotLoggedIfDefault]
        public Guild? Guild { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("kook")]
        [NotLoggedIfDefault]
        public KOOK? KOOK { get; set; }
        
        [TomlDoNotInlineObject]
        [TomlProperty("discord")]
        [NotLoggedIfDefault]
        public Discord? Discord { get; set; }

        [TomlNonSerialized]
        [NotLogged]
        public IDriverConfig Config => OneBotServer ?? OneBotClient ?? Guild ?? Discord ?? (IDriverConfig)KOOK!;

        public DriverConfig(IDriverConfig config) { 
            switch (config)
            {
                case OneBotServer c: OneBotServer = c; break;
                case OneBotClient c: OneBotClient = c; break;
                case Guild c: Guild = c; break;
                case KOOK c: KOOK = c; break;
                case Discord c: Discord = c; break;
            }
        }
    }

    public interface IDriverConfig;

    public class OneBotServer : IDriverConfig
    {
        [TomlProperty("host")]
        public string[] host { get; set; } = ["localhost"];

        [TomlProperty("port")]
        public int port { get; set; } = 7700;

        [TomlProperty("elevated")]
        public bool elevated { get; set; } = false;
    }
    public class OneBotClient : IDriverConfig
    {
        [TomlProperty("host")]
        public string host { get; set; } = "localhost";

        [TomlProperty("port")]
        public int port { get; set; } = 6700;

        [TomlProperty("http_port")]
        public int httpPort { get; set; } = 5700;
    }

    public class Guild : IDriverConfig
    {
        [TomlProperty("sandbox")]
        public bool sandbox { get; set; }

        [TomlProperty("app_id")]
        public long appID { get; set; }

        [TomlProperty("secret")]
        public string? secret { get; set; }

        [TomlProperty("token")]
        public string? token { get; set; }
    }

    public class KOOK : IDriverConfig
    {
        [TomlProperty("bot_id")]
        public string? botID { get; set; }

        [TomlProperty("token")]
        public string? token { get; set; }
    }

    public class Discord : IDriverConfig
    {
        [TomlProperty("bot_id")]
        public string? botID { get; set; }

        [TomlProperty("token")]
        public string? token { get; set; }
    }

    public class Base
    {
        [TomlProperty("debug")]
        public bool debug { get; set; }

        [TomlProperty("dev")]
        public bool dev { get; set; }
        [TomlProperty("calc_old_pp")]
        public bool calcOldPP { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("osu")]
        public OSU? osu { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("oss")]
        public OSS? oss { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("database")]
        public Database? database { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("mail")]
        public Mail? mail { get; set; }

        [TomlDoNotInlineObject]
        [TomlProperty("openai")]
        public OpenAI? openai { get; set; }

        [TomlProperty("drivers")]
        public DriverConfig[] drivers { get; set; } = [];

        public static Base Default()
        {
            return new Base()
            {
                debug = true,
                dev = true,
                calcOldPP = false,
                osu = new()
                {
                    clientId = 0,
                    clientSecret = "",
                    v1key = "",
                    v2EndPoint = "https://osu.ppy.sh/api/v2/"
                },
                drivers =
                [
                    new(new OneBotClient()
                    {
                        host = "localhost",
                        httpPort = 5700,
                        port = 6700
                    }),
                    new(new OneBotServer()
                    {
                        host = ["localhost"],
                        port = 7700
                    }),
                    new(new Guild()
                    {
                        appID = 0,
                        secret = "",
                        token = "",
                        sandbox = true
                    }),
                    new(new KOOK() { botID = "", token = "" }),
                    new(new Discord() { botID = "", token = "" }),
                ],
                oss = new()
                {
                    url = "",
                    accessKeyId = "",
                    accessKeySecret = "",
                    endPoint = "",
                    bucketName = ""
                },
                database = new()
                {
                    type = "mysql",
                    host = "",
                    port = 3306,
                    db = "kanonbot",
                    user = "",
                    password = ""
                },
                mail = new()
                {
                    smtpHost = "localhost",
                    smtpPort = 587,
                    username = "",
                    password = ""
                },
                openai = new()
                {
                    Key = "",
                    MaxTokens = 16,
                    Temperature = 0,
                    TopP = 1,
                    PreDefine = ""
                }
            };
        }

        public void save(string path)
        {
            using var f = new StreamWriter(path);
            f.Write(this.ToString());
        }

        public override string ToString()
        {
            return Toml.Serialize(this);
        }

        public string ToJson()
        {
            return Json.Serialize(this);
        }
    }

    public static Base load(string path)
    {
        string c;
        using (var f = File.OpenText(path))
        {
            c = f.ReadToEnd();
        }
        return Toml.Deserialize<Base>(c);
    }
}
