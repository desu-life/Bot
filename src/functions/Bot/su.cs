using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using Flurl.Util;
using KanonBot.Drivers;
using KanonBot.Functions.OSUBot;
using static KanonBot.Functions.Accounts;

namespace KanonBot.Functions.OSU
{
    // ── Su ICommand classes ──────────────────────────────

    public class SuHelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "su",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Task.CompletedTask;
    }

    public class SuUpdateAllCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "su updateall",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Su.Execute(target, "updateall");
    }

    // ── Su internal router ────────────────────────────────

    public static class Su
    {
        public static async Task Execute(Target target, string cmd)
        {
            if (target.isFromAdmin == false)
                return;
            try
            {
                var user = await Accounts.ResolveIamUser(target);
                if (user is null)
                    return;
                var perm = await API.Kagami.Client.GetUserPermissions(user.IamUserId);
                if (perm == null)
                    return;

                if (!perm.Roles.Any(x => x == "kagami.admin"))
                    return;

                //execute
                string rootCmd,
                    childCmd = "";
                try
                {
                    var tmp = cmd.Split(' ', 2, StringSplitOptions.TrimEntries);
                    ;
                    rootCmd = tmp[0];
                    childCmd = tmp[1];
                }
                catch
                {
                    rootCmd = cmd;
                }

                switch (rootCmd.ToLower())
                {
                    case "updateall":
                        await SuDailyUpdateAsync(target);
                        return;
                    default:
                        return;
                }
            }
            catch { } //直接忽略
        }

        public static async Task SuDailyUpdateAsync(Target target)
        {
            await target.Treply("su.update_started");
            var _ = Task.Run(async () =>
            {
                var (count, span) = await GeneralUpdate.UpdateUsers();
                var Text = "共用时";
                if (span.Hours > 0)
                    Text += $" {span.Hours} 小时";
                if (span.Minutes > 0)
                    Text += $" {span.Minutes} 分钟";
                Text += $" {span.Seconds} 秒";
                try
                {
                    await target.Treply("su.update_complete", count, Text);
                }
                catch
                {
                    await target.Treply("su.update_complete_short", Text);
                }
            });
        }
    }
}
