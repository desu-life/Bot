#pragma warning disable IDE0044 // 添加只读修饰符
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8604 // 解引用可能出现空引用。

using System.Net;
using System.Security.Authentication;
using MailKit;
using MailKit.Net.Smtp;
using MimeKit;

namespace KanonBot;
public static class Mail
{
    private static Config.Base config = Config.inner!;
    public class MailStruct
    {
        public required IEnumerable<String> MailTo; //收件人，可添加多个
        public IEnumerable<String> MailCC = []; //抄送人，不建议添加
        public required string Subject; //标题
        public required string Body; //正文
        public required bool IsBodyHtml;
    }
    public static async Task Send(MailStruct ms)
    {
        MimeKit.MimeMessage message = new();
        ms.MailTo.Iter(s => message.To.Add(new MailboxAddress(s, s))); //设置收件人
        if (message.To.Count == 0) return;
        message.From.Add(new MailboxAddress(config.mail.username, config.mail.username));

        message.Subject = ms.Subject;
        if (ms.IsBodyHtml) {
            message.Body = new TextPart("html") { Text = ms.Body };
        } else {
            message.Body = new TextPart("plain") { Text = ms.Body };
        }
        // var client = new SmtpClient(config.mail.smtpHost, config.mail.smtpPort)
        // {
        //     Credentials = new System.Net.NetworkCredential(config.mail.username, config.mail.password), //设置邮箱用户名与密码
        //     EnableSsl = true //启用SSL
        // }; //设置邮件服务器
        // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        // client.Send(message); //发送

        using var client = new SmtpClient();
        client.Connect(config.mail.smtpHost, config.mail.smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        client.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13; // 只允许 TLS 1.2 和 1.3
        client.Authenticate(config.mail.username, config.mail.password);
        await client.SendAsync(message);
        client.Disconnect(true);
    }
}
