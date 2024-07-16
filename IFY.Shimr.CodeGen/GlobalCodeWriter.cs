using IFY.Shimr.CodeGen.Models;
using IFY.Shimr.CodeGen.Models.Bindings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IFY.Shimr.CodeGen;

internal class GlobalCodeWriter(GeneratorExecutionContext context) : ICodeWriter
{
    public const string SB_NAMESPACE = "IFY.Shimr";
    public const string SB_CLASSNAME = "ShimBuilder";
    public const string EXT_NAMESPACE = "IFY.Shimr.Extensions";
    public const string EXT_CLASSNAME = nameof(Extensions.ObjectExtensions);
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
        var factoryDefs = shims.Where(s => s.Definition is ShimFactoryDefinition)
            .Select(s => s.Definition).ToArray();
        if (!factoryDefs.Any())
        {
                var sb = new StringBuilder();
            sb.AppendLine("// " + shims.Count());
            foreach (var def in factoryDefs.Distinct())
            {
                sb.AppendLine("////// " + def.FullTypeName);
            }
                writer.AddSource($"{SB_CLASSNAME}.g.cs", sb);
            return;
        }

        var code = new StringBuilder();
        code.AppendLine($"namespace {SB_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    public static partial class {SB_CLASSNAME}")
            .AppendLine("    {")

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
        var targetTypes = allBindings.Where(b => b.Definition is InstanceShimDefinition)
            .GroupBy(b => b.Target.FullTypeName).ToArray();
        if (!targetTypes.Any())
        {
            return;
        }

        var code = new StringBuilder();
        code.AppendLine("#nullable enable")
            .AppendLine($"namespace {EXT_NAMESPACE}") // TODO: option to use namespace of underlying?
            .AppendLine("{")
            .AppendLine($"    public static partial class {EXT_CLASSNAME}")
            .AppendLine("    {");

        foreach (var targetBinding in targetTypes)
        {
            // ValueType shim wrap, for better IntelliSense
            var targetType = targetBinding.First().Target;
            if (targetType.IsValueType)
            {
                if (writer.HasNullableAttributes)
                {
                    code.AppendLine("        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"inst\")]");
                }
                code.AppendLine($"        public static TInterface? Shim<TInterface>(this {targetType.FullTypeName}? inst) where TInterface : class")
                    .Append("            => !inst.HasValue")
                    .AppendLine($" ? default : Shim<TInterface>(inst.Value);");
            }

            // Shim: Underlying -> Interface
            code.AppendLine("        /// <summary>")
                .AppendLine($"        /// Shim an instance of <see cref=\"{targetType.FullTypeName}\"/> as <typeparamref name=\"TInterface\"/>.")
                .AppendLine("        /// </summary>")
                .AppendLine($"        public static TInterface Shim<TInterface>(this {targetType.FullTypeName} inst) where TInterface : class")
                .AppendLine("        {");
            var bindings = targetBinding.GroupBy(b => b.Definition).ToArray();
            foreach (var binding in bindings)
            {
                code.AppendLine($"            if (typeof(TInterface) == typeof({binding.Key.FullTypeName}))")
                    .AppendLine("            {")
                    .AppendLine($"                return (TInterface)(object)new {binding.First().ClassName}(inst);")
                    .AppendLine("            }");
            }
            code.AppendLine($"            throw new System.NotSupportedException($\"Interface '{{typeof(TInterface).FullName}}' is not registered as a shim for '{targetType.FullTypeName}'.\");")
                .AppendLine("        }");
        }

        // Unshim: Interface -> Underlying
        foreach (var shim in allBindings.Select(b => b.Definition).Distinct())
        {
            code.AppendLine($"        public static object Unshim(this {shim.FullTypeName} shim) => ((IShim)shim).Unshim();")
                .AppendLine($"        public static T Unshim<T>(this {shim.FullTypeName} shim) => (T)(object)((IShim)shim).Unshim();");
        }

        code.AppendLine("    }")
            .AppendLine("}");

        writer.AddSource($"{EXT_CLASSNAME}.g.cs", code);
    }
}
