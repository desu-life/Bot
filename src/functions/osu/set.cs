using KanonBot.Drivers;
using KanonBot.Message;

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
    }
}
