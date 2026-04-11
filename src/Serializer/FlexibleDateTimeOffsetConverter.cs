using System.Globalization;
using Newtonsoft.Json;

namespace KanonBot.Serializer;

public sealed class FlexibleDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? ReadJson(
        JsonReader reader,
        Type objectType,
        DateTimeOffset? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        return reader.TokenType switch
        {
            JsonToken.Null => null,
            JsonToken.Integer => ParseUnixTimestamp(Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture)),
            JsonToken.String => ParseString(reader.Value?.ToString()),
            JsonToken.Date => ParseDateToken(reader.Value),
            _ => throw new JsonSerializationException(
                $"Unsupported date token {reader.TokenType} for timestamp field."
            )
        };
    }

    public override void WriteJson(JsonWriter writer, DateTimeOffset? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(value.Value.UtcDateTime);
    }

    private static DateTimeOffset? ParseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTimestamp))
            return ParseUnixTimestamp(unixTimestamp);

        if (
            DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed
            )
        )
            return parsed;

        throw new JsonSerializationException($"Invalid timestamp value '{value}'.");
    }

    private static DateTimeOffset? ParseDateToken(object? value)
    {
        return value switch
        {
            null => null,
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime(),
            DateTime dateTime => new DateTimeOffset(
                dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime
            ).ToUniversalTime(),
            _ => throw new JsonSerializationException($"Invalid date token value '{value}'.")
        };
    }

    private static DateTimeOffset ParseUnixTimestamp(long value)
    {
        return Math.Abs(value) >= 100_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(value)
            : DateTimeOffset.FromUnixTimeSeconds(value);
    }
}