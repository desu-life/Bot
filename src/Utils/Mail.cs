using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace KanonBot;

public static partial class Utils
{
   
    [GeneratedRegex(@"([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+")]
    private static partial Regex EmailRegex();
    public static bool IsMailAddr(string str) => EmailRegex().IsMatch(str);

    public static string HideMailAddr(string mailAddr)
    {
        try
        {
            var parts = mailAddr.Split('@');
            if (parts.Length != 2) return mailAddr;

            var local = parts[0];
            var domain = parts[1];

            var maskedLocal = MaskPart(local);
            var dotIdx = domain.IndexOf('.');
            var maskedDomain = MaskPartWithDot(domain, dotIdx);

            return $"{maskedLocal}@{maskedDomain}";
        }
        catch
        {
            return mailAddr;
        }

        static string MaskPart(ReadOnlySpan<char> part)
        {
            if (part.Length <= 2)
                return part.ToString();
            return string.Create(part.Length, part.ToString(), static (span, src) =>
            {
                span.Fill('*');
                span[0] = src[0];
                span[^1] = src[^1];
            });
        }

        static string MaskPartWithDot(ReadOnlySpan<char> part, int dotIdx)
        {
            if (part.Length <= 2)
                return part.ToString();
            return string.Create(part.Length, (part.ToString(), dotIdx), static (span, state) =>
            {
                span.Fill('*');
                span[0] = state.Item1[0];
                span[^1] = state.Item1[^1];
                if (state.dotIdx >= 0 && state.dotIdx < span.Length)
                    span[state.dotIdx] = '.';
            });
        }
    }

    public static async Task SendDebugMail(string body)
    {
        if (Config.inner!.mail is not null) {
            Mail.MailStruct ms =
                new()
                {
                    MailTo = Config.inner.mail.mailTo,
                    Subject = $"KanonBot 错误自动上报 - 发生于 {DateTime.Now}",
                    Body = body,
                    IsBodyHtml = false
                };
            try
            {
                await Mail.Send(ms);
            }
            catch { }
        }
    }

    public static async Task SendMail(IEnumerable<string> mailto, string title, string body, bool isBodyHtml)
    {
        Mail.MailStruct ms =
            new()
            {
                MailTo = mailto,
                Subject = title,
                Body = body,
                IsBodyHtml = isBodyHtml
            };
        try
        {
            await Mail.Send(ms);
        }
        catch { }
    }


}
