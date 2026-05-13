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
    public class BadgeHelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply("!badge info/list\n徽章设置/兑换等操作请前往 https://hub.kagamistudio.com");
    }

    public class BadgeInfoCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge info",
                Args =
                [
                    new() { Name = "badge_number", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple, Parse = s => CommandDefs.ParseInt(s) }
                ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            Badge.ExecuteInfo(target, cmd.RawArgs);
    }

    public class BadgeListCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge list",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Badge.ExecuteList(target);
    }

    public class BadgeDeprecatedCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge set",
                Aliases =  [ "badge redeem", "badge sudo" ],
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply("徽章管理已迁移至网页端，请前往 https://hub.kagamistudio.com 进行操作。");
    }

    public class Badge
    {
        public static async Task ExecuteInfo(Drivers.Target target, string cmd)
        {
            var userCtx = await Accounts.ResolveIamUser(target);
            if (userCtx == null)
            {
                await target.reply("你还没有绑定 desu.life 账户，先使用 !bind 来进行绑定哦。");
                return;
            }
            await Info(target, cmd, userCtx);
        }

        public static async Task ExecuteList(Drivers.Target target)
        {
            var userCtx = await Accounts.ResolveIamUser(target);
            if (userCtx == null)
            {
                await target.reply("你还没有绑定 desu.life 账户，先使用 !bind 来进行绑定哦。");
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
                await target.reply("请提供正确的徽章编号，如: !badge info 1");
                return;
            }

            var profile = await API.Kagami.Client.GetPublicKanonBotProfile(userCtx.IamUserId);
            if (profile == null || profile.InstalledBadges.Count == 0)
            {
                await target.reply("你还没有佩戴的徽章呢...");
                return;
            }

            if (badgeNum > profile.InstalledBadges.Count)
            {
                await target.reply(
                    $"你当前佩戴了 {profile.InstalledBadges.Count} 个徽章，没有编号为 {badgeNum} 的。"
                );
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
                await target.reply("你还没有佩戴的徽章呢...\n前往 https://hub.kagamistudio.com 管理你的徽章。");
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
