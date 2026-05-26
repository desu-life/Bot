using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Execution;
using CommandSystem.Parsing;
using Flurl.Http;
using KanonBot.Drivers;
using KanonBot.Message;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Img = SixLabors.ImageSharp.Image;

namespace KanonBot.Functions.OSUBot
{
    public class BadgeInfoCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge info",
                Description = "Show details for one installed badge",
                Args =
                [
                    new() { Name = "badge_number", Description = "Badge number", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple, Parse = s => CommandDefs.ParseInt(s) }
                ],
                Flags = [ ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var userCtx = await Accounts.ResolveIamUser(target);
            if (userCtx == null)
            {
                await target.Treply("badge.not_bound");
                return;
            }

            var args = cmd.RawArgs;
            if (!int.TryParse(args, out int badgeNum) || badgeNum < 1)
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
    }
}
