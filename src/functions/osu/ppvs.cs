using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.Serializer;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace KanonBot.Functions.OSUBot
{
    public class PpvsCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "ppvs",
                Args =
                [
                    new() { Name = "users_raw", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                ],
                Flags =  [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var rawInput = cmd.RawArgs;
            var cmds = rawInput.Split('#');
            if (cmds.Length == 1)
            {
                if (cmds[0].Length == 0)
                {
                    await target.Treply("osu.ppvs_usage_self");
                    return;
                }

                // 通过IAM获取自身osu uid
                var accInfo = Accounts.GetAccInfo(target);
                string provider;
                try
                {
                    provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
                }
                catch (NotSupportedException)
                {
                    await target.Treply("account.platform_unsupported");
                    return;
                }

                var iamUserId = await API.IAM
                    .Client
                    .GetIamUserIdByExternalId(provider, accInfo.uid);
                if (iamUserId == null)
                {
                    await target.Treply("account.not_bound");
                    return;
                }

                var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
                if (bindings == null)
                {
                    await target.Treply("account.fetch_failed");
                    return;
                }

                var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
                if (!osuUid.HasValue)
                {
                    await target.Treply("account.osu_not_bound");
                    return;
                }

                // 分别获取两位的信息
                var userSelf = await API.OSU.Client.GetUser(osuUid.Value);
                if (userSelf == null)
                {
                    await target.Treply("account.banned");
                    return;
                }

                var user2 = await API.OSU.Client.GetUser(cmds[0]);
                if (user2 == null)
                {
                    await target.Treply("error.user_not_found");
                    return;
                }

                await target.Treply("osu.ppvs_loading");

                Image.PPVS.PPVSPanelData data = new();

                var d1 = await API.OSU.Client.PPlus.GetUserPlusDataNext(userSelf.Id);
                if (d1 == null)
                {
                    await target.Treply("osu.ppvs_error");
                    return;
                }
                data.u2Name = userSelf.Username;
                data.u2 = d1.Performances;

                var d2 = await API.OSU.Client.PPlus.GetUserPlusDataNext(user2.Id);
                if (d2 == null)
                {
                    await target.Treply("osu.ppvs_error");
                    return;
                }
                data.u1Name = user2.Username;
                data.u1 = d2.Performances;

                using var img = await Image.PPVS.DrawPPVS(data);
                await target.reply(img, new JpegEncoder());
            }
            else if (cmds.Length == 2)
            {
                if (cmds[0].Length == 0 || cmds[1].Length == 0)
                {
                    await target.Treply("osu.ppvs_usage_dual");
                    return;
                }

                // 分别获取两位的信息
                var user1 = await API.OSU.Client.GetUser(cmds[0]);
                if (user1 == null)
                {
                    await target.Treply("osu.ppvs_user_not_found", cmds[0]);
                    return;
                }

                var user2 = await API.OSU.Client.GetUser(cmds[1]);
                if (user2 == null)
                {
                    await target.Treply("osu.ppvs_user_not_found", cmds[1]);
                    return;
                }

                await target.Treply("osu.ppvs_loading");

                Image.PPVS.PPVSPanelData data = new();

                var d1 = await API.OSU.Client.PPlus.GetUserPlusDataNext(user1.Id);
                if (d1 == null)
                {
                    await target.Treply("osu.ppvs_error");
                    return;
                }
                data.u2Name = user1.Username;
                data.u2 = d1.Performances;

                var d2 = await API.OSU.Client.PPlus.GetUserPlusDataNext(user2.Id);
                if (d2 == null)
                {
                    await target.Treply("osu.ppvs_error");
                    return;
                }
                data.u1Name = user2.Username;
                data.u1 = d2.Performances;

                using var img = await Image.PPVS.DrawPPVS(data);
                await target.reply(img, new JpegEncoder());
            }
            else
            {
                await target.Treply("osu.ppvs_usage_full");
            }
        }
    }
}
