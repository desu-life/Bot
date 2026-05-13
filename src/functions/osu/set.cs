using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.API.OSU;
using KanonBot.API.PPYSB;
using KanonBot.Drivers;
using KanonBot.Functions;
using KanonBot.Message;

namespace KanonBot.Functions.OSUBot
{
    // ── Set ICommand classes ──────────────────────────────

    public class SetHelpCommand : ICommand
    {
        private const string SettingsUrl = "https://hub.kagamistudio.com/settings/";
        public CommandDef Definition =>
            new()
            {
                Name = "set",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply($"面板相关设置已迁移到网页端，请前往 {SettingsUrl} 进行设置。");
    }

    public class SetOsuModeCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "set osumode",
                Args =
                [
                    new() { Name = "mode", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple }
                ],
                Flags =  [ new() { Name = "sb_server", Value = "sb", SlashName = "is_sb" } ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Setter.SetOsuMode(target, cmd);
    }

    public class SetDeprecatedCommand : ICommand
    {
        private const string SettingsUrl = "https://hub.kagamistudio.com/settings/";
        public CommandDef Definition =>
            new()
            {
                Name = "set osuinfopanelversion",
                Aliases =
                [
                    "set osuinfopanelv2colormode",
                    "set osuinfopanelv2colorcustom",
                    "set osuinfopanelv2img",
                    "set osuinfopanelv1img",
                    "set osuinfopanelv2panel",
                    "set osuinfopanelv1panel",
                ],
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply($"该设置项已迁移到网页端，请前往 {SettingsUrl} 进行设置。");
    }

    // ── Setter internal methods ────────────────────────────

    public class Setter
    {
        public static async Task SetOsuMode(Target target, ParsedCommand cmd)
        {
            var modeToken = cmd.GetString("mode");
            bool hasSbFlag = cmd.Flag("sb_server");

            if (string.IsNullOrWhiteSpace(modeToken))
            {
                await target.reply(
                    "用法: !set osumode <模式> [&sb]\n示例: !set osumode std / !set osumode rx0 / !set osumode rx0 &sb"
                );
                return;
            }

            var osuMode = modeToken.ParseMode();
            var sbMode = modeToken.ParsePpysbMode();

            if (osuMode == null && sbMode == null)
            {
                await target.reply("提供的模式不正确，请重新确认 (osu/taiko/fruits/mania/rx0/ap0)");
                return;
            }

            if (sbMode != null && !sbMode.Value.IsSupported())
            {
                await target.reply("提供的 sb 模式当前不支持，请更换模式后重试。");
                return;
            }

            bool isSbOnlyMode = sbMode.HasValue && (int)sbMode.Value > 3;
            bool useSbMode = hasSbFlag || isSbOnlyMode;

            // Build a ParsedCommand for ResolveCommandUser (self-query with mode)
            var resolveCmd = new ParsedCommand
            {
                SelfQuery = true,
                Args = new() { ["osu_mode"] = modeToken, ["username"] = null },
                Flags = new() { ["sb_server"] = useSbMode },
            };
            var resolved = await Accounts.ResolveCommandUser(target, resolveCmd);
            if (resolved == null || resolved.IamUserId == null)
                return;

            if (useSbMode)
            {
                if (!sbMode.HasValue)
                {
                    await target.reply(
                        "当前模式不是可用的 ppy.sb 模式。\n示例: osu / taiko / ctb / mania / rx0 / ap0"
                    );
                    return;
                }

                osuMode = hasSbFlag ? null : sbMode.Value.ToOsu();

                try
                {
                    var sbModeRaw = KagamiExtensions.ToPpysbModeApiValue(sbMode.Value);
                    var sbOk = await API.Kagami
                        .Client
                        .SetPpySbGameMode(resolved.IamUserId, sbModeRaw);
                    if (!sbOk)
                    {
                        await target.reply("发生了错误，无法设置osu模式，请联系管理员。");
                        return;
                    }

                    if (osuMode != null)
                    {
                        var osuModeRaw = ToOsuModeApiValue(osuMode.Value);
                        var osuOk = await API.Kagami
                            .Client
                            .SetGameMode(resolved.IamUserId, osuModeRaw);
                    }

                    await target.reply("成功设置sb服的模式为 " + sbMode.Value.ToDisplay());
                }
                catch
                {
                    await target.reply("发生了错误，无法设置osu模式，请联系管理员。");
                }
            }
            else
            {
                if (!osuMode.HasValue)
                {
                    await target.reply("提供的模式不正确，请重新确认 (osu/taiko/fruits/mania)");
                    return;
                }

                try
                {
                    var modeRaw = ToOsuModeApiValue(osuMode.Value);
                    var ok = await API.Kagami.Client.SetGameMode(resolved.IamUserId, modeRaw);
                    if (ok)
                        await target.reply("成功设置模式为 " + osuMode.Value.ToDisplay());
                    else
                        await target.reply("发生了错误，无法设置osu模式，请联系管理员。");
                }
                catch
                {
                    await target.reply("发生了错误，无法设置osu模式，请联系管理员。");
                }
            }
        }
    }
}
