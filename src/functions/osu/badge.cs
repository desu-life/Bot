using System.IO;
using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using Flurl.Http;
using KanonBot.Drivers;
using KanonBot.Message;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using static KanonBot.Functions.Accounts;
using Img = SixLabors.ImageSharp.Image;

namespace KanonBot.Functions.OSUBot
{
    public class Badge
    {
        public static async Task ExecuteInfo(Drivers.Target target, string cmd)
        {
            var userCtx = await Accounts.ResolveIamUser(target);
            if (userCtx == null)
            {
                await target.Treply("badge.not_bound");
                return;
            }
            await Info(target, cmd, userCtx);
        }

        public static async Task ExecuteList(Drivers.Target target)
        {
            var userCtx = await Accounts.ResolveIamUser(target);
            if (userCtx == null)
            {
                await target.Treply("badge.not_bound");
                return;
            }
            await List(target, userCtx);
        }

        private static async Task Info(
            Drivers.Target target,
            string cmd,
            Accounts.UserContext userCtx
        )
        {
            if (!int.TryParse(cmd, out int badgeNum) || badgeNum < 1)
            {
                await target.Treply("badge.invalid_id");
                return;
            }

            var profile = await API.Kagami.Client.GetPublicKanonBotProfile(userCtx.IamUserId);
            if (profile == null || profile.InstalledBadges.Count == 0)
            {
                await target.Treply("badge.no_badges");
                return;
            }

            if (badgeNum > profile.InstalledBadges.Count)
            {
                await target.Treply("badge.out_of_range", profile.InstalledBadges.Count, badgeNum);
                return;
            }

            var badge = profile.InstalledBadges[badgeNum - 1];

            var rtmsg = new Chain();

            // Try to load badge image from URL
            if (!string.IsNullOrEmpty(badge.ImageUrl))
            {
                try
                {
                    using var imgStream = await badge.ImageUrl.GetStreamAsync();
                    using var badgeImg = await Img.LoadAsync<Rgba32>(imgStream);
                    await target.reply(badgeImg, new JpegEncoder());
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to load badge image from {Url}", badge.ImageUrl);
                }
            }

            var infoText =
                $"徽章信息如下：\n"
                + $"名称：{badge.NameEn}\n"
                + $"中文名称: {badge.NameZh}\n"
                + $"描述: {badge.Summary}";

            if (badge.ExpiresAt.HasValue)
                infoText += $"\n过期时间: {badge.ExpiresAt.Value:yyyy-MM-dd}";

            rtmsg.msg(infoText);
            await target.reply(rtmsg);
        }

        private static async Task List(Drivers.Target target, Accounts.UserContext userCtx)
        {
            var profile = await API.Kagami.Client.GetPublicKanonBotProfile(userCtx.IamUserId);
            if (profile == null || profile.InstalledBadges.Count == 0)
            {
                await target.Treply("badge.no_badges_with_link");
                return;
            }

            var text = "你当前佩戴的徽章：\n";
            for (int i = 0; i < profile.InstalledBadges.Count; i++)
            {
                var b = profile.InstalledBadges[i];
                text += $"{i + 1}. {b.NameZh}（{b.NameEn}）";
                if (b.ExpiresAt.HasValue)
                    text += $" [过期: {b.ExpiresAt.Value:yyyy-MM-dd}]";
                text += "\n";
            }
            text += $"\n徽章上限: {profile.BadgeLimit}\n管理徽章请前往 https://hub.kagamistudio.com";

            await target.reply(text);
        }
    }
}
