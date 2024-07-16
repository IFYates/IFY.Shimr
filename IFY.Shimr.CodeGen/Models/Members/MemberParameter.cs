using IFY.Shimr.CodeGen.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace IFY.Shimr.CodeGen.Models.Members;

internal class MemberParameter(IParameterSymbol symbol)
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

    public void RegisterOverride(ShimResolver shimResolver)
    {
        // Argument overrides
        if (UnderlyingType != null)
        {
            shimResolver.GetOrCreateShim(Type)
                .AddTarget(UnderlyingType);
        }
    }
}
