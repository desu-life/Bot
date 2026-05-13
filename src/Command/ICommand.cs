using CommandSystem.Definition;
using CommandSystem.Parsing;
using KanonBot.Drivers;

namespace CommandSystem;

/// <summary>
/// 所有指令的统一接口
/// </summary>
public interface ICommand
{
    CommandDef Definition { get; }
    Task Execute(Target target, ParsedCommand cmd);
}
