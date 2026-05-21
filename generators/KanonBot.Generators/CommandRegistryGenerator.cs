using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KanonBot.Generators;

/// <summary>
/// 增量源生成器：自动扫描所有实现 ICommand 的具体类，
/// 为标记了 [GenerateCommandRegistry] 的 partial class 生成 BuildRegistry() 方法。
/// </summary>
[Generator]
public class CommandRegistryGenerator : IIncrementalGenerator
{
    private const string AttributeShortName = "GenerateCommandRegistry";
    private const string AttributeFullName = "KanonBot.Generators.GenerateCommandRegistryAttribute";
    private const string ICommandFullName = "CommandSystem.ICommand";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 找到标记了 [GenerateCommandRegistry] 的类
        var registrarClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRegistrarCandidate(node),
                transform: static (ctx, _) => GetRegistrarInfo(ctx))
            .Where(static r => r is not null);

        // 2. 找到所有实现 ICommand 的具体类
        var commandClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds
                    && !cds.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: static (ctx, _) => GetCommandInfo(ctx))
            .Where(static c => c is not null);

        // 3. 收集所有命令类
        var collectedCommands = commandClasses.Collect();

        // 4. 组合 registrar + 所有命令类
        var combined = registrarClasses.Combine(collectedCommands);

        // 5. 生成代码
        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var registrar = pair.Left;
            var commands = pair.Right;

            if (registrar is null)
                return;

            var source = GenerateSource(registrar.Value, commands!);
            spc.AddSource($"{registrar.Value.ClassName}.g.cs", source);
        });
    }

    private static bool IsRegistrarCandidate(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax cds)
            return false;

        // Check if class has any attribute that looks like GenerateCommandRegistry
        foreach (var attrList in cds.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name == AttributeShortName ||
                    name == $"{AttributeShortName}Attribute" ||
                    name.EndsWith($".{AttributeShortName}") ||
                    name.EndsWith($".{AttributeShortName}Attribute"))
                    return true;
            }
        }
        return false;
    }

    private static RegistrarInfo? GetRegistrarInfo(GeneratorSyntaxContext ctx)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (symbol is null)
            return null;

        // Verify the attribute is actually our GenerateCommandRegistryAttribute
        var hasAttribute = symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == AttributeFullName);
        if (!hasAttribute)
            return null;

        return new RegistrarInfo
        {
            Namespace = symbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : symbol.ContainingNamespace.ToDisplayString(),
            ClassName = symbol.Name,
            IsStatic = symbol.IsStatic,
        };
    }

    private static CommandInfo? GetCommandInfo(GeneratorSyntaxContext ctx)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (symbol is null || symbol.IsAbstract || symbol.IsGenericType)
            return null;

        // 检查是否实现了 ICommand 接口
        if (!ImplementsICommand(symbol))
            return null;

        // 确保类有无参构造函数
        if (!HasParameterlessConstructor(symbol))
            return null;

        return new CommandInfo
        {
            FullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
        };
    }

    private static bool ImplementsICommand(INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(static i =>
            i.ToDisplayString() == ICommandFullName);
    }

    private static bool HasParameterlessConstructor(INamedTypeSymbol symbol)
    {
        // 如果没有定义任何构造函数，编译器会自动生成无参构造函数
        var constructors = symbol.InstanceConstructors;
        if (constructors.Length == 0)
            return true;

        return constructors.Any(static c =>
            c.Parameters.Length == 0 &&
            c.DeclaredAccessibility == Accessibility.Public);
    }

    private static string GenerateSource(RegistrarInfo registrar, ImmutableArray<CommandInfo?> commands)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using CommandSystem;");
        sb.AppendLine("using CommandSystem.Definition;");
        sb.AppendLine();

        if (registrar.Namespace is not null)
        {
            sb.AppendLine($"namespace {registrar.Namespace};");
            sb.AppendLine();
        }

        var staticModifier = registrar.IsStatic ? "static " : "";
        sb.AppendLine($"public {staticModifier}partial class {registrar.ClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    public static CommandRegistry BuildRegistry()");
        sb.AppendLine("    {");
        sb.AppendLine("        var registry = new CommandRegistry();");

        // 按完全限定名排序，确保生成代码稳定
        var sortedCommands = commands
            .Where(static c => c is not null)
            .OrderBy(static c => c!.Value.FullyQualifiedName)
            .ToList();

        foreach (var cmd in sortedCommands)
        {
            sb.AppendLine($"        registry.Register(new {cmd!.Value.FullyQualifiedName}());");
        }

        sb.AppendLine("        return registry;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private struct RegistrarInfo
    {
        public string? Namespace;
        public string ClassName;
        public bool IsStatic;
    }

    private struct CommandInfo
    {
        public string FullyQualifiedName;
    }
}
