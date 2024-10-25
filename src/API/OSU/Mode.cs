#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;

namespace KanonBot.API.OSU;

// 枚举部分
[DefaultValue(Unknown)] // 解析失败就unknown
public enum Mode
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")]
    Unknown,

    [Description("osu")]
    OSU,

    [Description("taiko")]
    Taiko,

    [Description("fruits")]
    Fruits,

    [Description("mania")]
    Mania,
}
