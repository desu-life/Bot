using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace KanonBot;

public static partial class Utils
{
    public static string GetDesc(object? value)
    {
        if (value is null)
            return string.Empty;

        FieldInfo? fieldInfo = value.GetType().GetField(value.ToString()!);
        if (fieldInfo is null)
            return string.Empty;

        return GetEnumMemberName(fieldInfo);
    }

    public static string? GetObjectDescription(object value)
    {
        foreach (var field in value.GetType().GetFields())
        {
            if (field.GetValue(null)?.Equals(value) ?? false)
                return GetEnumMemberName(field);
        }

        return null;
    }

    private static string GetEnumMemberName(FieldInfo fieldInfo)
    {
        var jsonName = fieldInfo.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
        if (jsonName is not null)
            return jsonName.Name;

        var description = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
        return description?.Description ?? string.Empty;
    }
}