#pragma warning disable CS8629

using System.Text.Json;
using System.Text.Json.Nodes;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Serializer;

namespace Tests;

public class JsonMigrationTests
{
    [Fact]
    public void EnumConverter_ReadsDescription()
    {
        var json = "\"ranked\"";
        var result = JsonSerializer.Deserialize<Models.Status>(json, Json.Options);
        Assert.Equal(Models.Status.Ranked, result);
    }

    [Fact]
    public void EnumConverter_ReadsNumeric()
    {
        // Mode enum: OSU=0, Taiko=1, Fruits=2, Mania=3
        var json = "0";
        var result = JsonSerializer.Deserialize<Mode>(json, Json.Options);
        Assert.Equal(Mode.OSU, result);
    }

    [Fact]
    public void EnumConverter_WritesDescription()
    {
        var json = JsonSerializer.Serialize(Models.Status.Ranked, Json.Options);
        Assert.Equal("\"ranked\"", json);
    }

    [Fact]
    public void EnumConverter_WritesMemberNameForUnknown()
    {
        var json = JsonSerializer.Serialize(Models.Status.Unknown, Json.Options);
        Assert.Equal("\"Unknown\"", json);
    }

    [Fact]
    public void EnumTypeConverter_WorksWithoutGlobalOptions()
    {
        var payload = JsonSerializer.Deserialize<QQGuild.Models.PayloadBase<object>>("{\"t\":\"READY\"}");
        Assert.NotNull(payload);
        Assert.Equal(QQGuild.Enums.EventType.Ready, payload!.Type);

        var requestJson = JsonSerializer.Serialize(
            new OneBot.Models.CQRequest<object>
            {
                action = OneBot.Enums.Actions.SendMsg,
                Params = new { }
            }
        );
        Assert.Contains("\"action\":\"send_msg\"", requestJson);
    }

    [Fact]
    public void FlexibleDateTime_ReadsIsoString()
    {
        var json = "\"2024-01-15T10:30:00Z\"";
        var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, Json.Options);
        Assert.NotNull(result);
        Assert.Equal(2024, result.Value.Year);
        Assert.Equal(1, result.Value.Month);
        Assert.Equal(15, result.Value.Day);
    }

    [Fact]
    public void FlexibleDateTime_ReadsUnixSeconds()
    {
        var json = "1705312200"; // 2024-01-15T10:30:00Z
        var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, Json.Options);
        Assert.NotNull(result);
        Assert.Equal(2024, result.Value.Year);
    }

    [Fact]
    public void FlexibleDateTime_ReadsUnixMilliseconds()
    {
        var json = "1705312200000"; // same time in milliseconds
        var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, Json.Options);
        Assert.NotNull(result);
        Assert.Equal(2024, result.Value.Year);
    }

    [Fact]
    public void FlexibleDateTime_ReadsNull()
    {
        var json = "null";
        var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, Json.Options);
        Assert.Null(result);
    }

    [Fact]
    public void FlexibleDateTime_ReadsUnixStringValue()
    {
        var json = "\"1705312200\"";
        var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, Json.Options);
        Assert.NotNull(result);
        Assert.Equal(2024, result.Value.Year);
    }

    [Fact]
    public void StringToLong_ReadsString()
    {
        var json = "{\"user_id\": \"12345\"}";
        var result = Json.Deserialize<TestLongModel>(json);
        Assert.NotNull(result);
        Assert.Equal(12345L, result.UserId);
    }

    [Fact]
    public void StringToLong_ReadsNumber()
    {
        var json = "{\"user_id\": 12345}";
        var result = Json.Deserialize<TestLongModel>(json);
        Assert.NotNull(result);
        Assert.Equal(12345L, result.UserId);
    }

    [Fact]
    public void Beatmap_Deserialization()
    {
        var json = File.ReadAllText("Tests/TestFiles/beatmap.json");
        var result = Json.Deserialize<Models.Beatmap>(json);
        Assert.NotNull(result);
        Assert.True(result.BeatmapId > 0);
    }

    [Fact]
    public void Score_Deserialization()
    {
        var json = File.ReadAllText("Tests/TestFiles/score.json");
        var result = Json.Deserialize<Models.Score>(json);
        Assert.NotNull(result);
    }

    [Fact]
    public void User_Deserialization()
    {
        var json = File.ReadAllText("Tests/TestFiles/user.json");
        var result = Json.Deserialize<Models.UserExtended>(json);
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public void PPlusData_Deserialization()
    {
        var json = File.ReadAllText("Tests/TestFiles/ppplus.json");
        var node = JsonNode.Parse(json);
        Assert.NotNull(node);
        var userData = node!["user_data"]?.ToObject<Models.PPlusData.UserData>();
        Assert.NotNull(userData);
    }

    [Fact]
    public void JsonNode_SelectToken()
    {
        var json = "{\"a\":{\"b\":{\"c\":42}}}";
        var node = JsonNode.Parse(json);
        var result = node?.SelectToken("a.b.c");
        Assert.NotNull(result);
        Assert.Equal(42, result!.GetValue<int>());
    }

    [Fact]
    public void JsonNode_ToObject()
    {
        var json = "{\"beatmapset_id\":1,\"difficulty_rating\":5.5,\"id\":100,\"mode\":\"osu\",\"status\":\"ranked\",\"total_length\":180,\"user_id\":2,\"version\":\"Hard\",\"accuracy\":8.0,\"ar\":9.0,\"convert\":false,\"count_circles\":500,\"count_sliders\":200,\"count_spinners\":3,\"cs\":4.0,\"drain\":7.0,\"hit_length\":160,\"is_scoreable\":true,\"last_updated\":\"2024-01-01T00:00:00Z\",\"mode_int\":0,\"passcount\":100,\"playcount\":1000,\"ranked\":1,\"url\":\"https://osu.ppy.sh/beatmaps/100\"}";
        var node = JsonNode.Parse(json);
        var beatmap = node.ToObject<Models.Beatmap>();
        Assert.NotNull(beatmap);
        Assert.Equal(100L, beatmap!.BeatmapId);
        Assert.Equal(Models.Status.Ranked, beatmap.Status);
    }

    [Fact]
    public void Serialize_Roundtrip()
    {
        var mod = new Models.Mod { Acronym = "DT", Settings = new JsonObject { { "speed_change", 1.5 } } };
        var json = Json.Serialize(mod);
        var deserialized = Json.Deserialize<Models.Mod>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("DT", deserialized.Acronym);
        Assert.NotNull(deserialized.Settings);
        Assert.Equal(1.5, (double)deserialized.Settings!["speed_change"]!);
    }

    [Fact]
    public void NullHandling_OmitsNullProperties()
    {
        var beatmap = new Models.Beatmap
        {
            BeatmapId = 1,
            BeatmapsetId = 1,
            DifficultyRating = 5.0,
            Mode = Mode.OSU,
            Status = Models.Status.Ranked,
            TotalLength = 100,
            UserId = 1,
            Version = "Normal",
            OD = 8.0,
            AR = 9.0,
            Convert = false,
            CountCircles = 100,
            CountSliders = 50,
            CountSpinners = 2,
            CS = 4.0,
            HPDrain = 7.0,
            HitLength = 90,
            IsScoreable = true,
            LastUpdated = DateTimeOffset.UtcNow,
            ModeInt = 0,
            Passcount = 10,
            Playcount = 100,
            Ranked = 1,
            Url = new Uri("https://osu.ppy.sh/beatmaps/1"),
            Failtimes = new Models.BeatmapFailtimes(),
            Checksum = null, // should be omitted
            Beatmapset = null // should be omitted
        };

        var json = Json.Serialize(beatmap);
        Assert.DoesNotContain("\"checksum\"", json);
        Assert.DoesNotContain("\"beatmapset\":", json);
    }

    private class TestLongModel
    {
        [System.Text.Json.Serialization.JsonPropertyName("user_id")]
        public long UserId { get; set; }
    }
}
