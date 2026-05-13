using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class HelpCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "help",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) =>
            target.reply(
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
