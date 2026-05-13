using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSUBot
{
    public class PingCommand : ICommand
    {
        public CommandDef Definition => new()
        {
            Name = "ping",
            Args = [],
            Flags = []
        };

        public Task Execute(Target target, ParsedCommand cmd)
            => target.reply("meow~");
    }
}
