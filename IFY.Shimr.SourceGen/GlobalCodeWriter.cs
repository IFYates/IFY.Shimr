using IFY.Shimr.SourceGen.CodeAnalysis;
using IFY.Shimr.SourceGen.Models;
using IFY.Shimr.SourceGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.SourceGen;

internal class GlobalCodeWriter(GeneratorExecutionContext context) : ICodeWriter
{
    public const string SB_NAMESPACE = "IFY.Shimr";
    public const string SB_CLASSNAME = "ShimBuilder";
    public const string EXT_NAMESPACE = $"{SB_NAMESPACE}.Extensions";
    public const string EXT_CLASSNAME = "ObjectExtensions";
    public const string EXT_CLASSNAMEFULL = $"{EXT_NAMESPACE}.{EXT_CLASSNAME}";

    private const string SB_CLASS_CS = $@"namespace {SB_NAMESPACE}
{{{{
    public static partial class {SB_CLASSNAME}
    {{{{
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Type> _factoryMap = new System.Collections.Generic.Dictionary<System.Type, System.Type>
        {{{{
{{0}}        }}}};

        /// <summary>
        /// Create a factory shim of <typeparamref name=""TInterface""/>
        /// The type must be decorated with <see cref=""IFY.Shimr.StaticShimAttribute""/>, otherwise <see cref=""System.NotSupportedException""/> will be thrown.
        /// </summary>
        public static TInterface Create<TInterface>() where TInterface : class
        {{{{
            if (_factoryMap.TryGetValue(typeof(TInterface), out var factoryType))
            {{{{
                return (TInterface)System.Activator.CreateInstance(factoryType);
            }}}}
            throw new System.NotSupportedException($""Interface '{{{{typeof(TInterface).FullName}}}}' does not have 'StaticShimAttribute' to register as factory."");
        }}}}
    }}}}
}}}}
";

    public static void WriteFactoryClass(ICodeWriter writer, IEnumerable<IBinding> shims)
    {
        var factoryDefs = shims.Select(s => s.Definition)
            .OfType<ShimFactoryDefinition>().ToArray();
        if (!factoryDefs.Any())
        {
            return;
        }

        // Factory
        var code = new StringBuilder();
        foreach (var def in factoryDefs.Distinct())
        {
            code.AppendLine($"            [typeof({def.FullTypeName})] = typeof({def.Name}),");
        }

        writer.AppendFormat(SB_CLASS_CS, code);
        writer.WriteSource($"{SB_CLASSNAME}.g.cs");
    }

    private const string EXT_CLASS_CS = $@"#nullable enable
namespace {EXT_NAMESPACE}
{{{{
    using System.Linq;
    public static partial class {EXT_CLASSNAME}
    {{{{
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.Dictionary<System.Type, System.Type>> _factoryMap = new System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.Dictionary<System.Type, System.Type>>
        {{{{
{{1}}        }}}};

        /// <summary>
        /// Shim an instance of an <cref name=""object""/> to <typeparamref name=""TInterface""/>.
        /// </summary>
{{0}}        public static TInterface{{2}} Shim<TInterface>(this object instance) where TInterface : class
        {{{{
            if (instance == null) return null;
            var interfaceType = typeof(TInterface).IsGenericType ? typeof(TInterface).GetGenericTypeDefinition() : typeof(TInterface);
            if (_factoryMap.TryGetValue(interfaceType, out var typeMap))
            {{{{
                var instanceType = instance.GetType();
                instanceType = instanceType.IsGenericType ? instanceType.GetGenericTypeDefinition() : instanceType;
                if (!typeMap.TryGetValue(instanceType, out var factoryType))
                {{{{
                    factoryType = typeMap.FirstOrDefault(m => m.Key.IsAssignableFrom(instanceType)).Value;
                    if (factoryType != null)
                    {{{{
                        typeMap.Add(instanceType, factoryType);
                    }}}}
                }}}}

                if (factoryType != null)
                {{{{
                    factoryType = factoryType.IsGenericType ? factoryType.MakeGenericType(typeof(TInterface).GenericTypeArguments) : factoryType;
                    return (TInterface)System.Activator.CreateInstance(factoryType, instance);
                }}}}
            }}}}

            throw new System.NotSupportedException($""Interface '{{{{typeof(TInterface).FullName}}}}' is not registered as a shim of type '{{{{instance.GetType().FullName}}}}'."");
        }}}}

        /// <summary>
        /// Recast shims to original type.
        /// No type-safety checks. Must already be <typeparamref name=""T""/> or be <see cref=""IShim""/> of <typeparamref name=""T""/>.
        /// </summary>
{{0}}        public static System.Collections.Generic.IEnumerable<T> Unshim<T>(this System.Collections.Generic.IEnumerable<object> shims)
        {{{{
            return shims.Select(s => s is T obj ? obj : (T)((IShim)s).Unshim());
        }}}}
    }}}}
}}}}
";

    /// <summary>
    /// Generate the static class providing the '.Shim&lt;T&gt;(object)' extension method.
    /// </summary>
    public static void WriteExtensionClass(ICodeWriter writer, IEnumerable<IBinding> allBindings)
    {
        // TODO: option to use namespace of underlying?
        var codeArgs = new string?[3];

        // If current project supports nullable, add extra info
        if (writer.HasNullableAttributes)
        {
            codeArgs[0] = "        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"inst\")]\r\n";
            codeArgs[2] = "?";
        }

        var code = new StringBuilder();
        var shimCombos = allBindings
            .Where(b => b is not ShimMemberProxyBinding && b.Definition is InstanceShimDefinition)
            .Distinct().ToArray();
        foreach (var shim in shimCombos.GroupBy(s => s.Definition))
        {
            code.AppendLine($"            [typeof({shim.Key.Symbol.ToGenericName()})] = new System.Collections.Generic.Dictionary<System.Type, System.Type>")
                .AppendLine("            {");
            foreach (var binding in shim.GroupBy(s => s.Target).Select(g => g.First()))
            {
                code.AppendLine($"                [typeof({binding.Target.Symbol.ToGenericName()})] = typeof({binding.ClassName}{(binding.Target.Symbol is INamedTypeSymbol { IsGenericType: true } ? "<>" : null)}),");
            }
            code.AppendLine("            },");
        }
        codeArgs[1] = code.ToString();

        writer.AppendFormat(EXT_CLASS_CS, codeArgs);
        writer.WriteSource($"{EXT_CLASSNAME}.g.cs");
    }

    private readonly StringBuilder _code = new();

    public bool HasNullableAttributes { get; } = context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute") != null;
    public bool HasStackTraceHiddenAttribute { get; } = context.Compilation.GetTypeByMetadataName("System.Diagnostics.StackTraceHiddenAttribute") != null;

    public void Append(string value)
        => _code.Append(value);
    public void AppendLine(string value)
        => _code.AppendLine(value);
    public void AppendFormat(string format, params object?[] args)
        => _code.AppendFormat(format, args);

    public void WriteSource(string name)
    {
        var code = _code.ToString();
        _code.Clear();
        context.AddSource(name, $"// Generated at {DateTime.Now:O}\r\n{code}");
        Diag.WriteOutput($"/** File: {name} **/\r\n{code}");
    }
}
