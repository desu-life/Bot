namespace KanonBot;

public static partial class Utils
{
    public static string GetTimeStamp(bool isMillisec) =>
        isMillisec
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public static DateTimeOffset TimeStampMilliToDateTime(int timeStamp) =>
        DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);

    public static DateTimeOffset TimeStampSecToDateTime(long timeStamp) =>
        DateTimeOffset.FromUnixTimeSeconds(timeStamp);

    public enum DurationFormat
    {
        DayFull,           // "1d 2h 3m 4s"
        DayNoSec,          // "1d 2h 3m"
        HourFull,          // "2h 3m 4s" or "3m 4s"
        HourNoSec,         // "2h 3m" or "3m"
        TimeColon,         // "2:03:04" or "3:04"
        TimeScoreV3,       // "2H,03M,04S" or "3M,04S"
    }

    public static string FormatDuration(long totalSeconds, DurationFormat format)
    {
        long day = totalSeconds / 86400;
        long rem = totalSeconds % 86400;
        long hour = rem / 3600;
        rem %= 3600;
        long minute = rem / 60;
        long second = rem % 60;
        long totalHour = totalSeconds / 3600;

        return format switch
        {
            DurationFormat.DayFull => $"{day}d {hour}h {minute}m {second}s",
            DurationFormat.DayNoSec => $"{day}d {hour}h {minute}m",
            DurationFormat.HourFull => hour > 0 ? $"{totalHour}h {minute}m {second}s" : $"{minute}m {second}s",
            DurationFormat.HourNoSec => hour > 0 ? $"{totalHour}h {minute}m" : $"{minute}m",
            DurationFormat.TimeColon => hour > 0 ? $"{totalHour}:{minute:00}:{second:00}" : $"{minute}:{second:00}",
            DurationFormat.TimeScoreV3 => hour > 0 ? $"{totalHour}H,{minute:00}M,{second:00}S" : $"{minute}M,{second:00}S",
            _ => $"{totalHour}h {minute}m {second}s"
        };
    }

    // Keep existing method signatures as thin wrappers for backward compatibility
    public static string DayDuration2String(long duration) => FormatDuration(duration, DurationFormat.DayFull);
    public static string DayDuration2StringWithoutSec(long duration) => FormatDuration(duration, DurationFormat.DayNoSec);
    public static string Duration2String(long duration) => FormatDuration(duration, DurationFormat.HourFull);
    public static string Duration2StringWithoutSec(long duration) => FormatDuration(duration, DurationFormat.HourNoSec);
    public static string Duration2TimeString(long duration) => FormatDuration(duration, DurationFormat.TimeColon);
    public static string Duration2TimeStringForScoreV3(long duration) => FormatDuration(duration, DurationFormat.TimeScoreV3);
}
