using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KanonBot.Serializer;

public sealed class AnyBoolConverter : JsonConverter<bool>
{
    public override bool Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // 如果 API 返回的是数字 1，则视为 true
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32() != 0;
        }
        // 如果 API 返回的是标准的布尔值，则直接读取
        return reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
