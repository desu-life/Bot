#pragma warning disable CS8602 // 解引用可能出现空引用。
using Flurl.Util;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using LanguageExt.SomeHelp;
using LanguageExt.UnsafeValueAccess;
using System.Net;

namespace KanonBot.Functions
{
    public static class Accounts
    {
        public struct AccInfo
        {
            public required Platform platform;
            public required string uid;
        }
        public static async Task RegAccount(Target target, string cmd)
        {
            //var is_private_msg = target.raw switch
            //{
            //    OneBot.Models.GroupMessage => false,
            //    OneBot.Models.PrivateMessage => true,
            //    _ => true,//无法调试qq频道私聊 暂时默认私聊
            //};
            //if (!is_private_msg)
            //{
            //    await target.reply("请私聊bot以使用本功能。");
            //    return;
            //}

            var mailAddr = cmd.Trim(); // reg 邮箱地址
            var verifyCode = Utils.RandomStr(22, true); //生成验证码

            if (!Utils.IsMailAddr(mailAddr))
            {
                await target.reply("请输入有效的电子邮件地址。");
                return;
            }
            string uid = "-1";
            bool is_regd = await Database.Client.IsRegd(mailAddr);
            bool is_append = false;
            Database.Model.User dbuser = new();

            if (is_regd) dbuser = (await Database.Client.GetUser(mailAddr))!;
            switch (target.platform) //获取用户ID及平台信息 平台： qq qguild khl discord 四个
            {
                case Platform.Guild:
                    if (target.raw is Guild.Models.MessageData g)
                    {
                        uid = g.Author.ID;
                        if (is_regd)
                            if (dbuser.qq_guild_uid == g.Author.ID) { await target.reply("您提供的邮箱已经与您目前的平台绑定了。"); return; }
                        var g1 = await Database.Client.GetUsersByUID(uid, Platform.Guild);
                        if (g1 != null)
                        {
                            if (g1.email != null)
                            {
                                is_append = true;
                            }
                            else
                            {
                                await target.reply(new Chain()
                                    .msg($"您目前的平台账户已经和邮箱为" +
                                    $"{Utils.HideMailAddr(g1.email ?? "undefined@undefined.undefined")}" +
                                    $"的用户绑定了。"));
                                return;
                            }
                        }
                    }
                    break;
                case Platform.OneBot:
                    if (target.raw is OneBot.Models.CQMessageEventBase o)
                    {
                        uid = o.UserId.ToString();
                        if (is_regd)
                            if (dbuser.qq_id == o.UserId) { await target.reply("您提供的邮箱已经与您目前的平台绑定了。"); return; }
                        var o1 = await Database.Client.GetUsersByUID(uid, Platform.OneBot);
                        if (o1 != null)
                        {
                            if (o1.email != null)
                            {
                                is_append = true;
                            }
                            else
                            {
                                await target.reply(new Chain()
                                    .msg($"您目前的平台账户已经和邮箱为" +
                                    $"{Utils.HideMailAddr(o1.email ?? "undefined@undefined.undefined")}" +
                                    $"的用户绑定了。"));
                                return;
                            }
                        }
                    }
                    break;
                case Platform.KOOK:
                    if (target.raw is Kook.WebSocket.SocketMessage k)
                    {
                        uid = k.Author.Id.ToString();
                        if (is_regd)
                            if (dbuser.kook_uid == uid) { await target.reply("您提供的邮箱已经与您目前的平台绑定了。"); return; }
                        var k1 = await Database.Client.GetUsersByUID(uid, Platform.KOOK);
                        if (k1 != null)
                        {
                            if (k1.email != null)
                            {
                                is_append = true;
                            }
                            else
                            {
                                await target.reply(new Chain()
                                    .msg($"您目前的平台账户已经和邮箱为" +
                                    $"{Utils.HideMailAddr(k1.email ?? "undefined@undefined.undefined")}" +
                                    $"的用户绑定了。"));
                                return;
                            }
                        }
                    }
                    break;
                case Platform.Discord:
                    if (target.raw is Discord.WebSocket.SocketMessage d)
                    {
                        uid = d.Author.Id.ToString();
                        if (is_regd)
                            if (dbuser.discord_uid == uid) { await target.reply("您提供的邮箱已经与您目前的平台绑定了。"); return; }
                        var k1 = await Database.Client.GetUsersByUID(uid, Platform.Discord);
                        if (k1 != null)
                        {
                            if (k1.email != null)
                            {
                                is_append = true;
                            }
                            else
                            {
                                await target.reply(new Chain()
                                    .msg($"您目前的平台账户已经和邮箱为" +
                                    $"{Utils.HideMailAddr(k1.email ?? "undefined@undefined.undefined")}" +
                                    $"的用户绑定了。"));
                                return;
                            }
                        }
                    }
                    break;
                default: break;
            }
            var platform = target.platform! switch
            {
                Platform.Guild => "qguild",
                Platform.KOOK => "kook",
                Platform.OneBot => "qq",
                Platform.Discord => "discord",
                _ => throw new NotSupportedException()
            };



            string read_html = System.IO.File.ReadAllText("./mail_desu_life_mailaddr_verify_template.txt");
           
            if (is_regd) //检查此邮箱是否已存在于数据库中
            {
                // 如果存在，执行绑定
                read_html = read_html.Replace("{{{{mailaddress}}}}", mailAddr).Replace("{{{{veritylink}}}}", $"https://desu.life/verify-email?mailAddr={mailAddr}&verifyCode={verifyCode}&platform={platform}&uid={uid}&op=2");
                Mail.MailStruct ms = new()
                {
                    MailTo = new string[] { mailAddr },
                    Subject = "[来自desu.life自动发送的邮件]请验证您的邮箱",
                    Body = read_html,
                    IsBodyHtml = true
                };
                try
                {
                    Mail.Send(ms);
                    await target.reply("绑定验证邮件发送成功，请继续从邮箱内操作，注意检查垃圾箱。");
                    await Database.Client.SetVerifyMail(mailAddr, verifyCode); //设置临时验证码
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error: " + ex.ToString());
                    await target.reply("发送验证邮件失败，请联系管理员。");
                }
            }
            else if (!is_append)
            {
                //如果不存在，新建
                read_html = read_html.Replace("{{{{mailaddress}}}}", mailAddr).Replace("{{{{veritylink}}}}", $"https://desu.life/verify-email?mailAddr={mailAddr}&verifyCode={verifyCode}&platform={platform}&uid={uid}&op=1");
                Mail.MailStruct ms = new()
                {
                    MailTo = new string[] { mailAddr },
                    Subject = "[来自desu.life自动发送的邮件]请验证您的邮箱",
                    Body = read_html,
                    IsBodyHtml = true
                };
                try
                {
                    Mail.Send(ms);
                    await target.reply("注册验证邮件发送成功，请继续从邮箱内操作，注意检查垃圾箱。");
                    await Database.Client.SetVerifyMail(mailAddr, verifyCode); //设置临时验证码
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error: " + ex.ToString());
                    await target.reply("发送验证邮件失败，请联系管理员。");
                }
            }
            else
            {
                //追加邮箱信息
                read_html = read_html.Replace("{{{{mailaddress}}}}", mailAddr).Replace("{{{{veritylink}}}}", $"https://desu.life/verify-email?mailAddr={mailAddr}&verifyCode={verifyCode}&platform={platform}&uid={uid}&op=3");
                Mail.MailStruct ms = new()
                {
                    MailTo = new string[] { mailAddr },
                    Subject = "[来自desu.life自动发送的邮件]请验证您的邮箱",
                    Body = read_html,
                    IsBodyHtml = true
                };
                try
                {
                    Mail.Send(ms);
                    await target.reply("电子邮箱追加验证邮件发送成功，请继续从邮箱内操作，注意检查垃圾箱。");
                    await Database.Client.SetVerifyMail(mailAddr, verifyCode); //设置临时验证码
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error: " + ex.ToString());
                    await target.reply("发送验证邮件失败，请联系管理员。");
                }
            }
        }

        async public static Task BindService(Target target, string cmd)
        {
            cmd = cmd.Trim();
            string childCmd_1 = "", childCmd_2 = "";
            try
            {
                var tmp = cmd.SplitOnFirstOccurence(" ");
                childCmd_1 = tmp[0];
                childCmd_2 = tmp[1];
            }
            catch { }

            var AccInfo = Accounts.GetAccInfo(target);
            var DBUser = await Accounts.GetAccount(AccInfo.uid, AccInfo.platform);
            //这里dbuser可空，后面一定要检测


            if (childCmd_1 == "osu")
            {
                // 先检查查询的用户是否有效
                API.OSU.Models.User? online_osu_userinfo;
                online_osu_userinfo = await API.OSU.Client.GetUser(childCmd_2);
                if (online_osu_userinfo == null) { await target.reply($"没有找到osu用户名为 {childCmd_2} 的osu用户，绑定失败。"); return; }

                // 检查要绑定的osu是否没有被Kanon用户绑定过
                var db_osu_userinfo = await Database.Client.GetOsuUser(online_osu_userinfo.Id);
                if (db_osu_userinfo != null)
                {
                    if (DBUser != null && DBUser.uid == db_osu_userinfo.uid) {
                        await target.reply($"你已绑定该账户。"); return;
                    }
                    await target.reply($"此osu账户已被用户ID为 {db_osu_userinfo.uid} 的用户绑定了，如果这是你的账户，请联系管理员更新账户信息。"); return;
                }

                // 查询当前kanon账户是否有效
                if (DBUser == null) { await target.reply("你还没有绑定desu.life账户，使用 !reg 你的邮箱 来进行绑定或注册喵。"); return; }

                // 检查用户是否已绑定osu账户
                var osuuserinfo = await Database.Client.GetOsuUserByUID(DBUser.uid);
                if (osuuserinfo != null) { await target.reply($"您已经与osu uid为 {osuuserinfo.osu_uid} 的用户绑定过了。"); return; }

                // 通过osu username搜索osu用户id
                try
                {
                    // 没被他人绑定，开始绑定流程
                    if (await Database.Client.InsertOsuUser(DBUser.uid, online_osu_userinfo.Id))
                    {
                        await target.reply($"绑定成功，已将osu用户 {online_osu_userinfo.Id} 绑定至Kanon账户 {DBUser.uid} 。");
                        await GeneralUpdate.UpdateUser(online_osu_userinfo.Id, true); //插入用户每日数据记录
                    }
                    else { await target.reply($"绑定失败，请稍后再试。"); }
                }
                catch { await target.reply($"在绑定用户时出错，请联系猫妈处理.png"); return; }
            }
            else
            {
                await target.reply("请按照以下格式进行绑定。\n!bind osu 您的osu用户名 "); return;
            }
        }
        public static async Task<(Option<API.OSU.Models.UserExtended>, Option<Database.Model.User>)> ParseAt(string atmsg) {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return (None, None);

            var (k, v) = res.Value();
            if (k == "osu") {
                var uid = parseLong(v).IfNone(() => 0);
                if (uid == 0)
                    return (None, None);
                
                var osuacc_ = await API.OSU.Client.GetUser(uid);
                if (osuacc_ is null)
                    return (None, None);

                var dbuser_ = await GetAccountByOsuUid(uid);
                if (dbuser_ is null)
                    return (Some(osuacc_!), None);
                else
                    return (Some(osuacc_!), Some(dbuser_!));
            }

            var platform =  k switch {
                "qq" => Platform.OneBot,
                "gulid" => Platform.Guild,
                "discord" => Platform.Discord,
                "kook" => Platform.KOOK,
                _ => Platform.Unknown
            };
            if (platform == Platform.Unknown)
                return (None, None);
            
            var dbuser = await GetAccount(v, platform);
            if (dbuser is null)
                return (None, None);

            var dbosu = await CheckOsuAccount(dbuser.uid);
            if (dbosu is null)
                return (None, Some(dbuser!));

            var osuacc = await API.OSU.Client.GetUser(dbosu.osu_uid);
            if (osuacc is null)
                return (None, Some(dbuser!));
            else
                return (Some(osuacc!), Some(dbuser!));
        }

        public static async Task<Database.Model.User?> GetAccount(string uid, Platform platform)
        {
            return await Database.Client.GetUsersByUID(uid, platform);
        }
        public static async Task<Database.Model.User?> GetAccountByOsuUid(long osu_uid)
        {
            return await Database.Client.GetUserByOsuUID(osu_uid);
        }
        public static async Task<Database.Model.UserOSU?> CheckOsuAccount(long uid)
        {
            return await Database.Client.GetOsuUserByUID(uid);
        }

        public static AccInfo GetAccInfo(Target target)
        {
            switch (target.platform)
            {
                case Platform.Guild:
                    if (target.raw is Guild.Models.MessageData g)
                    {
                        return new AccInfo() { platform = Platform.Guild, uid = g.Author.ID };
                    }
                    break;
                case Platform.OneBot:
                    if (target.raw is OneBot.Models.CQMessageEventBase o)
                    {
                        return new AccInfo() { platform = Platform.OneBot, uid = o.UserId.ToString() };
                    }
                    break;
                case Platform.KOOK:
                    if (target.raw is Kook.WebSocket.SocketMessage k)
                    {
                        return new AccInfo() { platform = Platform.KOOK, uid = k.Author.Id.ToString() };
                    }
                    break;
                case Platform.Discord:
                    if (target.raw is Discord.WebSocket.SocketMessage d)
                    {
                        return new AccInfo() { platform = Platform.Discord, uid = d.Author.Id.ToString() };
                    }
                    break;
            }
            return new() { platform = Platform.Unknown, uid = "" };
        }
    }
}
