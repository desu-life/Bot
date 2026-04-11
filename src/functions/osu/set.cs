using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API.OSU;
using KanonBot.API.PPYSB;
using KanonBot.Functions;

namespace KanonBot.Functions.OSUBot
{
    public class Setter
    {
        private const string SettingsUrl = "https://hub.kagamistudio.com";

        public static async Task Execute(Target target, string cmd)
        {
            string rootCmd,
                childCmd = "";
            try
            {
                var tmp = cmd.Split(' ', 2, StringSplitOptions.TrimEntries);;
                rootCmd = tmp[0];
                childCmd = tmp[1];
            }
            catch
            {
                rootCmd = cmd;
            }
            switch (rootCmd.ToLower())
            {
                case "osumode":
                    await SetOsuMode(target, childCmd);
                    return;
                case "osuinfopanelversion":
                case "osuinfopanelv2colormode":
                case "osuinfopanelv2colorcustom":
                case "osuinfopanelv2img":
                case "osuinfopanelv1img":
                case "osuinfopanelv2panel":
                case "osuinfopanelv1panel":
                    await target.reply($"该设置项已迁移到网页端，请前往 {SettingsUrl} 进行设置。");
                    break;
                default:
                    await target.reply(
                        $"面板相关设置已迁移到网页端，请前往 {SettingsUrl} 进行设置。"
                    );
                    return;
            }
        }

        private static async Task SetOsuMode(Target target, string cmd)
        {
            var tokens = cmd
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var hasSbFlag = tokens.Any(t => t.Equals("&sb", StringComparison.OrdinalIgnoreCase));
            var modeToken = tokens.FirstOrDefault(t => !t.StartsWith("&"));

            if (string.IsNullOrWhiteSpace(modeToken))
            {
                await target.reply("用法: !set osumode <模式> [&sb]\n示例: !set osumode std / !set osumode rx0 / !set osumode rx0 &sb");
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

            var resolveCmd = BotCmdHelper.CmdParser(
                $":{modeToken}" + (useSbMode ? "&sb" : string.Empty),
                BotCmdHelper.FuncType.Info
            );
            var resolved = await Accounts.ResolveCommandUser(target, resolveCmd);
            if (resolved == null || resolved.IamUserId == null)
                return;

            if (useSbMode)
            {
                if (!sbMode.HasValue)
                {
                    await target.reply("当前模式不是可用的 ppy.sb 模式。\n示例: osu / taiko / ctb / mania / rx0 / ap0");
                    return;
                }

                osuMode = hasSbFlag ? null : sbMode.Value.ToOsu();

                try
                {
                    var sbModeRaw = ToPpysbModeApiValue(sbMode.Value);
                    var sbOk = await API.Kagami.Client.SetPpySbGameMode(resolved.IamUserId, sbModeRaw);
                    if (!sbOk)
                    {
                        await target.reply("发生了错误，无法设置osu模式，请联系管理员。");
                        return;
                    }

                    if (osuMode != null)
                    {
                        var osuModeRaw = ToOsuModeApiValue(osuMode.Value);
                        var osuOk = await API.Kagami.Client.SetGameMode(resolved.IamUserId, osuModeRaw);
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
