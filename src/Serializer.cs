using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Tomlet;
using Tomlet.Exceptions;
using Tomlet.Models;

namespace KanonBot.Serializer;

public static class Json
{
    public static string Serialize(object? self) =>
        JsonConvert.SerializeObject(self, Settings.Json);

    public static string Serialize(object? self, Formatting format) =>
        JsonConvert.SerializeObject(self, format);

    public static T? Deserialize<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, Settings.Json);

    public static JObject ToLinq(string json) => JObject.Parse(json);
}

public static class Toml
{
    public static string Serialize(object self) => TomletMain.TomlStringFrom(self);
    public static T Deserialize<T>(string toml)
        where T : class, new() => TomletMain.To<T>(toml);
}

internal static class Settings
{
    public static readonly JsonSerializerSettings Json =
        new()
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            Formatting = Formatting.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
}

// https://justsimplycode.com/2021/08/01/custom-json-converter-to-de-serialise-enum-description-value-to-enum-value/
public class JsonEnumConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return enumType.IsEnum;
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        if (reader.Value is long)
            return Enum.ToObject(objectType, reader.Value);

        string description = reader.Value?.ToString() ?? string.Empty;

        if (description is null)
            return null;

        foreach (var field in objectType.GetFields())
        {
            if (
                Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                is DescriptionAttribute attribute
            )
            {
                if (attribute.Description == description)
                    return field.GetValue(null);
            }
            else
            {
                if (field.Name == description)
                    return field.GetValue(null);
            }
        }

        Log.Warning("Unknown Json Enum Value: {0}", description);
        // throw new ArgumentException("Not found.", nameof(description));
        return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (string.IsNullOrEmpty(value!.ToString()))
        {
            writer.WriteValue("");
            return;
        }
        writer.WriteValue(Utils.GetDesc(value));
    }
}
