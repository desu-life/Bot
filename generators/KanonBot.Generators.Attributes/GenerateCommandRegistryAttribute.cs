using System;

namespace KanonBot.Generators;

/// <summary>
/// 标记一个 partial class，源生成器将为其生成 BuildRegistry() 方法，
/// 自动注册所有实现 ICommand 接口的具体类。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateCommandRegistryAttribute : Attribute
{
}
