using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Tomlet;

namespace KanonBot.Serializer;

public static class Json
{
    public static readonly JsonSerializerOptions Options =
        new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            AllowTrailingCommas = true,
            Converters = { new FlexibleDateTimeOffsetConverter(), new AnyBoolConverter(), },
        };

    public static string Serialize<T>(T self) => JsonSerializer.Serialize(self, Options);

    public static string Serialize(object? self) =>
        JsonSerializer.Serialize(self, self?.GetType() ?? typeof(object), Options);

    public static string Serialize<T>(T self, bool indented)
    {
        if (!indented)
            return JsonSerializer.Serialize(self, Options);
        var opts = new JsonSerializerOptions(Options) { WriteIndented = true };
        return JsonSerializer.Serialize(self, opts);
    }

    public static T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException ex)
        {
            Log.Error("反序列化失败，输入: {0}\n异常: {1}", json, ex);
            return default;
        }
        catch (Exception ex)
        {
            Log.Error("未捕获的异常 ↓\n输入: {0}\n异常: {1}", json, ex);
            return default;
        }
    }

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new JsonException("Deserialize returned null");

    public static JsonNode? ToLinq(string json) => JsonNode.Parse(json);
}

public static class Toml
{
    public static string Serialize(object self) => TomletMain.TomlStringFrom(self);

    public static T Deserialize<T>(string toml)
        where T : class, new() => TomletMain.To<T>(toml);
}
