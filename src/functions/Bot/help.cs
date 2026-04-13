using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;

namespace KanonBot.Functions.OSUBot
{
    public class Help
    {
        public async static Task Execute(Target target, string cmd)
        {
            await target.reply(
                """
                用户查询：
                !info/recent/bp/get
                绑定/用户设置：
                !bind/reg/set
                更多细节请移步 https://support.desu.life/posts/2022-kanonbot-usage-doc/ 查阅
                """
            );
        }
    }
}
