using System.Text.Json;
using System.Text.Json.Serialization;

namespace KanonBot.Serializer;

public sealed class SafeJsonStringEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly JsonConverter<TEnum> inner;
    private readonly TEnum fallback;

    public SafeJsonStringEnumConverter()
        : this(default, null, true) { }

    public SafeJsonStringEnumConverter(
        TEnum fallback,
        JsonNamingPolicy? namingPolicy = null,
        bool allowIntegerValues = true
    )
    {
        this.fallback = fallback;

        inner =
            (JsonConverter<TEnum>)
                new JsonStringEnumConverter<TEnum>(
                    namingPolicy,
                    allowIntegerValues
                ).CreateConverter(typeof(TEnum), new JsonSerializerOptions());
    }

    public override TEnum Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        try
        {
            return inner.Read(ref reader, typeToConvert, options);
        }
        catch (JsonException)
        {
            Log.Warning(
                "Failed to parse enum value for type {0}, using fallback value {1}.",
                typeof(TEnum).Name,
                fallback
            );
            return fallback;
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        inner.Write(writer, value, options);
    }
}
