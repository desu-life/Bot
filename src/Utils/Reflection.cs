using System.ComponentModel;
using System.Reflection;

namespace KanonBot;

public static partial class Utils
{
    public static string GetDesc(object? value)
    {
        FieldInfo? fieldInfo = value!.GetType().GetField(value.ToString()!);
        if (fieldInfo == null)
            return string.Empty;
        DescriptionAttribute[] attributes = (DescriptionAttribute[])
            fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : string.Empty;
    }

    public static string? GetObjectDescription(Object value)
    {
        foreach (var field in value.GetType().GetFields())
        {
            // 获取object的类型，并遍历获取DescriptionAttribute
            // 提取出匹配的那个
            if (
                Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                is DescriptionAttribute attribute
            )
            {
                if (field.GetValue(null)?.Equals(value) ?? false)
                    return attribute.Description;
            }
        }
        return null;
    }

}
