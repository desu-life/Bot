using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KanonBot.Serializer;

public sealed class FallbackJsonStringEnumConverter<TEnum> : JsonConverterFactory
    where TEnum : struct, Enum
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TEnum);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new FallbackJsonStringEnumConverterInner<TEnum>();
    }
}

public sealed class FallbackJsonStringEnumConverterInner<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly JsonConverter<TEnum> inner;

    public FallbackJsonStringEnumConverterInner()
    {
        inner =
            (JsonConverter<TEnum>)
                new JsonStringEnumConverter<TEnum>().CreateConverter(
                    typeof(TEnum),
                    new JsonSerializerOptions()
                );
    }

    public override TEnum Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var readerCopy = reader;

        try
        {
            return inner.Read(ref reader, typeToConvert, options);
        }
        catch (JsonException)
        {
            var rawValue = readerCopy.TokenType switch
            {
                JsonTokenType.String => readerCopy.GetString() ?? "null",
                JsonTokenType.Number => Encoding.UTF8.GetString(readerCopy.ValueSpan),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.Null => "null",
                _ => readerCopy.TokenType.ToString(),
            };
            Log.Warning(
                "Failed to parse enum value '{RawValue}' for type {EnumType}, using fallback value {Fallback}.",
                rawValue,
                typeof(TEnum).Name,
                default(TEnum)
            );

            return default;
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        inner.Write(writer, value, options);
    }
}
