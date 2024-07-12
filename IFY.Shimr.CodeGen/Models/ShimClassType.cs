using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

/// <summary>
/// Models an interface used to shim another type.
/// </summary>
internal class ShimClassType(ITypeSymbol interfaceType) : BaseShimType(interfaceType)
{
    public ShimClassTarget AddShim(ITypeSymbol underlyingType)
    {
        var shim = new ShimClassTarget(this, underlyingType);
        return AddTarget(shim);
    }

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        code.AppendLine($"namespace {AutoShimCodeWriter.EXT_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    public static partial class {AutoShimCodeWriter.EXT_CLASSNAME}")
            .AppendLine("    {");

        foreach (var shim in Shims.OfType<ShimClassTarget>())
        {
            shim.GenerateCode(code, errors);
        }

        code.AppendLine("    }")
            .AppendLine("}");
    }
}
