using Discord;
using Flurl.Util;
using KanonBot.API;
using KanonBot.Drivers;
using static KanonBot.Functions.Accounts;

namespace KanonBot.Functions.OSUBot
{
    public class ChatBot
    {
        async public static Task Execute(Target target, string cmd)
        {
            if (target.platform != Platform.OneBot) return;

            // 通过IAM验证账户
            var accInfo = Accounts.GetAccInfo(target);
            string provider;
            try { provider = API.IAM.Client.PlatformToProvider(accInfo.platform); }
            catch (NotSupportedException) { await target.reply("当前平台暂不支持此功能。"); return; }

            var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
            if (iamUserId == null)
            {
                await target.reply("你还没有绑定 desu.life 账户，请先在 https://iam.neonprizma.com 注册并使用 !reg 验证码 进行绑定。");
                return;
            }

            bool chatbot_premission = false, custom_chatbot_premission = false;

            // TODO: 权限检查暂时移除，待IAM添加角色查询接口后恢复
            // 目前所有绑定用户均可使用chatbot
            chatbot_premission = true;
            custom_chatbot_premission = true;

            switch (target.raw)
            {
                case OneBot.Models.GroupMessage g:
                    if (g.GroupId == 217451241) //猫群，非猫群需要判断是否拥有chatbot权限
                        chatbot_premission = true;
                    break;
                default:
                    break;
            }

            //判断子命令
            if (custom_chatbot_premission)
            {
                string rootCmd, childCmd = "";
                try
                {
                    var tmp = cmd.Split(' ', 2, StringSplitOptions.TrimEntries);;
                    rootCmd = tmp[0];
                    childCmd = tmp[1];
                }
                catch { rootCmd = cmd; }

                switch (rootCmd.ToLower())
                {
                    case "set":
                        try
                        {
                            string botdefine = "", openaikey = "", organization = "";
                            if (childCmd.Contains(';'))
                            {
                                try
                                {
                                    var t = childCmd.Split(';');
                                    foreach (var item in t)
                                    {
                                        if (item.StartsWith("define"))
                                            botdefine = item[(item.IndexOf("=") + 1)..];
                                        if (item.StartsWith("openaikey"))
                                            openaikey = item[(item.IndexOf("=") + 1)..];
                                        if (item.StartsWith("organization"))
                                            organization = item[(item.IndexOf("=") + 1)..];
                                    }
                                }
                                catch
                                {
                                    await target.reply("失败了喵...");
                                }
                            }
                            else
                            {
                                if (childCmd.StartsWith("define"))
                                    botdefine = childCmd[(childCmd.IndexOf("=") + 1)..];
                                if (childCmd.StartsWith("openaikey"))
                                    openaikey = childCmd[(childCmd.IndexOf("=") + 1)..];
                                if (childCmd.StartsWith("organization"))
                                    organization = childCmd[(childCmd.IndexOf("=") + 1)..];
                            }

                            if (botdefine == "")
                            {
                                await target.reply("请使用以下格式上传喵，但是请注意，如果需要上传openai密钥的话，请不要在群内公开你的openai密钥哦\n" +
                                "!cat set define=####;openaikey=####\n" +
                                "(*后两项可省略，如果想删除某些参数的话，使用default就可以啦！比如：define=default;openaikey=default)");
                                return;
                            }

                            if (await Database.Client.UpdateChatBotInfo(iamUserId, botdefine, openaikey, organization))
                                await target.reply("成功了喵！");
                            else
                                await target.reply("失败了喵...");
                        }
                        catch
                        {
                            await target.reply("请使用以下格式上传喵，但是请注意，如果需要上传openai密钥的话，请不要在群内公开你的openai密钥哦\n" +
                                //"!cat set define=####;openaikey=####;organization=####\n" +
                                "!cat set define=####;openaikey=####\n" +
                                "(*后两项可省略，如果想删除某些参数的话，使用default就可以啦！比如：define=default;openaikey=default)");
                        }
                        return;
                    default:
                        break;
                }
            }


            try
            {
                if (chatbot_premission)
                    await target.reply(await OpenAI.Chat(cmd, target.sender!, iamUserId));
                else
                    await target.reply("你没有使用chatbot的权限呢T^T");
            }
            catch
            {
                await target.reply("目前无法访问API T^T");
            }
        }

    }
}
