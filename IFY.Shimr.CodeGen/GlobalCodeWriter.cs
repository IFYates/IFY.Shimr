using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen;

internal class GlobalCodeWriter(GeneratorExecutionContext context) : ICodeWriter
{
    public const string SB_NAMESPACE = "IFY.Shimr";
    public const string SB_CLASSNAME = "ShimBuilder";
    public const string EXT_NAMESPACE = $"{SB_NAMESPACE}.Extensions";
    public const string EXT_CLASSNAME = "ObjectExtensions";
    public const string EXT_CLASSNAMEFULL = $"{EXT_NAMESPACE}.{EXT_CLASSNAME}";

    public bool HasNullableAttributes { get; } = context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute") != null;

    public void AddSource(string name, StringBuilder code)
    {
        code.Insert(0, $"// Generated at {DateTime.Now:O}\r\n");
        context.AddSource(name, code.ToString());
        Diag.WriteOutput($"/** File: {name} **/\r\n{code}");
    }

    public static void WriteFactoryClass(ICodeWriter writer, IEnumerable<IBinding> shims)
    {
        var factoryDefs = shims.Select(s => s.Definition)
            .OfType<ShimFactoryDefinition>().ToArray();
        if (!factoryDefs.Any())
        {
            return;
        }

        var code = new StringBuilder();
        code.AppendLine($"namespace {SB_NAMESPACE}")
            .AppendLine("{")
            .AppendLine(1, $"public static partial class {SB_CLASSNAME}")
            .AppendLine(1, "{")

            .AppendLine("        /// <summary>")
            .AppendLine("        /// Create a factory shim of <typeparamref name=\"TInterface\"/>.")
            .AppendLine("        /// The type must be decorated with <see cref=\"IFY.Shimr.StaticShimAttribute\"/>, otherwise <see cref=\"System.NotSupportedException\"/> will be thrown.")
            .AppendLine("        /// </summary>")
            .AppendLine("        public static TInterface Create<TInterface>() where TInterface : class")
            .AppendLine("        {");

        // Factory
        foreach (var def in factoryDefs.Distinct())
        {
            code.AppendLine($"            if (typeof(TInterface) == typeof({def.FullTypeName}))")
                .AppendLine("            {")
                .AppendLine($"                return (TInterface)(object)new {def.Name}();")
                .AppendLine("            }");
        }

        code.AppendLine("            throw new System.NotSupportedException($\"Interface '{typeof(TInterface).FullName}' does not have 'StaticShimAttribute' to register as factory.\");")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");

        writer.AddSource($"{SB_CLASSNAME}.g.cs", code);
    }

    public static void WriteExtensionClass(ICodeWriter writer, IEnumerable<IBinding> allBindings)
    {
        var code = new StringBuilder();
        code.AppendLine("#nullable enable")
            .AppendLine($"namespace {EXT_NAMESPACE}") // TODO: option to use namespace of underlying?
            .AppendLine("{")
            .AppendLine(1, $"public static partial class {EXT_CLASSNAME}")
            .AppendLine(1, "{");

        // Shim: Underlying -> Interface
        code.AppendLine(2, "/// <summary>")
            .AppendLine(2, $"/// Shim an instance of an object to <typeparamref name=\"TInterface\"/>.")
            .AppendLine(2, "/// </summary>");
        if (writer.HasNullableAttributes)
        {
            code.AppendLine("        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"inst\")]");
        }
        code.AppendLine($"        public static TInterface Shim<TInterface>(this object inst) where TInterface : class")
            .AppendLine("        {")
            .AppendLine("            if (inst == null)")
            .AppendLine("            {")
            .AppendLine("                return null;")
            .AppendLine("            }")
            .AppendLine("            var instType = inst.GetType();");

        var shimCombos = allBindings
            .Where(b => b is not ShimMemberProxyBinding && b.Definition is InstanceShimDefinition)
            .Select(b => (b.Definition, b.Target, b.ClassName))
            .Distinct().ToArray();
        if (shimCombos.Any(s => !s.Target.Symbol.IsGenericType))
        {
            code.AppendLine("            if (!typeof(TInterface).IsGenericType && !instType.IsGenericType)")
                .AppendLine("            {");
            foreach (var binding in shimCombos.Where(s => !s.Target.Symbol.IsGenericType).GroupBy(s => s.Definition))
            {
                code.AppendLine($"                if (typeof(TInterface) == typeof({binding.Key.FullTypeName}))")
                    .AppendLine("                {");
                foreach (var (_, target, className) in binding)
                {
                    code.AppendLine($"                    if (instType.IsAssignableFrom(typeof({target.FullTypeName})))")
                        .AppendLine("                    {")
                        .AppendLine($"                        return (TInterface)(object)new {className}(({target.FullTypeName})inst);")
                        .AppendLine("                    }");
                }
                code.AppendLine("                }");
            }
            code.AppendLine("            }");
        }
        if (shimCombos.Any(s => s.Target.Symbol.IsGenericType))
        {
            code.AppendLine("            if (typeof(TInterface).IsGenericType && instType.IsGenericType)")
                .AppendLine("            {")
                .AppendLine("                var shimGenType = typeof(TInterface).GetGenericTypeDefinition();")
                .AppendLine("                var instGenType = instType.GetGenericTypeDefinition();");
            foreach (var binding in shimCombos.Where(s => s.Target.Symbol.IsGenericType).GroupBy(s => s.Definition))
            {
                code.AppendLine($"                if (shimGenType == typeof({binding.Key.Symbol.ToGenericName()}))")
                    .AppendLine("                {");
                foreach (var (_, target, className) in binding)
                {
                    code.AppendLine($"                    if (instGenType.IsAssignableFrom(typeof({target.Symbol.ToGenericName()})))")
                        .AppendLine("                    {")
                        .AppendLine($"                    var classType = typeof({className}<>).MakeGenericType(typeof(TInterface).GetGenericArguments());")
                        .AppendLine($"                        return (TInterface)System.Activator.CreateInstance(classType, inst);")
                        .AppendLine("                    }");
                }
                code.AppendLine("                }");
            }
            code.AppendLine("            }");
        }
        code.AppendLine($"            throw new System.NotSupportedException($\"Interface '{{typeof(TInterface).FullName}}' is not registered as a shim of type '{{instType.FullName}}'.\");")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");

        writer.AddSource($"{EXT_CLASSNAME}.g.cs", code);
    }
}
