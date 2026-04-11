using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;
using KanonBot.Serializer;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;

namespace KanonBot.Functions.OSUBot
{
    public class PPvs
    {
        public async static Task Execute(Target target, string cmd)
        {
            var cmds = cmd.Split('#');
            if (cmds.Length == 1) {
                if (cmds[0].Length == 0)
                {
                    await target.reply("!ppvs 要对比的用户");
                    return;
                }


                // 通过IAM获取自身osu uid
                var accInfo = Accounts.GetAccInfo(target);
                string provider;
                try { provider = API.IAM.Client.PlatformToProvider(accInfo.platform); }
                catch (NotSupportedException) { await target.reply("当前平台暂不支持此功能。"); return; }

                var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
                if (iamUserId == null)
                { await target.reply("你还没有绑定 desu.life 账户，请先在 https://iam.neonprizma.com 注册并使用 !reg 验证码 进行绑定。"); return; }

                var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
                if (bindings == null)
                { await target.reply("获取账户信息失败，请稍后再试。"); return; }

                var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
                if (!osuUid.HasValue)
                { await target.reply("你还没有绑定 osu! 账户，请前往 https://iam.neonprizma.com 绑定。"); return; }

                // 分别获取两位的信息
                var userSelf = await API.OSU.Client.GetUser(osuUid.Value);
                if (userSelf == null)
                {
                    await target.reply("被办了。");
                    return;
                }

                var user2 = await API.OSU.Client.GetUser(cmds[0]);
                if (user2 == null)
                {
                    await target.reply("猫猫没有找到此用户。");
                    return;
                }

                await target.reply("正在获取pp+数据，请稍等。。");

                Image.PPVS.PPVSPanelData data = new();

                // var d1 = await Database.Client.GetOsuPPlusDataNext(userSelf.Id);
                // if (d1 == null)
                // {
                //     var d1temp = await API.OSU.Client.GetUserPlusDataNext(userSelf.Id);
                //     if (d1temp == null)
                //     {
                //         await target.reply("获取pp+数据时出错，等会儿再试试吧");
                //         return;
                //     }
                //     d1 = d1temp;
                //     await Database.Client.UpdateOsuPPlusDataNext(d1);
                // }
                // data.u2Name = userSelf.Username;
                // data.u2 = d1.Performances;

                // var d2 = await Database.Client.GetOsuPPlusDataNext(user2.Id);
                // if (d2 == null)
                // {
                //     var d2temp = await API.OSU.Client.GetUserPlusDataNext(user2.Id);
                //     if (d2temp == null)
                //     {
                //         await target.reply("获取pp+数据时出错，等会儿再试试吧");
                //         return;
                //     }
                //     d2 = d2temp;
                //     await Database.Client.UpdateOsuPPlusDataNext(d2);
                // }
                // data.u1Name = user2.Username;
                // data.u1 = d2.Performances;

                var d1 = await API.OSU.Client.PPlus.GetUserPlusDataNext(userSelf.Id);
                if (d1 == null)
                {
                    await target.reply("获取pp+数据时出错，等会儿再试试吧");
                    return;
                }
                data.u2Name = userSelf.Username;
                data.u2 = d1.Performances;

                var d2 = await API.OSU.Client.PPlus.GetUserPlusDataNext(user2.Id);
                if (d2 == null)
                {
                    await target.reply("获取pp+数据时出错，等会儿再试试吧");
                    return;
                }
                data.u1Name = user2.Username;
                data.u1 = d2.Performances;

                using var stream = new MemoryStream();
                using var img = await Image.PPVS.DrawPPVS(data);
                await img.SaveAsync(stream, new JpegEncoder());
                await target.reply(new Chain().image(Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length), ImageSegment.Type.Base64));
            } else if (cmds.Length == 2) {
                if (cmds[0].Length == 0 || cmds[1].Length == 0)
                {
                    await target.reply("!ppvs 用户1#用户2");
                    return;
                }

                // 分别获取两位的信息
                var user1 = await API.OSU.Client.GetUser(cmds[0]);
                if (user1 == null)
                {
                    await target.reply($"猫猫没有找到叫 {cmds[0]} 用户。");
                    return;
                }

                var user2 = await API.OSU.Client.GetUser(cmds[1]);
                if (user2 == null)
                {
                    await target.reply($"猫猫没有找到叫 {cmds[1]} 用户。");
                    return;
                }

                await target.reply("正在获取pp+数据，请稍等。。");

                Image.PPVS.PPVSPanelData data = new();

                var d1 = await API.OSU.Client.PPlus.GetUserPlusDataNext(user1.Id);
                if (d1 == null)
                {
                    await target.reply("获取pp+数据时出错，等会儿再试试吧");
                    return;
                }
                data.u2Name = user1.Username;
                data.u2 = d1.Performances;

                var d2 = await API.OSU.Client.PPlus.GetUserPlusDataNext(user2.Id);
                if (d2 == null)
                {
                    await target.reply("获取pp+数据时出错，等会儿再试试吧");
                    return;
                }
                data.u1Name = user2.Username;
                data.u1 = d2.Performances;


                using var stream = new MemoryStream();
                using var img = await Image.PPVS.DrawPPVS(data);
                await img.SaveAsync(stream, new JpegEncoder());
                await target.reply(new Chain().image(Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length), ImageSegment.Type.Base64));
            } else {
                await target.reply("!ppvs 用户1#用户2/!ppvs 要对比的用户");
            }
        }
    }
}
