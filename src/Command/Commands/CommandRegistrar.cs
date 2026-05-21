using CommandSystem;
using CommandSystem.Definition;
using KanonBot.Generators;

namespace CommandSystem.Execution;

/// <summary>
/// 注册所有 ICommand 实例到 Registry。
/// BuildRegistry() 方法由源生成器自动生成。
/// </summary>
[GenerateCommandRegistry]
public static partial class CommandRegistrar
{
}
