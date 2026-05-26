using CommandSystem;
using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace KanonBot.Functions.OSU
{
    public class SuUpdateAllCommand : ICommand
    {
        public CommandDef Definition =>
            new()
            {
                Name = "su updateall",
                Description = "Run the administrator daily update task",
                Args =  [ ],
                Flags =  [ ]
            };

        public Task Execute(Target target, ParsedCommand cmd) => Su.Execute(target, "updateall");
    }
}
