using IFY.Shimr.CodeGen.CodeAnalysis;
using IFY.Shimr.CodeGen.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IFY.Shimr.CodeGen;

internal class AutoShimCodeWriter(GeneratorExecutionContext context, CodeErrorReporter errors)
{
    public const string SB_NAMESPACE = "IFY.Shimr";
    public const string SB_CLASSNAME = "ShimBuilder";
    public const string EXT_NAMESPACE = "IFY.Shimr.Extensions";
    public const string EXT_CLASSNAME = nameof(Extensions.ObjectExtensions);
    public const string EXT_CLASSNAMEFULL = $"{EXT_NAMESPACE}.{EXT_CLASSNAME}";

    public LanguageVersion CSLangver { get; }
        = (context.Compilation.SyntaxTrees.FirstOrDefault().Options as CSharpParseOptions)?.LanguageVersion
        ?? LanguageVersion.Default;

    public void WriteFactoryClass(StringBuilder code, IEnumerable<ShimModel> shims)
    {
        code.AppendLine($"namespace {SB_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    public static partial class {SB_CLASSNAME}")
            .AppendLine("    {")

            .AppendLine("        public static T Create<T>() where T : class")
            .AppendLine("        {");

        // Factory
        var shimTypes = shims.OfType<ShimFactoryModel>()
            .GroupBy(s => s.UnderlyingFullName).ToArray();
        foreach (var underlyingShims in shimTypes)
        {
            var underlyingType = underlyingShims.First().UnderlyingType;
            foreach (var shim in underlyingShims)
            {
                code.AppendLine($"            if (typeof(T) == typeof({shim.InterfaceFullName}))")
                    .AppendLine("            {")
                    .AppendLine($"                return (T)(object)new {shim.Name}();")
                    .AppendLine("            }");
            }
        }

        code.AppendLine("            throw new NotSupportedException();") // TODO: detail
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");
    }

    public void WriteExtensionClass(StringBuilder code, IEnumerable<ShimModel> shims)
    {
        code.AppendLine($"namespace {EXT_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    public static partial class {EXT_CLASSNAME}")
            .AppendLine("    {");

        // Shim: Underlying -> Interface
        var shimTypes = shims.Where(s => s is not ShimFactoryModel)
            .GroupBy(s => s.UnderlyingFullName).ToArray();
        foreach (var underlyingShims in shimTypes)
        {
            var underlyingType = underlyingShims.First().UnderlyingType;
            var isStruct = underlyingType.IsValueType;

            if (CSLangver >= LanguageVersion.CSharp8)
            {
                code.AppendLine("        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"inst\")]");
            }
            code.AppendLine($"        public static T? Shim<T>(this {underlyingShims.Key}? inst)")
                .Append($"            => {(isStruct ? "!inst.HasValue" : "inst is null")}")
                .AppendLine($" ? default : Shim<T>({(isStruct ? "inst.Value" : "inst")});")
                .AppendLine($"        public static T Shim<T>(this {underlyingShims.Key} inst)")
                .AppendLine("        {");
            foreach (var shim in underlyingShims)
            {
                code.AppendLine($"            if (typeof(T) == typeof({shim.InterfaceFullName}))")
                    .AppendLine("            {")
                    .AppendLine($"                return (T)(object)new {shim.Name}(inst);")
                    .AppendLine("            }");
            }
            code.AppendLine("            throw new NotSupportedException();") // TODO: detail
                .AppendLine("        }");
        }

        // Unshim: Interface -> Underlying
        shimTypes = shims.GroupBy(s => s.InterfaceFullName).ToArray();
        foreach (var shim in shimTypes)
        {
            code.AppendLine($"        public static object Unshim(this {shim.Key} shim) => ((IShim)shim).Unshim();")
                .AppendLine($"        public static T Unshim<T>(this {shim.Key} shim) => (T)(object)((IShim)shim).Unshim();");
        }

        code.AppendLine("    }")
            .AppendLine("}");
    }

    public void WriteShim(StringBuilder code, ShimModel shim)
    {
        if (shim is ShimFactoryModel)
        {
            code.AppendLine($"using {EXT_NAMESPACE};")
                .AppendLine($"namespace {SB_NAMESPACE}")
                .AppendLine("{")
                .AppendLine($"    public static partial class {SB_CLASSNAME}")
                .AppendLine("    {");
        }
        else
        {
            code.AppendLine($"namespace {EXT_NAMESPACE}")
                .AppendLine("{")
                .AppendLine($"    public static partial class {EXT_CLASSNAME}")
                .AppendLine("    {");
        }

        shim.GenerateCode(code, errors);

        code.AppendLine("    }")
            .AppendLine("}");
    }
}
