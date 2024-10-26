#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;

namespace KanonBot.API.OSU;

[DefaultValue(OSU)] // 解析失败就osu
public enum Mode
{
    [Description("osu")]
    OSU,

    [Description("taiko")]
    Taiko,

    [Description("fruits")]
    Fruits,

    [Description("mania")]
    Mania,
}
