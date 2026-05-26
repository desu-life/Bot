#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace KanonBot.API.OSU;

[DefaultValue(OSU)] // 解析失败就osu
[JsonConverter(typeof(JsonStringEnumConverter<Mode>))]
public enum Mode
{
    [JsonStringEnumMemberName("osu")]
    OSU,

    [JsonStringEnumMemberName("taiko")]
    Taiko,

    [JsonStringEnumMemberName("fruits")]
    Fruits,

    [JsonStringEnumMemberName("mania")]
    Mania,
}
