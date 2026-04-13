using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;

namespace KanonBot.Functions.OSUBot
{
    public class Bind
    {
          public static async Task Execute(Target target, string cmd)
        {
            var input = cmd.Trim();
            var accInfo = Accounts.GetAccInfo(target);
            if (accInfo.platform == Platform.Unknown)
            {
                await target.reply("无法获取您的平台信息。");
                return;
            }

            string provider;
            try
            {
                provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
            }
            catch (NotSupportedException)
            {
                await target.reply("当前平台暂不支持绑定流程。");
                return;
            }

            if (provider is not ("qq" or "discord"))
            {
                await target.reply("当前平台暂不支持绑定流程。");
                return;
            }

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (provider != "qq")
                {
                    return;
                }

                var code = input.Split(' ', 2, StringSplitOptions.TrimEntries)[0];
                if (code.Length != 6)
                {
                    await target.reply("验证码格式不正确，请输入网页显示的验证码。\n用法: !bind 验证码");
                    return;
                }

                var verifyResult = await API.IAM.Client.SubmitQqCode(code, accInfo.uid);
                switch (verifyResult)
                {
                    case API.IAM.VerifyResult.Success:
                        await target.reply("验证码已提交，请回到网页检查你的绑定状态哦。");
                        return;
                    case API.IAM.VerifyResult.InvalidCode:
                        await target.reply("验证码无效或已过期，请重新使用 !bind 生成新的绑定链接并完成网页流程。");
                        return;
                    case API.IAM.VerifyResult.AlreadyBound:
                        await target.reply("你的 QQ 账户已经绑定过 desu.life 账户了。若需更换绑定，请联系管理员。");
                        return;
                    case API.IAM.VerifyResult.InvalidApiKey:
                        Log.Error("IAM API Key is invalid for QQ submit-code");
                        await target.reply("服务配置错误，请联系管理员。");
                        return;
                    case API.IAM.VerifyResult.Misconfigured:
                        Log.Error("IAM integration is misconfigured for QQ submit-code");
                        await target.reply("服务配置错误，请联系管理员。");
                        return;
                    default:
                        await target.reply("提交验证码失败，请稍后再试。");
                        return;
                }
            }

            var sessionResult = await API.IAM.Client.StartBindSession(provider, accInfo.uid);
            switch (sessionResult.Type)
            {
                case API.IAM.BindSessionResultType.Success:
                    if (sessionResult.Session == null || string.IsNullOrWhiteSpace(sessionResult.Session.Url))
                    {
                        await target.reply("生成绑定链接失败，请稍后再试。");
                        return;
                    }

                 
                    await target.reply(
                        $"请点击一次性链接完成绑定：\n{sessionResult.Session.Url}\n进入页面后直接使用 osu! 登录，完成绑定流程\n绑定的验证码可以通过 !bind 验证码 提交。"
                    );
                    return;
                case API.IAM.BindSessionResultType.AlreadyBound:
                    await target.reply("你的平台账户已经绑定过 desu.life 账户了。若需更换绑定，请联系管理员。");
                    return;
                case API.IAM.BindSessionResultType.InvalidApiKey:
                    Log.Error("IAM API Key is invalid for provider {Provider} bind-session", provider);
                    await target.reply("服务配置错误，请联系管理员。");
                    return;
                case API.IAM.BindSessionResultType.Misconfigured:
                    Log.Error("IAM integration is misconfigured for provider {Provider} bind-session", provider);
                    await target.reply("服务配置错误，请联系管理员。");
                    return;
                default:
                    await target.reply("生成绑定链接失败，请稍后再试。");
                    return;
            }
        }
    }
}
