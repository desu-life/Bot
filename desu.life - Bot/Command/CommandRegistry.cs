﻿using desu_life_Bot.Drivers;
using System.Reflection;
using System.Data;

namespace desu_life_Bot.Command
{
    public static class CommandRegister
    {
        public static void Register()
        {
            var registry = new CommandRegistry();
            registry.RegisterCommandsInAssembly(Assembly.GetExecutingAssembly());
            CommandSystem.RegisterCommandFromRegistry(registry);
            Log.Information("已注册指令");
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string[] CommandNames { get; }

        public CommandAttribute(params string[] commandNames)
        {
            CommandNames = commandNames;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ParamsAttribute : Attribute
    {
        public string[] Params { get; }

        public ParamsAttribute(params string[] commandNames)
        {
            Params = commandNames;
        }
    }

    public class CommandRegistry
    {
        private readonly Dictionary<
            string,
            (ParamsAttribute?, Func<CommandContext, Target, Task>)
        > _commandHandlers = new();

        // 通过反射扫描带有 Command 特性的方法，并将它们注册为命令处理程序
        public void RegisterCommandsInAssembly(Assembly assembly)
        {
            // 获取所有 public 类型
            var types = assembly.GetTypes().Where(t => t.IsClass && t.IsPublic);

            foreach (var type in types)
            {
                // 获取这些类型中所有 public 方法
                var methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                );

                foreach (var method in methods)
                {
                    var commandAttr = method.GetCustomAttribute<CommandAttribute>();
                    if (commandAttr != null)
                    {
                        var paramsAttr = method.GetCustomAttribute<ParamsAttribute>();
                        var instance = method.IsStatic
                            ? null
                            : Activator.CreateInstance(method.DeclaringType!);
                        var func =
                            (Func<CommandContext, Target, Task>)
                                Delegate.CreateDelegate(
                                    typeof(Func<CommandContext, Target, Task>),
                                    instance,
                                    method
                                );

                        foreach (var name in commandAttr.CommandNames)
                        {
                            _commandHandlers[name] = (paramsAttr, func);
                        }
                    }
                }
            }
        }

        public async Task HandleCommand(string commandName, CommandContext context, Target target)
        {
            if (_commandHandlers.TryGetValue(commandName, out var method))
            {
                await method.Item2.Invoke(context, target);
            }
            else
            {
                Console.WriteLine($"No command handler registered for: {commandName}");
            }
        }

        public Dictionary<
            string,
            (ParamsAttribute?, Func<CommandContext, Target, Task>)
        > commandHandlers => _commandHandlers;
    }
}
