using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace KanonBot;

public static partial class Utils
{
   
    [GeneratedRegex(@"([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+")]
    private static partial Regex EmailRegex();
    public static bool IsMailAddr(string str)
    {
        if (EmailRegex().IsMatch(str))
            return true;
        return false;
    }


    public static string HideMailAddr(string mailAddr)
    {
        try
        {
            var t1 = mailAddr.Split('@');
            string[] t2 = new string[t1[0].Length];
            for (int i = 0; i < t1[0].Length; i++)
            {
                t2[i] = "*";
            }
            t2[0] = t1[0][0].ToString();
            t2[t1[0].Length - 1] = t1[0][^1].ToString();
            string ret = "";
            foreach (string s in t2)
            {
                ret += s;
            }
            ret += "@";
            t2 = new string[t1[1].Length];
            for (int i = 0; i < t1[1].Length; i++)
            {
                t2[i] = "*";
            }
            t2[0] = t1[1][0].ToString();
            t2[t1[1].Length - 1] = t1[1][^1].ToString();
            t2[t1[1].IndexOf(".")] = ".";
            foreach (string s in t2)
            {
                ret += s;
            }
            return ret;
        }
        catch
        {
            return mailAddr;
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
