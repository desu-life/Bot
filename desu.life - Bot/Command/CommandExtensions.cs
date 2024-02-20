namespace desu_life_Bot.Command;

public static class Extensions
{
    public static TypeCode GetTypeCodeEx(this Type type)
    {
        return Type.GetTypeCode(type);
    }

    public static bool IsNullable(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsFloatType(this Type type)
    {
        if (type.IsNullable())
            type = type.GetGenericArguments()[0];

        switch (type.GetTypeCodeEx())
        {
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal: return true;
        }

        return false;
    }

    public static bool IsIntegerType(this Type type)
    {
        if (type.IsNullable())
            type = type.GetGenericArguments()[0];

        switch (type.GetTypeCodeEx())
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64: return true;
        }

        return false;
    }
}