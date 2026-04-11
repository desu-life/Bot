using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ComponentModel;
using API = KanonBot.API;
using KanonBot.Serializer;
using KanonBot.Drivers;
using KanonBot;
using Newtonsoft.Json.Linq;
using Msg = KanonBot.Message;
using Img = KanonBot.Image;
using SixLabors.ImageSharp;

namespace Tests;

public class Misc
{
    private readonly ITestOutputHelper Output;
    public Misc(ITestOutputHelper Output)
    {
        this.Output = Output;
        var configPath = "./config.toml";
        if (File.Exists(configPath))
        {
            Config.inner = Config.load(configPath);
        }
        else
        {
            System.IO.Directory.SetCurrentDirectory("../../../../");
            Config.inner = Config.load(configPath);
        }
    }

    [Fact]
    public void UtilsTest()
    {
        Assert.Equal("osu", Utils.GetObjectDescription(API.OSU.Mode.OSU));
        Output.WriteLine(Utils.ForStarDifficulty(1.25).ToString());
        Output.WriteLine(Utils.ForStarDifficulty(2).ToString());
        Output.WriteLine(Utils.ForStarDifficulty(2.5).ToString());
        Output.WriteLine(Utils.ForStarDifficulty(3).ToString());
        Output.WriteLine(Utils.ForStarDifficulty(3.5).ToString());
    }

        [Fact]
        public void IamBindingsTimestampDeserialize()
        {
                var json = """
                {
                    "userId": "d505966d-54c3-40ed-bd90-e93b99b07398",
                    "userName": "Zh_Jk",
                    "displayName": "水瓶",
                    "avatarUrl": "/avatars/d505966d-54c3-40ed-bd90-e93b99b07398?v=0",
                    "createAt": 1775382990862,
                    "lastLoginAt": null,
                    "bindings": {
                        "qq": "1071814607",
                        "discord": "972128335851294782",
                        "qqGuild": "11174521116880165171",
                        "osu": "9037287",
                        "ppySb": null
                    }
                }
                """;

                var result = Json.Deserialize<KanonBot.API.IAM.UserBindingsResponse>(json);

                Assert.NotNull(result);
                Assert.Equal("Zh_Jk", result!.UserName);
                Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1775382990862), result.CreateAt);
                Assert.Null(result.LastLoginAt);
                Assert.Equal("9037287", result.Bindings.Osu);
        }

    [Fact]
    public void MsgChain()
    {
        var c = new Msg.Chain().msg("hello").image("C:\\hello.png", Msg.ImageSegment.Type.Url).msg("test\nhaha");
        c.Add(new Msg.RawSegment("Test", new JObject { { "test", "test" } }));
        Assert.True(c.StartsWith("he"));
        Assert.False(c.StartsWith("!"));
        c = new Msg.Chain().at("zhjk", Platform.OneBot);
        Assert.True(c.StartsWith(new Msg.AtSegment("zhjk", Platform.OneBot)));

        var c1 = OneBot.Message.Build(c);
        Assert.Equal("[{\"type\":\"at\",\"data\":{\"qq\":\"zhjk\"}}]", Json.Serialize(c1));
        var c2 = OneBot.Message.Parse(c1);
        Assert.Equal("qq=zhjk", c2.ToString());
    }

    // [Fact]
    // public void Mail()
    // {
    //     // 邮件测试
    //     KanonBot.Mail.MailStruct ms = new()
    //     {
    //         MailTo = new string[] { "deleted" },
    //         Subject = "你好！",
    //         Body = "你好！这是一封来自猫猫的测试邮件！"
    //     };
    //     KanonBot.Mail.Send(ms);
    // }

}

