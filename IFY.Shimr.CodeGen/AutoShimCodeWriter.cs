using IFY.Shimr.CodeGen.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IFY.Shimr.CodeGen;

internal class AutoShimCodeWriter(GeneratorExecutionContext context)
{
    public const string SB_NAMESPACE = "IFY.Shimr";
    public const string SB_CLASSNAME = "ShimBuilder";
    public const string EXT_NAMESPACE = "IFY.Shimr.Extensions";
    public const string EXT_CLASSNAME = nameof(Extensions.ObjectExtensions);
    public const string EXT_CLASSNAMEFULL = $"{EXT_NAMESPACE}.{EXT_CLASSNAME}";

    public LanguageVersion CSLangver { get; }
        = (context.Compilation.SyntaxTrees.FirstOrDefault().Options as CSharpParseOptions)?.LanguageVersion
        ?? LanguageVersion.Default;

    public void WriteFactoryClass(StringBuilder code, IEnumerable<IShimTarget> shims)
    {
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
        var shimTypes = shims.OfType<ShimFactoryTarget>()
            .GroupBy(s => s.UnderlyingFullName).ToArray();
        foreach (var shimType in shimTypes.Select(g => g.First().ShimType))
        {
            code.AppendLine($"            if (typeof(TInterface) == typeof({shimType.InterfaceFullName}))")
                .AppendLine("            {")
                .AppendLine($"                return (TInterface)(object)new {shimType.Name}();")
                .AppendLine("            }");
        }

        code.AppendLine("            throw new System.NotSupportedException();") // TODO: detail
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");
    }

    public void WriteExtensionClass(StringBuilder code, IEnumerable<IShimTarget> shims)
    {
        code.AppendLine($"namespace {EXT_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    public static partial class {EXT_CLASSNAME}")
            .AppendLine("    {");

        // Shim: Underlying -> Interface
        var shimTypes = shims.Where(s => s is not ShimFactoryTarget)
            .GroupBy(s => s.UnderlyingFullName).ToArray();
        foreach (var underlyingShims in shimTypes)
        {
            var underlyingType = underlyingShims.First().UnderlyingType;

            if (underlyingType.IsValueType)
            {
                if (CSLangver >= LanguageVersion.CSharp8)
                {
                    code.AppendLine("        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"inst\")]");
                }
                code.AppendLine($"        public static TInterface? Shim<TInterface>(this {underlyingShims.Key}? inst) where TInterface : class")
                    .Append("            => !inst.HasValue")
                    .AppendLine($" ? default : Shim<TInterface>(inst.Value);");
            }

            code.AppendLine("        /// <summary>")
                .AppendLine("        /// Create an instance of <typeparamref name=\"TInterface\"/> shim.")
                .AppendLine("        /// </summary>")
                .AppendLine($"        public static TInterface Shim<TInterface>(this {underlyingShims.Key} inst) where TInterface : class")
                .AppendLine("        {");
            foreach (var shim in underlyingShims)
            {
                code.AppendLine($"            if (typeof(TInterface) == typeof({shim.InterfaceFullName}))")
                    .AppendLine("            {")
                    .AppendLine($"                return (TInterface)(object)new {shim.Name}(inst);")
                    .AppendLine("            }");
            }
            code.AppendLine("            throw new System.NotSupportedException();") // TODO: detail
                .AppendLine("        }");
        }

        // Unshim: Interface -> Underlying
        shimTypes = shims.Where(s => s is not ShimFactoryTarget)
            .GroupBy(s => s.InterfaceFullName).ToArray();
        foreach (var shim in shimTypes)
        {
            code.AppendLine($"        public static object Unshim(this {shim.Key} shim) => ((IShim)shim).Unshim();")
                .AppendLine($"        public static T Unshim<T>(this {shim.Key} shim) => (T)(object)((IShim)shim).Unshim();");
        }

        code.AppendLine("    }")
            .AppendLine("}");
    }
}
