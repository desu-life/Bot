using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.Serializer;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace KanonBot.Functions
{
    public class PpvsCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "ppvs",
                Description = "Compare osu! performance plus stats",
                Args =
                [
                    new() { Name = "user1", Description = "First user to compare", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple },
                    new() { Name = "user2", Description = "Second user to compare", Prefix = ArgPrefix.Hash, Strategy = ParseStrategy.Simple },
                ],
                Flags =  [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var user1Str = cmd.GetString("user1");
            var user2Str = cmd.GetString("user2");

            if (string.IsNullOrWhiteSpace(user1Str) && string.IsNullOrWhiteSpace(user2Str))
            {
                await target.Treply("osu.ppvs_usage_self");
                return;
            }

            // 获取自身 osu 信息（在需要时复用）
            async Task<API.OSU.Models.User?> GetSelfUser()
            {
                var accInfo = Accounts.GetAccInfo(target);
                string provider;
                try
                {
                    provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
                }
                catch (NotSupportedException)
                {
                    await target.Treply("account.platform_unsupported");
                    return null;
                }

                var iamUserId = await API.IAM
                    .Client
                    .GetIamUserIdByExternalId(provider, accInfo.uid);
                if (iamUserId == null)
                {
                    await target.Treply("account.not_bound");
                    return null;
                }

                var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
                if (bindings == null)
                {
                    await target.Treply("account.fetch_failed");
                    return null;
                }

                var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
                if (!osuUid.HasValue)
                {
                    await target.Treply("account.osu_not_bound");
                    return null;
                }

                var userSelf = await API.OSU.Client.GetUser(osuUid.Value);
                if (userSelf == null)
                {
                    await target.Treply("account.banned");
                    return null;
                }

                return userSelf;
            }

            API.OSU.Models.User? leftUser;  // 画在左侧 (u2)
            API.OSU.Models.User? rightUser; // 画在右侧 (u1)

            if (string.IsNullOrWhiteSpace(user2Str))
            {
                // 只指定了一个用户：自己 vs 该用户
                var selfUser = await GetSelfUser();
                if (selfUser == null) return;

                rightUser = await API.OSU.Client.GetUser(user1Str!);
                if (rightUser == null)
                {
                    await target.Treply("error.user_not_found");
                    return;
                }

                leftUser = selfUser;
            }
            else
            {
                // 指定了两个用户（user1 可能为空，表示只写了 #user2）
                if (string.IsNullOrWhiteSpace(user1Str))
                {
                    var selfUser = await GetSelfUser();
                    if (selfUser == null) return;
                    leftUser = selfUser;
                }
                else
                {
                    leftUser = await API.OSU.Client.GetUser(user1Str);
                    if (leftUser == null)
                    {
                        await target.Treply("osu.ppvs_user_not_found", user1Str);
                        return;
                    }
                }

                rightUser = await API.OSU.Client.GetUser(user2Str);
                if (rightUser == null)
                {
                    await target.Treply("osu.ppvs_user_not_found", user2Str);
                    return;
                }
            }

            await target.Treply("osu.ppvs_loading");

            Image.PPVS.PPVSPanelData data = new();

            var d1 = await API.OSU.Client.PPlus.GetUserPlusDataNext(leftUser.Id);
            if (d1 == null)
            {
                await target.Treply("osu.ppvs_error");
                return;
            }
            data.u2Name = leftUser.Username;
            data.u2 = d1.Performances;

            var d2 = await API.OSU.Client.PPlus.GetUserPlusDataNext(rightUser.Id);
            if (d2 == null)
            {
                await target.Treply("osu.ppvs_error");
                return;
            }
            data.u1Name = rightUser.Username;
            data.u1 = d2.Performances;

            using var img = await Image.PPVS.DrawPPVS(data);
            await target.reply(img, new JpegEncoder());
        }
    }
}
