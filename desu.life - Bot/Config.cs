using desu.life_Bot.Serializer;
using Tomlyn.Model;

namespace desu.life_Bot;

/// <summary>
/// 提供配置管理功能，包括配置的加载、保存以及默认配置的生成。
/// 并通过TOML格式进行序列化和反序列化。
/// 此类设计为静态成员Inner的容器，以便全局访问配置数据。
/// </summary>
public class Config
{
    private static Base? inner;

    public static Base? Inner
    {
        get => inner;
        set
        {
            if (inner != null)
                inner = value;
        }
    }

    public class OneBot : ITomlMetadataProvider
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public int HttpPort { get; set; }
        public int ServerPort { get; set; }
        public long? ManagementGroup { get; set; }
        TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
    }

    public class Guild : ITomlMetadataProvider
    {
        public bool Sandbox { get; set; }
        public long AppID { get; set; }
        public string? Secret { get; set; }
        public string? Token { get; set; }
        TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
    }

    public class Discord : ITomlMetadataProvider
    {
        public long AppID { get; set; }
        public string? Secret { get; set; }
        public string? Token { get; set; }
        TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
    }

    public class Base : ITomlMetadataProvider
    {
        public bool Debug { get; set; }
        public OneBot? Onebot { get; set; }
        public Guild? Guild { get; set; }
        public Discord? Discord { get; set; }

        TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }

        public static Base Default()
        {
            return new Base()
            {
                Debug = true,
                Onebot = new()
                {
                    ManagementGroup = 0,
                    Host = "localhost",
                    ServerPort = 7700,
                    HttpPort = 5700,
                    Port = 6700
                },
                Guild = new()
                {
                    AppID = 0,
                    Secret = "",
                    Token = "",
                    Sandbox = true
                },
                Discord = new()
                {
                    AppID = 0,
                    Secret = "",
                    Token = "",
                }
            };
        }

        public void Save(string path)
        {
            using var f = new StreamWriter(path);
            f.Write(ToString());
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

    public static Base Load(string path)
    {
        string c;
        using (var f = File.OpenText(path))
        {
            c = f.ReadToEnd();
        }
        return Toml.Deserialize<Base>(c);
    }
}
