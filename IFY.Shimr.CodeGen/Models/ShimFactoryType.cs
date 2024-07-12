using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models;

internal class ShimFactoryType(ITypeSymbol interfaceType) : BaseShimType(interfaceType)
{
    public override string Name { get; } = $"{interfaceType.Name}_Factory";

    public ShimFactoryTarget AddShim(ITypeSymbol underlyingType, ISymbol? singleMember)
    {
        var shim = new ShimFactoryTarget(this, underlyingType, singleMember, false);
        return AddTarget(shim);
    }
    public ShimFactoryTarget AddConstructor(ITypeSymbol underlyingType, IMethodSymbol constructorMethod)
    {
        var shim = new ShimFactoryTarget(this, underlyingType, constructorMethod, true);
        return AddTarget(shim);
    }

    public override void GenerateCode(StringBuilder code, CodeErrorReporter errors)
    {
        code.AppendLine($"namespace {AutoShimCodeWriter.SB_NAMESPACE}")
            .AppendLine("{")
            .AppendLine($"    using {AutoShimCodeWriter.EXT_NAMESPACE};")
            .AppendLine($"    using System.Linq;")
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