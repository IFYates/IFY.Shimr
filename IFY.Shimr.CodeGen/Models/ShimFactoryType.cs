using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryType(ITypeSymbol interfaceType) : BaseShimType(interfaceType)
{
    public override string Name { get; } = $"{interfaceType.Name}_Factory";

    public ShimFactoryTarget AddShim(ITypeSymbol underlyingType, ISymbol? singleMember)
    {
        var shim = new ShimFactoryTarget(this, underlyingType, singleMember);
        return AddTarget(shim);
    }

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        code.AppendLine($"using {AutoShimCodeWriter.EXT_NAMESPACE};")
            .AppendLine($"namespace {AutoShimCodeWriter.SB_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    public static partial class {AutoShimCodeWriter.SB_CLASSNAME}")
            .AppendLine("    {")
            .AppendLine($"        protected class {Name} : {InterfaceFullName}")
            .AppendLine("        {");

        foreach (var shim in Shims.OfType<ShimFactoryTarget>())
        {
            shim.GenerateCode(code, errors);
        }

        code.AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");
    }
}