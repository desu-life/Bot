using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class BadgeListCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "badge list",
                Description = "List your installed badges",
                Args = [ ],
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
