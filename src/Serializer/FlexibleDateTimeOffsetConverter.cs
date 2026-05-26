using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KanonBot.Serializer;

/// <summary>
/// 日期时间转换器（支持秒/毫秒时间戳、字符串时间戳、ISO日期）
/// 只需声明为 DateTimeOffset，框架会自动完美支持 DateTimeOffset?
/// </summary>
public sealed class FlexibleDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => ParseUnixTimestamp(reader.GetInt64()),
            JsonTokenType.String => ParseString(reader.GetString()),
            JsonTokenType.Null => default,

            _
                => throw new JsonException(
                    $"Unsupported date token {reader.TokenType} for timestamp field."
                )
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(value.ToUniversalTime());
    }

    private static DateTimeOffset ParseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        // 1. 尝试将字符串形式的时间戳（例如 "1716336000"）转化为 long
        if (
            long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var unixTimestamp
            )
        )
            return ParseUnixTimestamp(unixTimestamp);

        // 2. 尝试解析标准的 ISO 字符串日期
        if (
            DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed
            )
        )
        {
            return parsed;
        }

        throw new JsonException($"Invalid timestamp value '{value}'.");
    }

    private static DateTimeOffset ParseUnixTimestamp(long value)
    {
        return Math.Abs(value) >= 100_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(value)
            : DateTimeOffset.FromUnixTimeSeconds(value);
    }
}
