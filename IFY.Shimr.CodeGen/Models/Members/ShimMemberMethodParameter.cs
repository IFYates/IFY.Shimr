using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal class ShimMemberMethodParameter(IParameterSymbol symbol)
{
    // TODO: out, ref, default
    // TODO: nullability?
    // TODO: attributes?

    public string Name { get; } = symbol.Name;
    public ITypeSymbol Type { get; } = symbol.Type;
    public ITypeSymbol? UnderlyingType { get; } = symbol.GetArgumentShimType(true);

    public string GetTargetArgumentCode()
    {
        return UnderlyingType == null
            ? Name
            : $"{Name}.Unshim<{UnderlyingType.ToDisplayString()}>()";
    }

    public override string ToString()
        => $"{Type.ToDisplayString()} {Name}";

    public void ResolveImplicitShims(ShimRegister shimRegister)
    {
        // Argument overrides
        if (UnderlyingType != null)
        {
            shimRegister.GetOrCreate(Type)
                .AddShim(UnderlyingType);
        }
    }
}
