using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.API.OSU;
using KanonBot.API.PPYSB;
using KanonBot.Drivers;
using KanonBot.Functions;
using KanonBot.Message;
using static KanonBot.API.OSU.OSUExtensions;

namespace KanonBot.Functions
{
    public class SetOsuModeCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "set osumode",
                Description = "Set your default osu! Gamemode",
                Args =
                [
                    new() { Name = "mode", Description = "Default osu! Gamemode", Prefix = ArgPrefix.None, Strategy = ParseStrategy.Simple }
                ],
                Flags = [ new() { Name = "sb_server", Description = "Fetch from ppysb", Value = "sb", SlashName = "is_sb" } ]
            };

        public async Task Execute(Target target, ParsedCommand cmd)
        {
            var modeToken = cmd.GetString("mode");
            bool hasSbFlag = cmd.Flag("sb_server");

            if (string.IsNullOrWhiteSpace(modeToken))
            {
                await target.Treply("set.mode_usage");
                return;
            }

            var osuMode = modeToken.ParseMode();
            var sbMode = modeToken.ParsePpysbMode();

            if (osuMode == null && sbMode == null)
            {
                await target.Treply("set.invalid_mode");
                return;
            }

            if (sbMode != null && !sbMode.Value.IsSupported())
            {
                await target.Treply("set.sb_mode_unsupported");
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
                    await target.Treply("set.sb_mode_invalid");
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
                        await target.Treply("set.error");
                        return;
                    }

                    if (osuMode != null)
                    {
                        var osuModeRaw = ToOsuModeApiValue(osuMode.Value);
                        var osuOk = await API.Kagami
                            .Client
                            .SetGameMode(resolved.IamUserId, osuModeRaw);
                    }

                    await target.Treply("set.sb_mode_success", sbMode.Value.ToDisplay());
                }
                catch
                {
                    await target.Treply("set.error");
                }
            }
            else
            {
                if (!osuMode.HasValue)
                {
                    await target.Treply("set.invalid_mode_standard");
                    return;
                }

                try
                {
                    var modeRaw = ToOsuModeApiValue(osuMode.Value);
                    var ok = await API.Kagami.Client.SetGameMode(resolved.IamUserId, modeRaw);
                    if (ok)
                        await target.Treply("set.mode_success", osuMode.Value.ToDisplay());
                    else
                        await target.Treply("set.error");
                }
                catch
                {
                    await target.Treply("set.error");
                }
            }
        }
    }
}
